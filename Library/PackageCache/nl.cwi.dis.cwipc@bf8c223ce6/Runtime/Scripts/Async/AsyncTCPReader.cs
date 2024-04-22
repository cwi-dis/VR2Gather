using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Cwipc;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
   
    /// <summary>
    /// Implementation of AsyncReader that connects to a TCP socket on a remote machine and receives
    /// frames from that socket using a very simple protocol:
    /// Each frame starts with a 16-byte header: 4 bytes 4CC, 4 bytes frame data length and 8 bytes timestamp. No
    /// endianness conversion is done. After that we simply transmit the frame data bytes.
    /// <seealso cref="AsyncTCPWriter"/>
    /// </summary>
    public class AsyncTCPReader : AsyncReader
    {

        protected Uri url;
        protected class ReceiverInfo
        {
            public QueueThreadSafe outQueue;
            public string host;
            public int port;
            public int portOffset = 0;
            public object tileDescriptor;
            public int tileNumber = -1;
            public uint fourcc;
        }
        protected ReceiverInfo[] receivers;
   
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }
        protected class TCPPullThread
        {
            AsyncTCPReader parent;
            Socket socket = null;
            bool stopping = false;
            int thread_index;
            ReceiverInfo receiverInfo;
            System.Threading.Thread myThread;
            
            public TCPPullThread(AsyncTCPReader _parent, int _thread_index, ReceiverInfo _receiverInfo)
            {
                parent = _parent;
                thread_index = _thread_index;
                receiverInfo = _receiverInfo;
                myThread = new System.Threading.Thread(run);
                myThread.Name = Name();
#if VRT_WITH_STATS
                stats = new Stats(Name());
#endif
                Debug.Log($"{Name()}: connecting to tcp://{receiverInfo.host}:{receiverInfo.port} 4cc={receiverInfo.fourcc:X}");
            }

            public string Name()
            {
                return $"{parent.Name()}.{thread_index}";
            }

            public void Start()
            {
                myThread.Start();
            }

            public void Stop() {
                stopping = true;
                if (socket != null)
                {
                    socket.Close();
                }
                socket = null;

            }

            public void Join()
            {
                myThread.Join();
            }

            protected int _ReceiveAll(Socket sock, byte[] buffer)
            {
                int wanted = buffer.Length;
                int got = 0;
                while (wanted > 0)
                {
                    int curGot = sock.Receive(buffer, got, wanted, SocketFlags.None);
                    if (curGot <= 0) return got;
                    got += curGot;
                    wanted -= curGot;
                }
                return got;
            }

            protected void run()
            {
                int portOffset = 0;
                try
                {
                    while (!stopping)
                    {
                        //
                        // First check whether we should terminate, and otherwise whether we have nay work to do currently.
                        //
                        if (receiverInfo == null || receiverInfo.outQueue.IsClosed())
                        {
                            return;
                        }

                        if (socket == null)
                        {
                            IPAddress[] all = Dns.GetHostAddresses(receiverInfo.host);
                            all = Array.FindAll(all, a => a.AddressFamily == AddressFamily.InterNetwork);
                            IPAddress ipAddress = all[0];
                            portOffset = receiverInfo.portOffset;
                            IPEndPoint remoteEndpoint = new IPEndPoint(ipAddress, receiverInfo.port + portOffset);
#if VRT_WITH_STATS
                            Statistics.Output(Name(), $"connected=0, destination={remoteEndpoint.ToString()}");
#endif
                            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            try
                            {
                                socket.Connect(remoteEndpoint);
                            }
                            catch(SocketException e)
                            {
                                Debug.LogWarning($"{Name()}: Connect({remoteEndpoint}) failed: {e.ToString()}. Sleep 1 second.");
                                socket = null;
                                System.Threading.Thread.Sleep(1000);
                                continue;
                            }
#if VRT_WITH_STATS
                            Statistics.Output(Name(), $"connected=1, destination={remoteEndpoint.ToString()}");
#endif
                            Debug.Log($"{Name()}: Connect({remoteEndpoint}) succeeded");
                        }
                        System.DateTime receiveStartTime = System.DateTime.Now;
                        byte[] hdr = new byte[16];
                        int hdrSize = _ReceiveAll(socket, hdr);
                        System.DateTime receiveMidTime = System.DateTime.Now;
                        if (hdrSize != 16)
                        {
                            Debug.LogWarning($"{Name()}: short header read ({hdrSize} in stead of {hdr.Length}), closing socket");
                            socket.Close();
                            socket = null;
                            continue;
                        }
                        // Check fourcc
                        int fourccReceived = BitConverter.ToInt32(hdr, 0);
                        if (fourccReceived != receiverInfo.fourcc)
                        {
                            Debug.LogError($"{Name()}: expected 4CC 0x{receiverInfo.fourcc:x} got 0x{fourccReceived:x}");
                        } 
                        int dataSize = BitConverter.ToInt32(hdr, 4);
                        Timestamp timestamp = BitConverter.ToInt64(hdr, 8);
                        byte[] data = new byte[dataSize];
                        int actualDataSize = _ReceiveAll(socket, data);
                        if (actualDataSize != dataSize)
                        {
                            if (actualDataSize != 0) Debug.LogWarning($"{Name()}: short data read ({actualDataSize} in stead of {dataSize}), closing socket");
                            socket.Close();
                            socket = null;
                            continue;
                        }
                        System.DateTime receiveStopTime = System.DateTime.Now;
                        Timedelta receiveDuration = (Timedelta)(receiveStopTime - receiveMidTime).TotalMilliseconds;

                        NativeMemoryChunk mc = new NativeMemoryChunk(dataSize);
                        mc.metadata.timestamp = timestamp;
                        System.Runtime.InteropServices.Marshal.Copy(data, 0, mc.pointer, dataSize);
                        var buf = new byte[mc.length];
                        System.Runtime.InteropServices.Marshal.Copy(mc.pointer, buf, 0, mc.length);
                        bool ok = receiverInfo.outQueue.Enqueue(mc);
#if VRT_WITH_STATS
                        stats.statsUpdate(dataSize, receiveDuration, !ok);
#endif
                        // Close the socket if the portOffset (the quality index) has been changed in the mean time
                        if (socket != null && receiverInfo.portOffset != portOffset)
                        {
                            Debug.Log($"{Name()}: closing socket for quality switch");
                            socket.Close();
                            socket = null;
                        }
                    }
                }
#pragma warning disable CS0168
                catch (System.Exception e)
                {
                    if (!stopping)
                    {
#if UNITY_EDITOR
                        throw;
#else
                        Debug.Log($"{Name()}: Exception: {e.Message} Stack: {e.StackTrace}");
                        Debug.LogError("Error while receiving visual representation or audio from another participant");
#endif
                    }
                }

            }

#if VRT_WITH_STATS
            protected class Stats : Statistics
            {
                public Stats(string name) : base(name) { }

                System.DateTime statsConnectionStartTime;
                double statsTotalBytes;
                double statsTotalDuration;
                double statsTotalPackets;
                int statsAggregatePackets;
                double statsDroppedPackets;
                
                public void statsUpdate(int nBytes, Timedelta duration, bool dropped)
                {
                    statsTotalBytes += nBytes;
                    statsTotalDuration += duration;
                    statsTotalPackets++;
                    statsAggregatePackets++;
                    if (dropped) statsDroppedPackets++;
                    if (ShouldOutput())
                    {
                        Output($"fps={statsTotalPackets / Interval():F2}, fps_dropped={statsDroppedPackets / Interval():F2}, receive_ms={(int)(statsTotalDuration/statsTotalPackets)}, receive_bandwidth={(int)(statsTotalBytes/Interval())}, bytes_per_packet={(int)(statsTotalBytes / statsTotalPackets)}, aggregate_packets={statsAggregatePackets}");
                        Clear();
                        statsTotalBytes = 0;
                        statsTotalDuration = 0;
                        statsTotalPackets = 0;
                        statsDroppedPackets = 0;
                    }
                }
            }

            protected Stats stats;
#endif
        }
        TCPPullThread[] threads;

        SyncConfig.ClockCorrespondence clockCorrespondence; // Allows mapping stream clock to wall clock

        /// <summary>
        /// Constructor that can be used by subclasses to create a multi-tile receiver.
        /// The URL should be of the form tcp://host:port. Subsequent streams will increment the port number.
        /// The subclass could initialize the receivers array and call Start().
        /// </summary>
        /// <param name="_url">The base URL for the streams</param>
        protected AsyncTCPReader(string _url) : base()
        {
            NoUpdateCallsNeeded();
            lock (this)
            {
                joinTimeout = 20000;

                if (_url == "" || _url == null)
                {
                    throw new System.Exception($"{Name()}: TCP transport requires tcp://host:port/ URL, but no URL specified");
                }
                url = new Uri(_url);
                if (url.Scheme != "tcp" || url.Host == "" || url.Port <= 0)
                {
                    throw new System.Exception($"{Name()}: TCP transport requires tcp://host:port/ URL, got \"{_url}\"");
                }
                if (url.Host == "" || url.Port == 0)
                {
                    Debug.LogError($"{Name()}: configuration error: url misses host or port");
                    throw new System.Exception($"{Name()}: configuration error: url misses host or port");
                }

            }
        }

        /// <summary>
        /// Create a TCP reader (client).
        /// The URL should be of the form tcp://host:post.
        /// </summary>
        /// <param name="_url">The server to connect to</param>
        /// <param name="fourcc">The 4CC of the frames expected on the stream</param>
        /// <param name="outQueue">The queue into which received frames will be deposited</param>
        public AsyncTCPReader(string _url, string fourcc, QueueThreadSafe outQueue) : this(_url)
        {
            lock (this)
            {
                receivers = new ReceiverInfo[]
                {
                    new ReceiverInfo()
                    {
                        outQueue = outQueue,
                        host = url.Host,
                        port = url.Port,
                        fourcc = StreamSupport.VRT_4CC(fourcc[0], fourcc[1], fourcc[2], fourcc[3])
                    },
                };
                Start();

            }
        }

        public override void Stop()
        {
            base.Stop();
            _closeQueues();
        }

        protected override void Start()
        {
            base.Start();
            InitThreads();
        }

        public override void AsyncOnStop()
        {
            if (debugThreading) Debug.Log($"{Name()}: Stopping threads");
            foreach(var t in threads)
            {
                t.Stop();
                t.Join();
            }
            base.AsyncOnStop();
        }

        protected override void AsyncUpdate()
        {
        }

        protected void InitThreads()
        {
            lock (this)
            {
                int threadCount = receivers.Length;
                threads = new TCPPullThread[threadCount];
                for (int i = 0; i < threadCount; i++)
                {
                    threads[i] = new TCPPullThread(this, i, receivers[i]);
                    string msg = $"pull_thread={threads[i].Name()}";
                    if (receivers[i].tileNumber >= 0)
                    {
                        msg += $", tile={receivers[i].tileNumber}";
                    }
#if VRT_WITH_STATS
                    Statistics.Output(base.Name(), msg);
#endif
                }
                foreach (var t in threads)
                {
                    t.Start();
                }
            }
        }
        private void _closeQueues()
        {
            foreach (var r in receivers)
            {
                var oq = r.outQueue;
                if (!oq.IsClosed()) oq.Close();
            }
        }
    }
}

