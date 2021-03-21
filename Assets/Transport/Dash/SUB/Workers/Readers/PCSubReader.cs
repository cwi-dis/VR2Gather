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
            public sub.StreamDescriptor[] streamDescriptors; // streams for this tile only
        }
        protected TileDescriptor[] tileDescriptors;
        protected sub.StreamDescriptor[] allStreamDescriptors;

        public PCSubReader(string _url, string _streamName, int _initialDelay, TileDescriptor[] _tileDescriptors)
        : base(_url, _streamName, _initialDelay)
        {
            lock (this)
            {
                tileDescriptors = _tileDescriptors;
                int nTiles = tileDescriptors.Length;
                //Debug.Log($"xxxjack {Name()}: constructor: nTiles={nTiles}");
                receivers = new ReceiverInfo[nTiles];
                for (int ti = 0; ti < nTiles; ti++)
                {
                    ReceiverInfo ri = new ReceiverInfo();
                    ri.tileNumber = ti;
                    TileDescriptor td = tileDescriptors[ti];
                    ri.tileDescriptor = td;
                    ri.outQueue = tileDescriptors[ti].outQueue;
                    // Probably streamDescriptors will be received later,
                    // then we set the current streamIndex to -1 (nothing to receive yet)
                    if (td.streamDescriptors == null || td.streamDescriptors.Length == 0)
                    {
                        ri.curStreamIndex = -1;
                    } 
                    else
                    {
                        ri.curStreamIndex = td.streamDescriptors[0].streamIndex;
                    }
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
                allStreamDescriptors = subHandle.get_streams();
                foreach (var sd in allStreamDescriptors)
                {
                    BaseStats.Output(Name(), $"stream_index={sd.streamIndex}, tile={sd.tileNumber}, quality={sd.quality}");
                }
                _recomputeStreams();
            }
        }

        public void setTileQuality(int tileNumber, int quality)
        {
            lock (this)
            {
                // Find correct receiver for this tile
                ReceiverInfo ri = null;
                foreach (var _ri in receivers)
                {
                    Debug.Log($"{Name()}: setTileQuality: tileNumber={_ri.tileNumber} quality={quality}");
                    if (_ri.tileNumber == tileNumber) ri = _ri;
                }
                if (ri == null)
                {
                    Debug.LogError($"{Name()}: setTileQuality: unknown tileNumber {tileNumber}");
                    return;
                }
                // Now for this tile (and therefore receiver) find correct stream descriptor for this quality.
                int streamIndex = -1;
                if (quality >= 0)
                {
                    TileDescriptor td = (TileDescriptor)ri.tileDescriptor;
                    sub.StreamDescriptor? sd = null;
                    foreach (var _sd in td.streamDescriptors)
                    {
                        if (_sd.quality == quality) sd = _sd;
                    }
                    if (sd == null)
                    {
                        Debug.LogError($"{Name()}: setTileQuality: unknown quality {quality} for tile {tileNumber}");
                        return;
                    }
                    // Found the right tile and the right quality, so now we have the streamIndex.
                    streamIndex = sd.Value.streamIndex;
                    Debug.Log($"{Name()}: setTileQuality: found streamIndex={streamIndex}");
                }
                // We can now tell the receiver to start receiving this stream (or stop receiving altogether)
                ri.curStreamIndex = streamIndex;
                // And we tell the SUB to enable the stream for this tile/quality (or disable for this tile)
                if (streamIndex >= 0)
                {
                    BaseStats.Output(Name(), $"tile={tileNumber}, reader_enabled=1, quality={quality}, streamIndex={streamIndex}");
                    bool ok = subHandle.enable_stream(tileNumber, quality);
                    if (!ok)
                    {
                        Debug.LogError($"{Name()}: Could not enable quality {quality} for tile {tileNumber}");
                    }
                    
                }
                else
                {
                    BaseStats.Output(Name(), $"tile={tileNumber}, reader_enabled=0");
                    bool ok = subHandle.disable_stream(tileNumber);
                    if (!ok)
                    {
                        Debug.LogError($"{Name()}: Could not disable tile {tileNumber}");
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
                    TileDescriptor td = tileDescriptors[i];
                    ReceiverInfo ri = receivers[i];

                    List<sub.StreamDescriptor> streamDescriptorsPerTile = new List<sub.StreamDescriptor>();
                    foreach (var sd in allStreamDescriptors)
                    {
                        if (sd.tileNumber == td.tileNumber)
                        {
                            Debug.Log($"{Name()}: xxxjack found tile={sd.tileNumber} quality={sd.quality} streamIndex={sd.streamIndex}");
                            // If this stream is for this tile we remember the streamIndex.
                            streamDescriptorsPerTile.Add(sd);
                          }
                    }
                    // Convert per-tile stream descriptor to an array
                    td.streamDescriptors = streamDescriptorsPerTile.ToArray();
                    // And update per-receiver tile information
                    ri.tileDescriptor = td;
                    // We know all the streams that may be used for this tile. Remember for the puller thread.
                    if (td.streamDescriptors.Length == 0)
                    {
                        Debug.LogError($"{Name()} _recomputeStreams: tile={i} has no streams");
                    }
#if bad
                    else if (true || td.streamDescriptors.Length == 1)
                    {
                        Debug.Log($"{Name()}: _recomputeStreams: tile {i}: {td.streamDescriptors.Length} streams, no reason to update");
                    }
#endif
                    else
                    {
                        Debug.Log($"{Name()}:_recomputeStreams: tile {td.tileNumber}: looking at {td.streamDescriptors.Length} streams");
                        // And we can also tell the SUB which quality we want for this tile.
                        setTileQuality(td.tileNumber, td.streamDescriptors[0].quality);
                    }
                }
            }
        }
    }
}
