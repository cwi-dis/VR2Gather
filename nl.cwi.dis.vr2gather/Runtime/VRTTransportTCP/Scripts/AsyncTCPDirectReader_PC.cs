using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.Transport.TCP
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
 
    public class AsyncTCPDirectReader_PC : Cwipc.AsyncTCPPCReader
    {
        public AsyncTCPDirectReader_PC(string _url, string fourcc, Cwipc.StreamSupport.IncomingTileDescription[] _tileDescriptors)
         : base(_url, fourcc, _tileDescriptors)
        {

        }
    }
}