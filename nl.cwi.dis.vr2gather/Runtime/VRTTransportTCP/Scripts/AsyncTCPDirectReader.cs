using UnityEngine;
using VRT.Core;
using Cwipc;

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
            base.Init(_url, fourcc, outQueue);
            return this;
        }
    }
}