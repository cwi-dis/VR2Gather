using VRT.Core;
using Cwipc;
using System;
using System.Text;

namespace VRT.Transport.TCPSFU
{
    public class TransportProtocolTCPSFU : TransportProtocol
    {
        public static void Register()
        {
            RegisterTransportProtocol("tcpsfu", AsyncTCPSFUWriter.Factory, AsyncTCPSFUReader.Factory, AsyncTCPSFUReader.Factory_Tiled);
        }

        private static TransportProtocolTCPSFU _Instance;
        private static string _InstanceURL;

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

        TransportProtocolTCPSFU(string url)
        {
            // xxxjack open connection
            // xxxjack create sender queue
            // xxxjack create receiver queue dictionary
            // xxxjack start sender thread
            // xxxjack start receiver thread
        }

        public void Stop()
        {
            // xxxjack stop sender thread
            // xxxjack stop receiver thread
            // xxxjack clear sender queue
        }

        public void RegisterOutgoingStream(string streamName)
        {

        }

        public void UnregisterOutgoingStream(string streamName)
        {

        }

        public void RegisterIncomingStream(string streamName, QueueThreadSafe outQueue)
        {

        }

        public void UnregisterIncomingStream(string streamName)
        {

        }

        public void SendChunk(BaseMemoryChunk chk, string streamName)
        {
            string header = $"{streamName},{chk.metadata.timestamp},{chk.length}\n";
            byte[] b_header = Encoding.UTF8.GetBytes(header);
            int totalLength = b_header.Length + chk.length;
            var buf = new byte[totalLength];
            Array.Copy(b_header, buf, b_header.Length);
            System.Runtime.InteropServices.Marshal.Copy(chk.pointer, buf, b_header.Length, chk.length);
            // xxxjack push to sender queue
        }
    }

    
}
