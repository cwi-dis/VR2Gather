using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.Transport.TCP
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
 
    public class AsyncTCPDirectReader : Cwipc.AsyncTCPReader
    {
        public new AsyncTCPDirectReader Init(string _url, string fourcc, QueueThreadSafe outQueue)
        {
            base.Init(_url, fourcc, outQueue);
            return this;
        }
    }
}