using UnityEngine;
using VRT.Core;
using Cwipc;
using System;

namespace VRT.Transport.TCP
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
 
    public class AsyncTCPDirectWriter : Cwipc.AsyncTCPWriter, ITransportProtocolWriter
    {
        static public ITransportProtocolWriter Factory()
        {
            return new AsyncTCPDirectWriter();
        }

        public AsyncTCPDirectWriter()
        : base()
        {

        }

        public ITransportProtocolWriter Init(string _url, string streamName, string fourcc, Cwipc.StreamSupport.OutgoingStreamDescription[] _descriptions)
        {
            if (streamName != "audio") {
                Uri tmp = new Uri(_url);
                _url = $"tcp://{tmp.Host}:{tmp.Port + 1}";
            }
            base.Init(_url, fourcc, _descriptions);
            return this;
        }
    }
}