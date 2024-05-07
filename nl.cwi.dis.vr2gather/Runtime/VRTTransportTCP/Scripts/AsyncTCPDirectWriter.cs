using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.Transport.TCP
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
 
    public class AsyncTCPDirectWriter : Cwipc.AsyncTCPWriter
    {
        public AsyncTCPDirectWriter(string _url, string streamName, string fourcc, Cwipc.StreamSupport.OutgoingStreamDescription[] _descriptions)
         : base(_url, fourcc, _descriptions)
        {

        }
    }
}