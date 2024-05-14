using System;
using VRT.Core;

namespace VRT.Transport.TCP
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
 
    public class AsyncTCPDirectReader : Cwipc.AsyncTCPReader, ITransportProtocolReader
    {
        static public ITransportProtocolReader Factory()
        {
            return new AsyncTCPDirectReader();
        }

        public ITransportProtocolReader Init(string _url, string _streamName, int _streamNumber, string fourcc, QueueThreadSafe outQueue)
        {
            if (_streamName != "audio")
            {
                Uri tmp = new Uri(_url);
                _url = $"tcp://{tmp.Host}:{tmp.Port + 1}";
            }
            base.Init(_url, fourcc, outQueue);
            return this;
        }
    }
}