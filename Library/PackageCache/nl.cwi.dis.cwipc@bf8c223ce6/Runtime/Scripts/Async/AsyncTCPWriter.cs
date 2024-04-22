using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using Cwipc;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;

    /// <summary>
    /// Class that writes frames over TCP using a very simple protocol.
    /// This object (the writer) is the server side, <seealso cref="AsyncTCPReader"/> for
    /// the receiver side and for a description of the no-frills protocol used.
    ///
    /// The class supports sending tiled streams, by creating multiple servers (on increasing port numbers).
    /// </summary>
    public class AsyncTCPWriter : AsyncWriter
    {
   

        protected struct TCPStreamDescription
        {
            public string host;
            public int port;
            public uint fourcc;
            public QueueThreadSafe inQueue;
        };

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        protected TCPStreamDescription[] descriptions;

        protected class TCPPushThread
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
#if VRT_WITH_STATS
                stats = new Stats(Name());
#endif
                Debug.Log($"{Name()}: serving on tcp://{description.host}:{description.port} 4cc={description.fourcc:X}");
                IPAddress[] all = Dns.GetHostAddresses(description.host);
                all = Array.FindAll(all, a => a.AddressFamily == AddressFamily.InterNetwork);
                IPAddress ipAddress = all[0];
                IPEndPoint localEndpoint = new IPEndPoint(ipAddress, description.port);
                listenSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(localEndpoint);
                listenSocket.Listen(4);
#if CWIPC_WITH_LOGGING
                Debug.Log($"{Name()}: Start server on ({localEndpoint})");
#endif
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
#if VRT_WITH_STATS
                            Statistics.Output(Name(), $"open=0, listen=1");
#endif
                            try
                            {
                                sendSocket = listenSocket.Accept();
#if VRT_WITH_STATS
                                Statistics.Output(Name(), $"open=1, remote={sendSocket.RemoteEndPoint.ToString()}");
#endif
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
#if VRT_WITH_STATS
                        stats.statsUpdate(mc.length);
#endif
                        byte[] hdr = new byte[16];
                        var hdr1 = BitConverter.GetBytes((UInt32)description.fourcc);
                        hdr1.CopyTo(hdr, 0);
                        var hdr2 = BitConverter.GetBytes((Int32)mc.length);
                        hdr2.CopyTo(hdr, 4);
                        var hdr3 = BitConverter.GetBytes(mc.metadata.timestamp);
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
#if VRT_WITH_STATS
                            Statistics.Output(Name(), $"open=0");
#endif
                        }
                        catch(SocketException e)
                        {
                            if (sendSocket != null)
                            {
                                Debug.Log($"{Name()}: socket exception: {e.ToString()}");
                                sendSocket.Close();
                                sendSocket = null;
                            }
#if VRT_WITH_STATS
                            Statistics.Output(Name(), $"open=0");
#endif
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

#if VRT_WITH_STATS
            protected class Stats : Statistics
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
#endif
        }
 
        TCPPushThread[] pusherThreads;

        protected AsyncTCPWriter() : base()
        {
           
        }

        /// <summary>
        /// Create a frame server (or a set of frame server for multi-tile usage).
        /// The URL should be of the form tcp://host:port with host being the name of the local
        /// interface on which the server should serve. Can be 0.0.0.0 on most operating systems.
        /// </summary>
        /// <param name="_url">Where the server should ser on</param>
        /// <param name="fourcc">4CC media type</param>
        /// <param name="_descriptions">Array of stream descriptions</param>
        public AsyncTCPWriter(string _url, string fourcc, OutgoingStreamDescription[] _descriptions) : base()
        {
            NoUpdateCallsNeeded();
            if (_descriptions == null || _descriptions.Length == 0)
            {
                throw new System.Exception($"{Name()}: descriptions is null or empty");
            }
            if (fourcc.Length != 4)
            {
                throw new System.Exception($"{Name()}: 4CC is \"{fourcc}\" which is not exactly 4 characters");
            }
            uint fourccInt = StreamSupport.VRT_4CC(fourcc[0], fourcc[1], fourcc[2], fourcc[3]);
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
            int portsPerQuality = maxTileNumber+1;
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
#if VRT_WITH_STATS
                Statistics.Output(base.Name(), $"pusher={pusherThreads[i].Name()}, stream={i}, port={descriptions[i].port}");
#endif
            }
            foreach (var t in pusherThreads)
            {
                t.Start();
            }
        }

        public override void AsyncOnStop()
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
            base.AsyncOnStop();
            // wait for pusherThreads to terminate
            foreach (var t in pusherThreads)
            {
                t.Stop();
                t.Join();
            }
            Debug.Log($"{Name()} Stopped");
        }

        protected override void AsyncUpdate()
        {
        }

#if xxxjack_disabled

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
