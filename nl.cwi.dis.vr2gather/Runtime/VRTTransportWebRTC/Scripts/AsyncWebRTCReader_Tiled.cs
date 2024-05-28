using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using Cwipc;
using VRT.Core;

namespace VRT.Transport.WebRTC
{
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;

    public class AsyncWebRTCReader_Tiled : AsyncWebRTCReader
    {
        public ITransportProtocolReader_Tiled Init(string remoteUrl, string userId, string streamName, string fourcc, IncomingTileDescription[] _tileDescriptors)
        {
            lock (this)
            {
                int nTiles = _tileDescriptors.Length;
                receivers = new ReceiverInfo[nTiles];
                for (int ti = 0; ti < nTiles; ti++)
                {
                    ReceiverInfo ri = new ReceiverInfo();
                    ri.tileNumber = ti;
                    ri.trackOrStream = new XxxjackTrackOrStream();
                    IncomingTileDescription td = _tileDescriptors[ti];
                    ri.tileDescriptor = td;
                    ri.outQueue = _tileDescriptors[ti].outQueue;
                    ri.fourcc = StreamSupport.VRT_4CC(fourcc[0], fourcc[1], fourcc[2], fourcc[3]);
                    receivers[ti] = ri;
                }
                Start();
            }
            return this;
        }

        public void setTileQualityIndex(int tileIndex, int qualityIndex)
        {
            Debug.Log($"{Name()}: setTileQualityIndex({tileIndex},{qualityIndex})");
            int portOffset = qualityIndex * receivers.Length;
            Debug.LogWarning($"{Name()}: setTileQuanlityIndex not yet implemented");
        }
    }
}