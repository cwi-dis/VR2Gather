using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using VRT.Core;
using VRT.Transport.Dash;

namespace VRT.Transport.TCP
{

    public class TCPWriter : BaseWriter
    {
        public struct TCPStreamDescription
        {
            public string host;
            public int port;
            public uint fourcc;
            public QueueThreadSafe inQueue;
        };

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

#if xxxjack_disabled
        public bin2dash.connection uploader;
        public string url;
#endif
        TCPStreamDescription[] descriptions;

        public class TCPPushThread
        {
            TCPWriter parent;
            TCPStreamDescription description;
            System.Threading.Thread myThread;
            Socket listenSocket = null;
            Socket sendSocket = null;

            public TCPPushThread(TCPWriter _parent, TCPStreamDescription _description)
            {
                parent = _parent;
                description = _description;
                myThread = new System.Threading.Thread(run);
                myThread.Name = Name();
                stats = new Stats(Name());
                IPAddress[] all = Dns.GetHostAddresses(description.host);
                all = Array.FindAll(all, a => a.AddressFamily == AddressFamily.InterNetwork);
                IPAddress ipAddress = all[0];
                IPEndPoint localEndpoint = new IPEndPoint(ipAddress, description.port);
                listenSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(localEndpoint);
                listenSocket.Listen(4);
                Debug.Log($"{Name()}: xxxjack listen to port {description.port} endpoint {localEndpoint.ToString()}");
            }

            public string Name()
            {
                return $"{parent.Name()}.{description.port}";
            }

            public void Start()
            {
                myThread.Start();
            }

            public void Stop()
            {
                if (listenSocket != null)
                {
                    listenSocket.Close();
                    listenSocket = null;
                }
                if (sendSocket != null)
                {
                    sendSocket.Close();
                    sendSocket = null;
                }
            }

            public void Join()
            {
                myThread.Join();
            }

            protected void run()
            {
                try
                {
                    Debug.Log($"{Name()}: thread started");
                    QueueThreadSafe queue = description.inQueue;
                    while (!queue.IsClosed() && listenSocket != null)
                    {
                        // Accept incoming connection
                        if (sendSocket == null)
                        {
                            BaseStats.Output(Name(), $"open=0, listen=1");
                            try
                            {
                                sendSocket = listenSocket.Accept();
                                BaseStats.Output(Name(), $"open=1, remote={sendSocket.RemoteEndPoint.ToString()}");
                            }
                            catch(SocketException e)
                            {
                                Debug.Log($"{Name()}: Accept: Exception {e.ToString()}");
                                continue;
                            }
                        }
                        NativeMemoryChunk mc = (NativeMemoryChunk)queue.Dequeue();
                        if (mc == null) continue; // Probably closing...
                        stats.statsUpdate(mc.length);
                        var hdr1 = BitConverter.GetBytes((UInt32)description.fourcc);
                        var hdr2 = BitConverter.GetBytes((Int32)mc.length);
                        var buf = new byte[mc.length];
                        System.Runtime.InteropServices.Marshal.Copy(mc.pointer, buf, 0, mc.length);
                        try
                        {
                            sendSocket.Send(hdr1);
                            sendSocket.Send(hdr2);
                            sendSocket.Send(buf);
                        }
                        catch (ObjectDisposedException)
                        {
                            sendSocket = null;
                            BaseStats.Output(Name(), $"open=0");
                        }


                    }
                    Debug.Log($"{Name()}: thread stopped");
                }
                catch (System.Exception e)
                {
#if UNITY_EDITOR
                    throw;
#else
                    Debug.Log($"{Name()}: Exception: {e.Message} Stack: {e.StackTrace}");
                    Debug.LogError("Error while sending visual representation or audio to other participants");
#endif
                }

            }

            protected class Stats : VRT.Core.BaseStats
            {
                public Stats(string name) : base(name) { }

                double statsTotalBytes = 0;
                double statsTotalPackets = 0;

                public void statsUpdate(int nBytes)
                {
 
                    statsTotalBytes += nBytes;
                    statsTotalPackets += 1;

                    if (ShouldOutput())
                    {
                        Output($"fps={statsTotalPackets / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / (statsTotalPackets == 0 ? 1 : statsTotalPackets))}");
                      }
                    if (ShouldClear())
                    {
                        Clear();
                        statsTotalBytes = 0;
                        statsTotalPackets = 0;
                    }
                }
            }

            protected Stats stats;
        }
 
        TCPPushThread[] pusherThreads;

        public TCPWriter(string _url, string fourcc, B2DWriter.DashStreamDescription[] _descriptions) : base(WorkerType.End)
        {
            if (_descriptions == null || _descriptions.Length == 0)
            {
                throw new System.Exception($"{Name()}: descriptions is null or empty");
            }
            if (fourcc.Length != 4)
            {
                throw new System.Exception($"{Name()}: 4CC is \"{fourcc}\" which is not exactly 4 characters");
            }
            uint fourccInt = bin2dash.VRT_4CC(fourcc[0], fourcc[1], fourcc[2], fourcc[3]);
            Uri url = new Uri(_url);
            TCPStreamDescription[] ourDescriptions = new TCPStreamDescription[_descriptions.Length];
            for(int i=0; i<_descriptions.Length; i++)
            {
                ourDescriptions[i] = new TCPStreamDescription
                {
                    host = url.Host,
                    port = url.Port + i,
                    fourcc = fourccInt,
                    inQueue = _descriptions[i].inQueue

                };
                Debug.Log($"{Name()}: xxxjack url={_url}, index={i}, host={ourDescriptions[i].host}, port={ourDescriptions[i].port}");
            }
            descriptions = ourDescriptions;
#if xxxjack_disabled
            try
            {
                //if (cfg.fileMirroring) bw = new BinaryWriter(new FileStream($"{Application.dataPath}/../{cfg.streamName}.dashdump", FileMode.Create));
                url = _url;
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(_streamName))
                {
                    Debug.LogError($"{Name()}: configuration error: url or streamName not set");
                    throw new System.Exception($"{Name()}: configuration error: url or streamName not set");
                }
                // xxxjack Is this the correct way to initialize an array of structs?
                Debug.Log($"xxxjack {Name()}: {descriptions.Length} output streams");
                bin2dash.StreamDesc[] b2dDescriptors = new bin2dash.StreamDesc[descriptions.Length];
                for (int i = 0; i < descriptions.Length; i++)
                {
                    b2dDescriptors[i] = new bin2dash.StreamDesc
                    {
                        MP4_4CC = fourccInt,
                        tileNumber = descriptions[i].tileNumber,
                        quality = descriptions[i].quality
                    };
                    if (descriptions[i].inQueue == null)
                    {
                        throw new System.Exception($"{Name()}.{i}: inQueue");
                    }
                }
                uploader = bin2dash.create(_streamName, b2dDescriptors, url, _segmentSize, _segmentLife);
                if (uploader != null)
                {
                    Debug.Log($"{Name()}: started {url + _streamName}.mpd");
                    Start();
                }
                else
                    throw new System.Exception($"{Name()}: vrt_create: failed to create uploader {url + _streamName}.mpd");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}({url}) Exception:{e.Message}");
                throw;
            }
#endif
            Start();
        }

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        protected override void Start()
        {
            base.Start();
            int nThreads = descriptions.Length;
            pusherThreads = new TCPPushThread[nThreads];
            for (int i = 0; i < nThreads; i++)
            {
                // Note: we need to copy i to a new variable, otherwise the lambda expression capture will bite us
                int stream_number = i;
                pusherThreads[i] = new TCPPushThread(this, descriptions[i]);
                BaseStats.Output(Name(), $"pusher={pusherThreads[i].Name()}, port={descriptions[i].port}");
            }
            foreach (var t in pusherThreads)
            {
                t.Start();
            }
        }

        public override void OnStop()
        {
            // Signal that no more data is forthcoming to every pusher
            for (int i = 0; i < descriptions.Length; i++)
            {
                var d = descriptions[i];
                if (!d.inQueue.IsClosed())
                {
                    Debug.LogWarning($"{Name()}.{i}: input queue not closed. Closing.");
                    d.inQueue.Close();
                }
            }
            // Stop our thread
            base.OnStop();
            // wait for pusherThreads to terminate
            foreach (var t in pusherThreads)
            {
                t.Stop();
                t.Join();
            }
            Debug.Log($"{Name()} Stopped");
        }

        protected override void Update()
        {
            base.Update();
            // xxxjack anything to do?
            System.Threading.Thread.Sleep(10);
        }
#if xxxjack_disabled
        public override SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);

            return new SyncConfig.ClockCorrespondence
            {
                wallClockTime = (long)sinceEpoch.TotalMilliseconds,
                streamClockTime = uploader.get_media_time(1000)
            };
        }
#endif
    }
}
