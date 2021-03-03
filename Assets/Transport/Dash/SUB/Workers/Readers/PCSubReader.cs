using UnityEngine;
using System.Collections.Generic;
using VRT.Core;

namespace VRT.Transport.Dash
{
    public class PCSubReader : BaseSubReader
    {

        public struct TileDescriptor
        {
            public string name;
            public QueueThreadSafe outQueue;
            public int tileNumber;
            public int currentQuality;
        }
        protected TileDescriptor[] tileDescriptors;
        protected sub.StreamDescriptor[] streamDescriptors;

        public PCSubReader(string _url, string _streamName, int _initialDelay, TileDescriptor[] _tileDescriptors)
        : base(_url, _streamName, _initialDelay)
        {
            lock (this)
            {
                tileDescriptors = _tileDescriptors;
                int nTiles = tileDescriptors.Length;
                receivers = new ReceiverInfo[nTiles];
                for (int ti = 0; ti < nTiles; ti++)
                {
                    ReceiverInfo ri = new ReceiverInfo();
                    ri.outQueue = tileDescriptors[ti].outQueue;
                    ri.streamIndexes = new int[0];
                    receivers[ti] = ri;
                }
                Start();
            }
        }

        protected override void _streamInfoAvailable()
        {
            lock (this)
            {
                //
                // Get stream information
                //
                streamCount = subHandle.get_stream_count();
                Debug.Log($"{Name()}: sub.play({url}) successful, {streamCount} streams.");
                //
                // Get more stream information
                //
                streamDescriptors = subHandle.get_streams();
                foreach (var sd in streamDescriptors)
                {
                    Debug.Log($"{Name()}: xxxjack streamIndex={sd.streamIndex}, tileNumber={sd.tileNumber}, quality={sd.quality}");
                }
                _recomputeStreams();
            }
        }

        public void setTileQuality(int tileNumber, int quality)
        {
            lock (this)
            {
                if (quality > 0)
                {
                    Debug.Log($"{Name()}: xxxjack SKIP enable_stream({tileNumber}, {quality});");
                    //                    bool ok = subHandle.enable_stream(tileNumber, quality);
                    //                    if (!ok)
                    //                    {
                    //                        Debug.LogError($"{Name()}: Could not enable quality {quality} for tile {tileNumber}");
                    //                    }
                }
                else
                {
                    Debug.Log($"{Name()}: xxxjack SKIP disable_stream({tileNumber});");
                    //                    bool ok = subHandle.disable_stream(tileNumber);
                    //                    if (!ok)
                    //                    {
                    //                        Debug.LogError($"{Name()}: Could not disable tile {tileNumber}");
                    //                    }
                }
            }
        }

        protected void _recomputeStreams()
        {
            lock (this)
            {
                //
                // We have both tile descriptions and stream descriptions. Match them up.
                //
                Debug.Log($"{Name()}: xxxjack _recomputeStreams: looking at {tileDescriptors.Length} tiles");
                for (int i = 0; i < tileDescriptors.Length; i++)
                {
                    var td = tileDescriptors[i];
                    var ri = receivers[i];

                    List<int> streamIndexes = new List<int>();
                    foreach (var sd in streamDescriptors)
                    {
                        if (sd.tileNumber == td.tileNumber)
                        {
                            // If this stream is for this tile we remember the streamIndex.
                            streamIndexes.Add(sd.streamIndex);
                            // And if this is the first time we see a stream for this tile
                            // we select this quality
                            if (td.currentQuality <= 0)
                            {
                                td.currentQuality = sd.quality;
                            }
                        }
                    }
                    // We know all the streams that may be used for this tile. Remember for the puller thread.
                    ri.streamIndexes = streamIndexes.ToArray();
                    Debug.Log($"{Name()}: xxxjack _recomputeStreams: tile {i}: looking at {ri.streamIndexes.Length} streams");
                    // And we can also tell the SUB which quality we want for this tile.
                    setTileQuality(td.tileNumber, td.currentQuality);
                }
                //
                // Finally, we disable the tiles that exist but that we are not interested in.
                //
                foreach (var sd in streamDescriptors)
                {
                    bool tileUsed = false;
                    foreach (var td in tileDescriptors)
                    {
                        if (td.tileNumber == sd.tileNumber)
                        {
                            tileUsed = true;
                            break;
                        }
                    }
                    if (!tileUsed)
                    {
                        setTileQuality(sd.tileNumber, -1);
                    }
                }
            }
        }
    }
}
