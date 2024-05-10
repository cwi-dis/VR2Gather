using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.Transport.TCP
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
 
    public class AsyncTCPDirectWriter : Cwipc.AsyncTCPWriter
    {
        public AsyncTCPDirectWriter()
        : base()
        {

        }

        public AsyncTCPDirectWriter Init(string _url, string streamName, string fourcc, Cwipc.StreamSupport.OutgoingStreamDescription[] _descriptions)
        {
            base.Init(_url, fourcc, _descriptions);
            return this;
        }
    }
}