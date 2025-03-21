using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using Cwipc;
using VRT.Core;

namespace VRT.Transport.WebRTC
{
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;

    public class AsyncWebRTCReader_Tiled : AsyncWebRTCReader, ITransportProtocolReader_Tiled
    {
        static public ITransportProtocolReader_Tiled Factory_Tiled()
        {
            return new AsyncWebRTCReader_Tiled();
        }

        public ITransportProtocolReader_Tiled Init(string _url, string userId, string streamName, string fourcc, IncomingTileDescription[] _tileDescriptors)
        {
            NoUpdateCallsNeeded();
            connection = TransportProtocolWebRTC.Connect(_url);
            clientId = GetClientIdFromUserId(userId);
            isAudio = streamName == "audio";    
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
#if VRT_WITH_STATS
                Statistics.Output(Name(), $"url={_url}, stream={streamName}, nStream={nTiles}, clientId={clientId}");
#endif
                Start();
            }
            return this;
        }

        /* public void setTileQualityIndex(int tileIndex, int qualityIndex)
        {
            Debug.Log($"{Name()}: setTileQualityIndex({tileIndex}, {qualityIndex})");
            int portOffset = qualityIndex * receivers.Length;
            Debug.LogWarning($"{Name()}: setTileQuanlityIndex not yet implemented");
        } */

        public void setTileQualities(int[] tileQualities)
        {
            Debug.Log($"{Name()}: setTileQualities({tileQualities})");
            connection.SetTileQualities(tileQualities);
        }
    }
}