using VRT.Core;
using Cwipc;
using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using UnityEngine;

namespace VRT.Transport.TCPSFU
{
    public class TransportProtocolTCPSFU : TransportProtocol
    {
       
        private static TransportProtocolTCPSFU _Instance;
        private static string _InstanceURL;

        private Socket Sock;
        private HashSet<string> OutgoingStreams;
        private Queue<byte[]> OutgoingQueue;
        private Thread OutgoingThread;

        private Dictionary<string, QueueThreadSafe> IncomingQueues;
        private Thread IncomingThread;

        public static void Register()
        {
            RegisterTransportProtocol("tcpsfu", AsyncTCPSFUWriter.Factory, AsyncTCPSFUReader.Factory, AsyncTCPSFUReader.Factory_Tiled);
        }


        public static TransportProtocolTCPSFU Connect(string url)
        {
            if (_Instance == null)
            {
                _InstanceURL = url;
                _Instance = new TransportProtocolTCPSFU(url);
                return _Instance;
            }
            if (_InstanceURL == url)
            {
                return _Instance;
            }
            throw new System.Exception($"TransportProtocolTCPSFU: request connection to {url} but {_InstanceURL} already connected");
        }

        string Name()
        {
            return "TransportProtocolTCPSFU";
        }

        TransportProtocolTCPSFU(string url)
        {
            Uri tmp = new Uri(url);
            string host = tmp.Host;
            int port = tmp.Port;
            Sock = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Sock.Connect(host, port);
            OutgoingThread = new Thread(OutgoingRun);
            IncomingThread = new Thread(IncomingRun);
        }

        public void Stop()
        {
            Sock.Close();
            Sock = null;
            OutgoingThread.Abort();
            IncomingThread.Abort();
            OutgoingQueue = null;
            IncomingQueues = null;
        }

        public void RegisterOutgoingStream(string streamName)
        {
            OutgoingStreams.Add(streamName);
        }

        public void UnregisterOutgoingStream(string streamName)
        {
            OutgoingStreams.Remove(streamName);
            if (OutgoingStreams.Count == 0 && IncomingQueues.Count == 0)
            {
                Stop();
            }
        }

        public void RegisterIncomingStream(string streamName, QueueThreadSafe outQueue)
        {
            IncomingQueues[streamName] = outQueue;
        }

        public void UnregisterIncomingStream(string streamName)
        {
            IncomingQueues.Remove(streamName);
            if (OutgoingStreams.Count == 0 && IncomingQueues.Count == 0)
            {
                Stop();
            }
        }

        public void SendChunk(BaseMemoryChunk chk, string streamName)
        {
            if (Sock == null)
            {
                Debug.Log($"{Name()}: No socket, dropping.");
                return;
            }
            string header = $"{streamName},{chk.metadata.timestamp},{chk.length}\n";
            byte[] b_header = Encoding.UTF8.GetBytes(header);
            if (b_header.Length > 64)
            {
                Debug.LogError($"{Name()}: header size {b_header.Length} greater than 64. Dropping.");
                return;
            }
            Array.Resize(ref b_header, 64);
            int totalLength = b_header.Length + chk.length;
            var buf = new byte[totalLength];
            Array.Copy(b_header, buf, b_header.Length);
            System.Runtime.InteropServices.Marshal.Copy(chk.pointer, buf, b_header.Length, chk.length);
            OutgoingQueue.Enqueue(buf);
        }

        private bool decodeHeader(string header, out string streamName, out long timestamp, out int dataLength)
        {
            string[] lines = header.Split('\n');
            string[] words = lines[0].Split(',');
            if (lines.Length != 2 || words.Length != 3)
            {
                Debug.LogWarning($"{Name()}:Bad header: {header}");
                streamName = "";
                timestamp = 0;
                dataLength = 0;
                return false;
            }
            streamName = words[0];
            timestamp = long.Parse(words[1]);
            dataLength = int.Parse(words[2]);
            return true;
        }

        private void OutgoingRun()
        {
            while (Sock != null)
            {
                byte[] packet = OutgoingQueue.Dequeue();
                Sock.Send(packet);
            }
        }

        private string _ReadHeader()
        {
            byte[] b_header = new byte[64];
            int actualSize = Sock.Receive(b_header);
            if (actualSize != 64)
            {
                Debug.LogError($"{Name()}: Received short header, {actualSize} bytes");
                return null;
            }
            int lf_pos = 0;
            while (b_header[lf_pos] != (byte)'\n')
            {
                lf_pos++;
            }
            return Encoding.UTF8.GetString(b_header, 0, lf_pos);
        }

        private void IncomingRun()
        {
            while (Sock != null)
            {
                string header = _ReadHeader();
                if (header == null)
                {
                    break;
                }
                string streamName;
                long timeStamp;
                int dataLength;
                bool ok = decodeHeader(header, out streamName, out timeStamp, out dataLength);
                if (!ok)
                {
                    break;
                }
                // We always want to read the data, even if we don't want it
                byte[] data = new byte[dataLength];
                int dataLengthGotten = Sock.Receive(data);
                if (dataLengthGotten != dataLength)
                {
                    Debug.LogError($"{Name()}: Received {dataLengthGotten} bytes in stead of {dataLength}");
                    break;
                }
                if (IncomingQueues.ContainsKey(streamName))
                {
                    QueueThreadSafe queue = IncomingQueues[streamName];
                    BaseMemoryChunk chunk = new NativeMemoryChunk(dataLength);
                    chunk.metadata.timestamp = timeStamp;
                    System.Runtime.InteropServices.Marshal.Copy(data, sizeof(long), chunk.pointer, chunk.length);
                    bool didDrop = queue.Enqueue(chunk);
                    // xxxjack should add Stats
                }
            }
        }
    }    
}
