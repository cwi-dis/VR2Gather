using UnityEngine;
using System.Collections.Generic;

namespace Workers {
    public class PCSubReader : BaseSubReader
    {

        public struct TileDescriptor
        {
            public QueueThreadSafe outQueue;
            public int tileNumber;
            public int[] qualities;
            public int currentQualityIndex;
        }
        protected TileDescriptor[] tileDescriptors;
        protected sub.StreamDescriptor[] streamDescriptors;

        public PCSubReader(string _url, string _streamName, int _initialDelay, TileDescriptor[] _tileDescriptors)
        : base(_url, _streamName, _initialDelay)
        {
            lock(this)
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
            lock(this)
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

        public void setTileQualityIndex(int tileIndex, int qualityIndex)
        {
            lock(this)
            {
                if (qualityIndex >= 0)
                {
                    Debug.Log($"{Name()}: xxxjack enable_stream({tileIndex}, {qualityIndex});");
                    bool ok = subHandle.enable_stream(tileIndex, qualityIndex);
                    if (!ok)
                    {
                        Debug.LogError($"{Name()}: Could not enable quality#{qualityIndex} (value {tileDescriptors[tileIndex].qualities[qualityIndex]}) for tile {tileIndex}");
                    }
                }
                else
                {
                    Debug.Log($"{Name()}: xxxjack disable_stream({tileIndex});");
                    bool ok = subHandle.disable_stream(tileIndex);
                    if (!ok)
                    {
                        Debug.LogError($"{Name()}: Could not disable tile {tileIndex}");
                    }
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
                    List<int> qualityValues = new List<int>();
                    foreach (var sd in streamDescriptors)
                    {
                        if (sd.tileNumber == td.tileNumber)
                        {
                            // If this stream is for this tile we remember the streamIndex and quality
                            streamIndexes.Add(sd.streamIndex);
                            qualityValues.Add(sd.quality);
                        }
                    }
                    // We know all the streams that may be used for this tile. Remember for the puller thread.
                    ri.streamIndexes = streamIndexes.ToArray();
                    td.qualities = qualityValues.ToArray();
                    Debug.Log($"{Name()}: xxxjack _recomputeStreams: tile {i}: looking at {ri.streamIndexes.Length} streams");
                    // And we can also tell the SUB which quality we want for this tile.
                    if (td.qualities.Length == 0) td.currentQualityIndex = -1;
                    if (td.qualities.Length > 0 && td.currentQualityIndex <= 0) td.currentQualityIndex = 0;
                    setTileQualityIndex(td.tileNumber, td.currentQualityIndex);
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
                        setTileQualityIndex(sd.tileNumber, -1);
                    }
                }
            }
        }
    }
}
