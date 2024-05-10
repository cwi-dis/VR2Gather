using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.Transport.TCP
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
 
    public class AsyncTCPDirectReader_PC : Cwipc.AsyncTCPPCReader
    {
        public AsyncTCPDirectReader_PC Init(string _url, string streamname, string fourcc, Cwipc.StreamSupport.IncomingTileDescription[] _tileDescriptors)
        {
            base.Init(_url, fourcc, _tileDescriptors);
            return this;
        }
    }
}