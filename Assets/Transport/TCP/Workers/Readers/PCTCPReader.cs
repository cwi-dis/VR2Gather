using UnityEngine;
using System.Collections.Generic;
using VRT.Core;
using VRT.Transport.Dash;

namespace VRT.Transport.TCP
{
    public class PCTCPReader : BaseTCPReader
    {
        public PCTCPReader(string _url, PCSubReader.TileDescriptor[] _tileDescriptors)
        : base(_url)
        {
            lock (this)
            {
                int nTiles = _tileDescriptors.Length;
                receivers = new ReceiverInfo[nTiles];
                for (int ti = 0; ti < nTiles; ti++)
                {
                    ReceiverInfo ri = new ReceiverInfo();
                    ri.tileNumber = ti;
                    ri.host = url.Host;
                    ri.port = url.Port + _tileDescriptors[ti].tileNumber;
                    PCSubReader.TileDescriptor td = _tileDescriptors[ti];
                    ri.tileDescriptor = td;
                    ri.outQueue = _tileDescriptors[ti].outQueue;
                    receivers[ti] = ri;
                }
                Start();
            }
        }
    }
}
