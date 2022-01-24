﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using VRT.Core;
using VRT.Transport.Dash;

namespace VRT.Transport.TCP
{

    public class BaseTCPReader : BaseReader
    {

        protected Uri url;
        public class ReceiverInfo
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

        public class TCPPullThread
        {
            BaseTCPReader parent;
            Socket socket = null;
            bool stopping = false;
            int thread_index;
            ReceiverInfo receiverInfo;
            System.Threading.Thread myThread;
            
            public TCPPullThread(BaseTCPReader _parent, int _thread_index, ReceiverInfo _receiverInfo)
            {
                parent = _parent;
                thread_index = _thread_index;
                receiverInfo = _receiverInfo;
                myThread = new System.Threading.Thread(run);
                myThread.Name = Name();
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
                            BaseStats.Output(Name(), $"connected=0, destination={remoteEndpoint.ToString()}");
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
                            BaseStats.Output(Name(), $"connected=1, destination={remoteEndpoint.ToString()}");
                            Debug.Log($"{Name()}: Connect({remoteEndpoint}) succeeded");
                        }
                        byte[] hdr = new byte[16];
                        int hdrSize = _ReceiveAll(socket, hdr);
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
                            Debug.LogWarning($"{Name()}: expected 4CC 0x{receiverInfo.fourcc:x} got 0x{fourccReceived:x}");
                        } 
                        int dataSize = BitConverter.ToInt32(hdr, 4);
                        long timestamp = BitConverter.ToInt64(hdr, 8);
                        byte[] data = new byte[dataSize];
                        int actualDataSize = _ReceiveAll(socket, data);
                        if (actualDataSize != dataSize)
                        {
                            Debug.LogWarning($"{Name()}: short data read ({actualDataSize} in stead of {dataSize}), closing socket");
                            socket.Close();
                            socket = null;
                            continue;
                        }

                        NativeMemoryChunk mc = new NativeMemoryChunk(dataSize);
                        mc.info.timestamp = timestamp;
                        System.Runtime.InteropServices.Marshal.Copy(data, 0, mc.pointer, dataSize);
                        var buf = new byte[mc.length];
                        System.Runtime.InteropServices.Marshal.Copy(mc.pointer, buf, 0, mc.length);
                        bool ok = receiverInfo.outQueue.Enqueue(mc);
                        stats.statsUpdate(dataSize, !ok);
                        // Close the socket if the portOffset (the quality index) has been changed in the mean time
                        if (socket != null && receiverInfo.portOffset != portOffset)
                        {
                            Debug.Log($"{Name()}: closing socket for quality switch");
                            socket.Close();
                            socket = null;
                        }
                    }
                }
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

            protected class Stats : VRT.Core.BaseStats
            {
                public Stats(string name) : base(name) { }

                System.DateTime statsConnectionStartTime;
                double statsTotalBytes;
                double statsTotalPackets;
                double statsDroppedPackets;
                
                public void statsUpdate(int nBytes, bool dropped)
                {
                    statsTotalBytes += nBytes;
                    statsTotalPackets++;
                    if (dropped) statsDroppedPackets++;
                    if (ShouldOutput())
                    {
                        Output($"fps={statsTotalPackets / Interval():F2}, fps_dropped={statsDroppedPackets / Interval():F2}, bytes_per_packet={(int)(statsTotalBytes / statsTotalPackets)}");
                     }
                    if (ShouldClear())
                    {
                        Clear();
                        statsTotalBytes = 0;
                        statsTotalPackets = 0;
                        statsDroppedPackets = 0;
                    }
                }
            }

            protected Stats stats;

        }
        TCPPullThread[] threads;

        SyncConfig.ClockCorrespondence clockCorrespondence; // Allows mapping stream clock to wall clock

        protected BaseTCPReader(string _url) : base()
        {
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

        public BaseTCPReader(string _url, string fourcc, QueueThreadSafe outQueue) : this(_url)
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
                        fourcc = bin2dash.VRT_4CC(fourcc[0], fourcc[1], fourcc[2], fourcc[3])
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

