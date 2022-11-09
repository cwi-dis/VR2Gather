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
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using QueueThreadSafe = Cwipc.QueueThreadSafe;

    public class AsyncTCPWriter : AsyncWriter
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

        TCPStreamDescription[] descriptions;

        public class TCPPushThread
        {
            AsyncTCPWriter parent;
            TCPStreamDescription description;
            System.Threading.Thread myThread;
            Socket listenSocket = null;
            Socket sendSocket = null;

            public TCPPushThread(AsyncTCPWriter _parent, TCPStreamDescription _description)
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
                    Socket tmp = listenSocket;
                    listenSocket = null;
                    tmp.Close();
                }
                if (sendSocket != null)
                {
                    Socket tmp = sendSocket;
                    sendSocket = null;
                    tmp.Close();
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
                                if (listenSocket != null)
                                {
                                    Debug.Log($"{Name()}: Accept: Exception {e.ToString()}");
                                }
                                continue;
                            }
                        }
                        NativeMemoryChunk mc = (NativeMemoryChunk)queue.Dequeue();
                        if (mc == null) continue; // Probably closing...
                        stats.statsUpdate(mc.length);
                        byte[] hdr = new byte[16];
                        var hdr1 = BitConverter.GetBytes((UInt32)description.fourcc);
                        hdr1.CopyTo(hdr, 0);
                        var hdr2 = BitConverter.GetBytes((Int32)mc.length);
                        hdr2.CopyTo(hdr, 4);
                        var hdr3 = BitConverter.GetBytes(mc.info.timestamp);
                        hdr3.CopyTo(hdr, 8);
                        var buf = new byte[mc.length];
                        System.Runtime.InteropServices.Marshal.Copy(mc.pointer, buf, 0, mc.length);
                        try
                        {
                            sendSocket.Send(hdr);
                            sendSocket.Send(buf);
                        }
                        catch (ObjectDisposedException)
                        {
                            if (sendSocket != null)
                            {
                                Debug.Log($"{Name()}: socket was closed by another thread");
                            }
                            sendSocket = null;
                            BaseStats.Output(Name(), $"open=0");
                        }
                        catch(SocketException e)
                        {
                            if (sendSocket != null)
                            {
                                Debug.Log($"{Name()}: socket exception: {e.ToString()}");
                                sendSocket.Close();
                                sendSocket = null;
                            }
                            BaseStats.Output(Name(), $"open=0");
                        }
                    }
                    Debug.Log($"{Name()}: thread stopped");
                }
#pragma warning disable CS0168
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
                int statsAggregatePackets = 0;

                public void statsUpdate(int nBytes)
                {
 
                    statsTotalBytes += nBytes;
                    statsTotalPackets++;
                    statsAggregatePackets++;

                    if (ShouldOutput())
                    {
                        Output($"fps={statsTotalPackets / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / (statsTotalPackets == 0 ? 1 : statsTotalPackets))}, aggregate_packets={statsAggregatePackets}");
                        Clear();
                        statsTotalBytes = 0;
                        statsTotalPackets = 0;
                    }
                }
            }

            protected Stats stats;
        }
 
        TCPPushThread[] pusherThreads;

        public AsyncTCPWriter(string _url, string fourcc, AsyncB2DWriter.DashStreamDescription[] _descriptions) : base()
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
            if (url.Scheme != "tcp" || url.Host == "" || url.Port <= 0)
            {
                throw new System.Exception($"{Name()}: TCP transport requires tcp://host:port/ URL, got \"{_url}\"");
            }
            TCPStreamDescription[] ourDescriptions = new TCPStreamDescription[_descriptions.Length];
            // We use the lowest ports for the first quality, for each tile.
            // The the next set of ports is used for the next quality, and so on.
            int maxTileNumber = -1;
            for(int i=0; i<_descriptions.Length; i++)
            {
                if (_descriptions[i].tileNumber > maxTileNumber) maxTileNumber = (int)_descriptions[i].tileNumber;
            }
            int portsPerQuality = maxTileNumber;
            if (portsPerQuality == 0) portsPerQuality = 1;
            for(int i=0; i<_descriptions.Length; i++)
            {
                ourDescriptions[i] = new TCPStreamDescription
                {
                    host = url.Host,
                    port = url.Port + (int)_descriptions[i].tileNumber + (portsPerQuality*_descriptions[i].qualityIndex),
                    fourcc = fourccInt,
                    inQueue = _descriptions[i].inQueue

                };
            }
            descriptions = ourDescriptions;
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
                BaseStats.Output(Name(), $"pusher={pusherThreads[i].Name()}, stream={i}, port={descriptions[i].port}");
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

#if xxxjack_disabled
        protected override void Update()
        {
            base.Update();
            // xxxjack anything to do?
            System.Threading.Thread.Sleep(10);
        }

        public override SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);

            return new SyncConfig.ClockCorrespondence
            {
                wallClockTime = (Timestamp)sinceEpoch.TotalMilliseconds,
                streamClockTime = uploader.get_media_time(1000)
            };
        }
#endif
    }
}
