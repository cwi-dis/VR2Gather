using UnityEngine;
using System.Collections.Generic;
using VRT.Core;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
namespace VRT.Transport.Dash
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
    using IncomingStreamDescription = Cwipc.StreamSupport.IncomingStreamDescription;
    using IncomingTileDescription = Cwipc.StreamSupport.IncomingTileDescription;

    public class AsyncDashReader_PC : AsyncDashReader
    {
        protected IncomingTileDescription[] tileDescriptors;
        protected IncomingStreamDescription[] allStreamDescriptors;

        public AsyncDashReader_PC Init(string _url, string _streamName, string fourcc, IncomingTileDescription[] _tileDescriptors)
        {
            base.Init(_url, _streamName);
            lock (this)
            {
                tileDescriptors = _tileDescriptors;
                int nTiles = tileDescriptors.Length;
                //Debug.Log($"xxxjack {Name()}: constructor: nTiles={nTiles}");
                perTileInfo = new TileOrMediaInfo[nTiles];
                for (int ti = 0; ti < nTiles; ti++)
                {
                    TileOrMediaInfo ri = new TileOrMediaInfo();
                    IncomingTileDescription td = tileDescriptors[ti];
                    ri.tileDescriptor = td;
                    ri.tileNumber = td.tileNumber;
                    ri.outQueue = tileDescriptors[ti].outQueue;
                    ri.streamIndexes = new List<int>();
                    if (td.streamDescriptors != null) {
                        foreach (var sd in td.streamDescriptors)
                        {
                            ri.streamIndexes.Add(sd.streamIndex);
                        }
                    }
                    perTileInfo[ti] = ri;
                }
                Start();
            }
            initialized = true;
            return this;
        }

        protected override void _streamInfoAvailable()
        {
            lock (this)
            {
                //
                // Get stream information
                //
                streamCount = subHandle.get_stream_count();
                Debug.Log($"{Name()}: streamInfoAvailable: {streamCount} streams.");
                //
                // Get more stream information
                //
                allStreamDescriptors = subHandle.get_streams();
#if VRT_WITH_STATS
                foreach (var sd in allStreamDescriptors)
                {
                    Statistics.Output(base.Name(), $"stream_index={sd.streamIndex}, tile={sd.tileNumber}, orientation={sd.orientation}");
                }
#endif
                _recomputeStreams();
            }
        }

        public void setTileQualityIndex(int tileIndex, int qualityIndex)
        {
            lock (this)
            {
                if (subHandle == null)
                {
                    // Too early: not playing yet
                    return;
                }
                var td = tileDescriptors[tileIndex];
                int tileNumber = td.tileNumber;
                
                // Now for this tile (and therefore receiver) find correct stream descriptor for this quality.
                if (qualityIndex >= 0)
                {
#if VRT_WITH_STATS
                    Statistics.Output(base.Name(), $"tile={tileNumber}, reader_enabled=1, tileIndex={tileIndex}, qualityIndex={qualityIndex}");
#endif
                    bool ok = subHandle.enable_stream(tileIndex, qualityIndex);
                    if (!ok)
                    {
                        Debug.LogError($"{Name()}: Could not enable quality {qualityIndex} for tile {tileNumber}, tileIndex={tileIndex}, qualityIndex={qualityIndex}");
                    }
                    
                }
                else
                {
#if VRT_WITH_STATS
                    Statistics.Output(base.Name(), $"tile={tileNumber}, reader_enabled=0, tileIndex={tileIndex}");
#endif
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
                Debug.Log($"{Name()}: xxxjack _recomputeStreams: received tileDescriptors for {tileDescriptors.Length} tiles");
                if (tileDescriptors.Length != perTileInfo.Length)
                {
                    Debug.LogError($"{Name()}: _recomputeStreams: {tileDescriptors.Length} tile descriptors but {perTileInfo.Length} receivers");
                }
                for (int i = 0; i < tileDescriptors.Length; i++)
                {
                    IncomingTileDescription td = tileDescriptors[i];
                    TileOrMediaInfo ri = perTileInfo[i];

                    List<IncomingStreamDescription> streamDescriptorsPerTile = new List<IncomingStreamDescription>();
                    Debug.Log($"{Name()}: _recomputeStreams: tile {i}: tileNumber={td.tileNumber}: examine streamDescriptors for {allStreamDescriptors.Length} streams");
                    foreach (var sd in allStreamDescriptors)
                    {
                        if (sd.tileNumber == td.tileNumber)
                        {
                            Debug.Log($"{Name()}: xxxjack tile {i}: tileNumber={td.tileNumber}: found orientation={sd.orientation} streamIndex={sd.streamIndex}");
                            // If this stream is for this tile we remember the streamIndex.
                            streamDescriptorsPerTile.Add(sd);
                        }
                    }
                    // Convert per-tile stream descriptor to an array
                    td.streamDescriptors = streamDescriptorsPerTile.ToArray();
                    // And update per-receiver tile information
                    tileDescriptors[i] = td;
                    ri.tileDescriptor = td;
                    // Update streamIndexes
                    ri.streamIndexes.Clear();
                    foreach (var sd in td.streamDescriptors)
                    {
                        ri.streamIndexes.Add(sd.streamIndex);
                    }


                }
                for(int tileIndex=0; tileIndex < tileDescriptors.Length; tileIndex++) 
                {
                    var td = tileDescriptors[tileIndex];
                    // We know all the streams that may be used for this tile. Remember for the puller thread.
                    if (td.streamDescriptors.Length == 0)
                    {
                        Debug.LogError($"{Name()} _recomputeStreams: tile={td.tileNumber} has no streams");
                    }
                    else
                    {
                        int wantedIndex = 0; // td.streamDescriptors.Length - 1; // xxxjack debug attempt: select last quality, not first
                        Debug.Log($"{Name()}:_recomputeStreams: tileNumber={td.tileNumber}: {td.streamDescriptors.Length} streams, selecting {wantedIndex}");
                        // And we can also tell the SUB which quality we want for this tile.
                        setTileQualityIndex(tileIndex, wantedIndex);
                    }
                }
            }
        }
    }
}
