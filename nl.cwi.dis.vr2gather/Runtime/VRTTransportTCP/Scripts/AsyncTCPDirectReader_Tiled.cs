using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.Transport.TCP
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
 
    public class AsyncTCPDirectReader_Tiled : Cwipc.AsyncTCPPCReader
    {
        public AsyncTCPDirectReader_Tiled Init(string _url, string streamname, string fourcc, Cwipc.StreamSupport.IncomingTileDescription[] _tileDescriptors)
        {
            base.Init(_url, fourcc, _tileDescriptors);
            return this;
        }
    }
}