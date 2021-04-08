using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using VRT.Core;
using VRT.Transport.Dash;

namespace VRT.Transport.TCP
{

    public class BaseTCPReader : BaseReader
    {

        protected Uri url;
#if xxxjack_disabled
        protected int streamCount;
        protected uint[] stream4CCs;
        protected sub.connection subHandle;
        protected bool isPlaying;
        protected int frequency=20;
        int numberOfUnsuccessfulReceives;
        //        object subLock = new object();
        System.DateTime subRetryNotBefore = System.DateTime.Now;
        System.TimeSpan subRetryInterval = System.TimeSpan.FromSeconds(5);
#endif
        public class ReceiverInfo
        {
            public QueueThreadSafe outQueue;
            public string host;
            public int port;
            public object tileDescriptor;
            public int tileNumber = -1;
        }
        protected ReceiverInfo[] receivers;
        //        protected QueueThreadSafe[] outQueues;
        //        protected int[] streamIndexes;

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public class TCPPullThread
        {
            BaseTCPReader parent;
            Socket socket = null;
            //            int stream_index;
            //            QueueThreadSafe outQueue;
            bool stopping = false;
            int thread_index;
            ReceiverInfo receiverInfo;
            int frequency = 20;
            System.Threading.Thread myThread;
            System.DateTime lastSuccessfulReceive;
            System.TimeSpan maxNoReceives = System.TimeSpan.FromSeconds(5);
            System.TimeSpan receiveInterval = System.TimeSpan.FromMilliseconds(2); // xxxjack maybe too aggressive for PCs and video?

            public TCPPullThread(BaseTCPReader _parent, int _thread_index, ReceiverInfo _receiverInfo)
            {
                parent = _parent;
                thread_index = _thread_index;
                receiverInfo = _receiverInfo;
                myThread = new System.Threading.Thread(run);
                myThread.Name = Name();
                lastSuccessfulReceive = System.DateTime.Now;
                stats = new Stats(Name());
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

            protected void run()
            {
                bool bCanQueue = (frequency==0); // If frequency is 0 enqueue at the very begining
                // Create a stopwatch to measure the time.
                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();
                System.TimeSpan oldElapsed = System.TimeSpan.Zero;
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
                            IPEndPoint remoteEndpoint = new IPEndPoint(ipAddress, receiverInfo.port);
                            BaseStats.Output(Name(), $"connected=0, destination={remoteEndpoint.ToString()}");
                            socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            Debug.Log($"{Name()}: xxxjack connect to {remoteEndpoint.ToString()}");
                            try
                            {
                                socket.Connect(remoteEndpoint);
                            }
                            catch(SocketException e)
                            {
                                Debug.Log($"{Name()}: Connect({remoteEndpoint}) failed: {e.ToString()}");
                                socket = null;
                                System.Threading.Thread.Sleep(1000);
                                continue;
                            }
                            BaseStats.Output(Name(), $"connected=1, destination={remoteEndpoint.ToString()}");
                            Debug.Log($"{Name()}: Connect({remoteEndpoint}) succeeded");
                        }
                        byte[] hdr = new byte[8];
                        Debug.Log($"{Name()}: xxxjack  start receive header 8 bytes");
                        int hdrSize = socket.Receive(hdr);
                        Debug.Log($"{Name()}: xxxjack receive header returned {hdrSize} bytes");
                        if (hdrSize != 8)
                        {
                            Debug.Log($"{Name()}: short header read ({hdrSize} in stead of {hdr.Length}), closing socket");
                            socket.Close();
                            socket = null;
                            continue;
                        }
                        int dataSize = BitConverter.ToInt32(hdr, 4);
                        byte[] data = new byte[dataSize];
                        Debug.Log($"{Name()}: xxxjack start receive data {dataSize} bytes");
                        int actualDataSize = socket.Receive(data);
                        Debug.Log($"{Name()}: xxxjack receive data returned {actualDataSize} bytes");
                        if (actualDataSize != dataSize)
                        {
                            Debug.Log($"{Name()}: short data read ({actualDataSize} in stead of {dataSize}), closing socket");
                            socket.Close();
                            socket = null;
                            continue;
                        }

                        NativeMemoryChunk mc = new NativeMemoryChunk(dataSize);
                        System.Runtime.InteropServices.Marshal.Copy(data, 0, mc.pointer, dataSize);
                        var buf = new byte[mc.length];
                        System.Runtime.InteropServices.Marshal.Copy(mc.pointer, buf, 0, mc.length);
                        receiverInfo.outQueue.Enqueue(mc);
#if xxxjack_disabled
                        //
                        // We have work to do. 
                        //
                        FrameInfo frameInfo = new FrameInfo();
                        int stream_index = receiverInfo.curStreamIndex;
                        int bytesNeeded = 0;
                       
                        // See whether data is available on this stream, and how many bytes we need to allocate
                        bytesNeeded = subHandle.grab_frame(stream_index, System.IntPtr.Zero, 0, ref frameInfo);
  

                        // If no data is available we may want to close the subHandle, or sleep a bit
                        if (bytesNeeded == 0)
                        {
                            subHandle.free();
                            System.TimeSpan noReceives = System.DateTime.Now - lastSuccessfulReceive;
                            if (noReceives > maxNoReceives)
                            {
                                Debug.LogWarning($"{Name()}: No data received for {noReceives.TotalSeconds} seconds, closing subHandle");
                                parent.playFailed();
                                return;
                            }
                            System.Threading.Thread.Sleep(receiveInterval);
                            continue;
                        }

                        lastSuccessfulReceive = System.DateTime.Now;

                        // Allocate and read.
                        NativeMemoryChunk mc = new NativeMemoryChunk(bytesNeeded);
                        int bytesRead = subHandle.grab_frame(stream_index, mc.pointer, mc.length, ref frameInfo);

                        // We no longer need subHandle
                        subHandle.free();

                        if (bytesRead != bytesNeeded)
                        {
                            Debug.LogError($"{Name()}: programmer error: sub_grab_frame returned {bytesRead} bytes after promising {bytesNeeded}");
                            mc.free();
                            continue;
                        }


                        // If we have no clock correspondence yet we use the first received frame on any stream to set it
                        if (parent.clockCorrespondence.wallClockTime == 0)
                        {
                            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                            parent.clockCorrespondence.wallClockTime = (long)sinceEpoch.TotalMilliseconds;
                            parent.clockCorrespondence.streamClockTime = frameInfo.timestamp;
                            BaseStats.Output(parent.Name(), $"stream_timestamp={parent.clockCorrespondence.streamClockTime}, timestamp={parent.clockCorrespondence.wallClockTime}, delta={parent.clockCorrespondence.wallClockTime-parent.clockCorrespondence.streamClockTime}");
                        }
                        // Convert clock values to wallclock
                        frameInfo.timestamp = frameInfo.timestamp - parent.clockCorrespondence.streamClockTime + parent.clockCorrespondence.wallClockTime;
                        mc.info = frameInfo;
                        stats.statsUpdate(bytesRead, frameInfo.timestamp, stream_index);
                        // xxxjack we should investigate the following code (and its history). It looks
                        // like some half-way attempt to lower latency, but unsure.
                        // Check if can start to enqueue
                        if (!bCanQueue) {
                            receiverInfo.outQueue.Enqueue(mc);
                        } else { 
                            // Check time btween last package.
                            System.TimeSpan newElapsed = stopWatch.Elapsed;
                            // If is not the first and the time is greater or igual than frequency start to enqueue
                            if (oldElapsed != System.TimeSpan.Zero && (newElapsed - oldElapsed).TotalMilliseconds >= frequency) {
                                bCanQueue = true;
                                // Enqueue the first chunk
                                receiverInfo.outQueue.Enqueue(mc);
                            } else {
                                // if not, release data an save the elapsed time.
                                oldElapsed = newElapsed;
                                mc.free();
                            }
                        }
#endif
                    }
                }
                catch (System.Exception e)
                {
#if UNITY_EDITOR
                    throw;
#else
                    Debug.Log($"{Name()}: Exception: {e.Message} Stack: {e.StackTrace}");
                    Debug.LogError("Error while receiving visual representation or audio from another participant");
#endif
                }

                stopWatch.Stop();
            }

            protected class Stats : VRT.Core.BaseStats
            {
                public Stats(string name) : base(name) { }

                System.DateTime statsConnectionStartTime;
                double statsTotalBytes;
                double statsTotalPackets;
                double statsTotalLatency;
                bool statsGotFirstReception;

                public void statsUpdate(int nBytes, long timeStamp, int stream_index)
                {
                    if (!statsGotFirstReception)
                    {
                        statsConnectionStartTime = System.DateTime.Now;
                        statsGotFirstReception = true;
                    }
      
                    System.TimeSpan sinceEpoch = System.DateTime.Now - statsConnectionStartTime;
                    double latency = (sinceEpoch.TotalMilliseconds - timeStamp) / 1000.0;
                    // Unfortunately we don't know the _real_ connection start time (because it is on the sender end)
                    // if we appear to be ahead we adjust connection start time.
                    if (latency < 0)
                    {
                        statsConnectionStartTime -= System.TimeSpan.FromMilliseconds(-latency);
                        latency = 0;
                    }
                    statsTotalLatency += latency;
                    statsTotalBytes += nBytes;
                    statsTotalPackets++;
                    if (ShouldOutput())
                    {
                        int msLatency = (int)(1000 * statsTotalLatency / statsTotalPackets);
                        Output($"fps={statsTotalPackets / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / statsTotalPackets)}, latency_lowerbound_ms={msLatency}, stream_index={stream_index}");
                     }
                    if (ShouldClear())
                    {
                        Clear();
                        statsTotalBytes = 0;
                        statsTotalPackets = 0;
                        statsTotalLatency = 0;
                    }
                }
            }

            protected Stats stats;

        }
        TCPPullThread[] threads;

        SyncConfig.ClockCorrespondence clockCorrespondence; // Allows mapping stream clock to wall clock

        protected BaseTCPReader(string _url) : base(WorkerType.Init)
        {
            Debug.Log($"{Name()}: xxxjack url={_url}");
            lock (this)
            {
                joinTimeout = 20000;

                if (_url == "" || _url == null)
                {
                    Debug.LogError($"{Name()}: configuration error: url not set");
                    throw new System.Exception($"{Name()}: configuration error: url not set");
                }
                url = new Uri(_url);
                if (url.Host == "" || url.Port == 0)
                {
                    Debug.LogError($"{Name()}: configuration error: url misses host or port");
                    throw new System.Exception($"{Name()}: configuration error: url misses host or port");
                }

            }
        }

        public BaseTCPReader(string _url, QueueThreadSafe outQueue) : this(_url)
        {
            lock (this)
            {
                receivers = new ReceiverInfo[]
                {
                    new ReceiverInfo()
                    {
                        outQueue = outQueue,
                        host = url.Host,
                        port = url.Port
                    },
                };
                Start();

            }
        }

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        public override void Stop()
        {
            if (debugThreading) Debug.Log($"{Name()}: Stop");
            base.Stop();
            _closeQueues();
        }

        protected override void Start()
        {
            base.Start();
            InitThreads();
        }

        public override void OnStop()
        {
            if (debugThreading) Debug.Log($"{Name()}: Stopping");
            foreach(var t in threads)
            {
                t.Stop();
                t.Join();
            }
            base.OnStop();
            if (debugThreading) Debug.Log($"{Name()}: Stopped");
        }
#if xxxjack_disabled

        protected virtual void _streamInfoAvailable()
        {
            lock (this)
            {
                //
                // Get stream information
                //
                streamCount = subHandle.get_stream_count();
                stream4CCs = new uint[streamCount];
                for (int i = 0; i < streamCount; i++)
                {
                    stream4CCs[i] = subHandle.get_stream_4cc(i);
                }
                Debug.Log($"{Name()}: sub.play({url}) successful, {streamCount} streams.");
            }
        }

        protected bool InitDash()
        {
            lock (this)
            {
                if (System.DateTime.Now < subRetryNotBefore)
                {
                    return false;
                }
                subRetryNotBefore = System.DateTime.Now + subRetryInterval;
                //
                // Create SUB instance
                //
                if (subHandle != null)
                {
                    Debug.LogError($"{Name()}: Programmer error: InitDash() called but subHandle != null");
                }
                sub.connection newSubHandle = sub.create(Name());
                if (newSubHandle == null) throw new System.Exception($"{Name()}: sub_create() failed");
                Debug.Log($"{Name()}: retry sub.create() successful.");
                //
                // Start playing
                //
                isPlaying = newSubHandle.play(url);
                if (!isPlaying)
                {
                    subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(5);
                    Debug.Log($"{Name()}: sub.play({url}) failed, will try again later");
                    newSubHandle.free();
                    return false;
                }
                subHandle = newSubHandle;
                //
                // Stream information is available. Allow subclasses to act on it to reconfigure.
                //
                _streamInfoAvailable();
                Debug.Log($"{Name()}: sub.play({url}) successful, {streamCount} streams.");
                return true;
            }
        }
#endif
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
                    BaseStats.Output(Name(), msg);
                }
                foreach (var t in threads)
                {
                    t.Start();
                }
            }
        }
#if xxxjack_disabled
        private void _DeinitDash(bool closeQueues)
        {
            lock (this)
            {
                subHandle?.free();
                subHandle = null;
                isPlaying = false;
            }
            if (closeQueues) _closeQueues();
            if (threads == null) return;
            foreach (var t in threads)
            {
                t.Join();
            }
            threads = null;
        }
#endif
        private void _closeQueues()
        {
            foreach (var r in receivers)
            {
                var oq = r.outQueue;
                if (!oq.IsClosed()) oq.Close();
            }
        }

#if xxxjack_disabled
        protected override void Update()
        {
            base.Update();
            lock (this)
            {
                // If we should stop playing we stop

                if (!isPlaying)
                {
                    _DeinitDash(false);
                }
                // If we are not playing we start
                if (subHandle == null)
                {
                    if (InitDash())
                    {
                        InitThreads();
                    }
                }
            }
        }

        public override void SetSyncInfo(SyncConfig.ClockCorrespondence _clockCorrespondence)
        {
            clockCorrespondence = _clockCorrespondence;
        }
#endif
    }
}

