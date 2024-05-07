using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.Transport.TCP
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
 
    public class AsyncTCPDirectReader : Cwipc.AsyncTCPReader
    {
        public AsyncTCPDirectReader(string _url, string fourcc, QueueThreadSafe outQueue)
         : base(_url, fourcc, outQueue)
        {

        }
    }
}