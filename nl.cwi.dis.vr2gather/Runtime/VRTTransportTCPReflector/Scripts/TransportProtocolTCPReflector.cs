using VRT.Core;
using Cwipc;
using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Net.Sockets;
using UnityEngine;

namespace VRT.Transport.TCPReflector
{
    public class TransportProtocolTCPReflector : TransportProtocol
    {
        /// <summary>
        /// Transport protocol that uses a TCP reflector, which simply sends any incoming packets
        /// back out on all connections (except the connection it came in on).
        /// 
        /// Packets are a 64 byte ASCII/UTF8 header (which happens to be a single line) followed
        /// by the binary data bytes.
        /// 
        /// The header is a line of comma-separated fields followed by a single linefeed (LF, \n).
        /// 
        /// The fields are as follows:
        /// <list type="bullet">
        /// <item>
        /// <term>
        /// version
        /// </term>
        /// <description>
        /// must be 1.
        /// </description>
        /// </item>
        /// <item>
        /// <term>
        /// streamName
        /// </term>
        /// <description>
        /// determines which stream this packet belongs to
        /// </description>
        /// </item>
        /// <item>
        /// <term>
        /// timestamp
        /// </term>
        /// <description>
        /// timestamp of this packet
        /// </description>
        /// </item>
        /// <item>
        /// <term>
        /// datalength
        /// </term>
        /// <description>
        /// the number of binary bytes that follow this header and make up the payload of this packet
        /// </description>
        /// </item>
        /// <item>
        /// <term>
        /// filler
        /// </term>
        /// <description>
        /// excetly enough '0' characters to make the header 64 bytes
        /// </description>
        /// </item>
        /// </list>
        /// </summary>
        const int HeaderVersion = 1;
        const int HeaderLength = 64;

        private static TransportProtocolTCPReflector _Instance;
        private static string _InstanceURL;

        private Socket Sock;
        private HashSet<string> OutgoingStreams = new();
        private Queue<byte[]> OutgoingQueue = new();
        private Thread OutgoingThread;

        private Dictionary<string, QueueThreadSafe> IncomingQueues;
        private Thread IncomingThread;

        public static void Register()
        {
            RegisterTransportProtocol("tcpreflector", AsyncTCPReflectorWriter.Factory, AsyncTCPReflectorReader.Factory, AsyncTCPReflectorReader.Factory_Tiled);
        }

        public static TransportProtocolTCPReflector Connect(string url)
        {
            if (_Instance == null)
            {
                _InstanceURL = url;
                _Instance = new TransportProtocolTCPReflector(url);
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

        TransportProtocolTCPReflector(string url)
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
            string header = $"{HeaderVersion},{streamName},{chk.metadata.timestamp},{chk.length},";
            int zeroesNeeded = HeaderLength - 1 - header.Length;
            string zeroes = new('0', zeroesNeeded);
            header = header + zeroes + "\n";
            byte[] b_header = Encoding.UTF8.GetBytes(header);
            if (b_header.Length != HeaderLength)
            {
                Debug.LogError($"{Name()}: header size {b_header.Length} unequal {HeaderLength}. Dropping.");
                return;
            }
            int totalLength = b_header.Length + chk.length;
            var buf = new byte[totalLength];
            Array.Copy(b_header, buf, b_header.Length);
            System.Runtime.InteropServices.Marshal.Copy(chk.pointer, buf, b_header.Length, chk.length);
            OutgoingQueue.Enqueue(buf);
        }

        private bool _DecodeHeader(string header, out string streamName, out long timestamp, out int dataLength)
        {
            string[] lines = header.Split('\n');
            string[] words = lines[0].Split(',');
            if (lines.Length != 2 || words.Length != 5)
            {
                Debug.LogWarning($"{Name()}:Bad header: {header}");
                streamName = "";
                timestamp = 0;
                dataLength = 0;
                return false;
            }
            int version = int.Parse(words[0]);
            if (version != HeaderVersion) {
                Debug.LogWarning($"{Name()}: Bad header version: {version}, expected {HeaderVersion}");
                streamName = "";
                timestamp = 0;
                dataLength = 0;
                return false;
            }
            streamName = words[1];
            timestamp = long.Parse(words[2]);
            dataLength = int.Parse(words[3]);
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
            byte[] b_header = new byte[HeaderLength];
            int actualSize = Sock.Receive(b_header);
            if (actualSize != HeaderLength)
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
                bool ok = _DecodeHeader(header, out string streamName, out long timeStamp, out int dataLength);
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
