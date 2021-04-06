using UnityEngine;
using System.Collections.Generic;
using VRT.Core;

namespace VRT.Transport.TCP
{
    public class PCTCPReader : BaseTCPReader
    {
#if xxxjack_disabled
        public struct TileDescriptor
        {
            public string name;
            public QueueThreadSafe outQueue;
            public int tileNumber;
            public sub.StreamDescriptor[] streamDescriptors; // streams for this tile only
        }
        protected TileDescriptor[] tileDescriptors;
        protected sub.StreamDescriptor[] allStreamDescriptors;
#endif
        public PCTCPReader(string _url)
        : base(_url)
        {
#if xxxjack_disabled
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
#endif
        }
#if xxxjack_disabled
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
                int streamIndex = -1;
                int tileIndex = tileNumber; // xxxjack is this always correct?
                int qualityIndex = 0;
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
                if (quality >= 0)
                {
                    TileDescriptor td = (TileDescriptor)ri.tileDescriptor;
                    sub.StreamDescriptor? sd = null;
                    for(int qi=0; qi < td.streamDescriptors.Length; qi++)
                    {
                        if (td.streamDescriptors[qi].quality == quality)
                        {
                            sd = td.streamDescriptors[qi];
                            qualityIndex = qi;
                        }
                    }
                    if (sd == null)
                    {
                        Debug.LogError($"{Name()}: setTileQuality: unknown quality {quality} for tile {tileNumber}");
                        return;
                    }
                    // Found the right tile and the right quality, so now we have the streamIndex.
                    streamIndex = sd.Value.streamIndex;
                    Debug.Log($"{Name()}: setTileQuality: found streamIndex={streamIndex} qualityIndex={qualityIndex}");
                }
                // We can now tell the receiver to start receiving this stream (or stop receiving altogether)
                ri.curStreamIndex = streamIndex;
                // And we tell the SUB to enable the stream for this tile/quality (or disable for this tile)
                if (streamIndex >= 0)
                {
                    BaseStats.Output(Name(), $"tile={tileNumber}, reader_enabled=1, quality={quality}, streamIndex={streamIndex}, tileIndex={tileIndex}, qualityIndex={qualityIndex}");
                    bool ok = subHandle.enable_stream(tileIndex, qualityIndex);
                    if (!ok)
                    {
                        Debug.LogError($"{Name()}: Could not enable quality {quality} for tile {tileNumber}, streamIndex={streamIndex}, tileIndex={tileIndex}, qualityIndex={qualityIndex}");
                    }
                    
                }
                else
                {
                    BaseStats.Output(Name(), $"tile={tileNumber}, reader_enabled=0, tileIndex={tileIndex}");
                    bool ok = subHandle.disable_stream(tileIndex);
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
                if (tileDescriptors.Length != receivers.Length)
                {
                    Debug.LogError($"{Name()}: _recomputeStreams: {tileDescriptors.Length} tile descriptors but {receivers.Length} receivers");
                }
                for (int i = 0; i < tileDescriptors.Length; i++)
                {
                    TileDescriptor td = tileDescriptors[i];
                    ReceiverInfo ri = receivers[i];

                    List<sub.StreamDescriptor> streamDescriptorsPerTile = new List<sub.StreamDescriptor>();
                    foreach (var sd in allStreamDescriptors)
                    {
                        if (sd.tileNumber == td.tileNumber)
                        {
                            Debug.Log($"{Name()}: xxxjack i={i} found tile={sd.tileNumber} quality={sd.quality} streamIndex={sd.streamIndex}");
                            // If this stream is for this tile we remember the streamIndex.
                            streamDescriptorsPerTile.Add(sd);
                        }
                    }
                    // Convert per-tile stream descriptor to an array
                    td.streamDescriptors = streamDescriptorsPerTile.ToArray();
                    // And update per-receiver tile information
                    tileDescriptors[i] = td;
                    ri.tileDescriptor = td;
                }
                foreach(var td in tileDescriptors) 
                {
                    // We know all the streams that may be used for this tile. Remember for the puller thread.
                    if (td.streamDescriptors.Length == 0)
                    {
                        Debug.LogError($"{Name()} _recomputeStreams: tile={td.tileNumber} has no streams");
                    }
                    else
                    {
                        Debug.Log($"{Name()}:_recomputeStreams: tile {td.tileNumber}: looking at {td.streamDescriptors.Length} streams");
                        // And we can also tell the SUB which quality we want for this tile.
                        setTileQuality(td.tileNumber, td.streamDescriptors[0].quality);
                    }
                }
            }
        }
#endif
    }
}
