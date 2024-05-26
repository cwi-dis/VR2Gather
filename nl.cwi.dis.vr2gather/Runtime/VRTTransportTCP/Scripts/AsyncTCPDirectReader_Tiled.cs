using System;
using VRT.Core;

namespace VRT.Transport.TCP
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
 
    public class AsyncTCPDirectReader_Tiled : Cwipc.AsyncTCPPCReader, ITransportProtocolReader_Tiled
    {
        static public ITransportProtocolReader_Tiled Factory()
        {
            return new AsyncTCPDirectReader_Tiled();
        }

        public ITransportProtocolReader_Tiled Init(string _url, string userId, string streamName, string fourcc, Cwipc.StreamSupport.IncomingTileDescription[] _tileDescriptors)
        {
            if (streamName != "audio")
            {
                Uri tmp = new Uri(_url);
                _url = $"tcp://{tmp.Host}:{tmp.Port + 1}";
            }
            base.Init(_url, fourcc, _tileDescriptors);
            return this;
        }
    }
}