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

        public PCSubReader(string _url, string _streamName, string fourcc, int _initialDelay, TileDescriptor[] _tileDescriptors)
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
                    TileDescriptor td = tileDescriptors[ti];
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
                Debug.Log($"{Name()}: streamInfoAvailable: {streamCount} streams.");
                //
                // Get more stream information
                //
                allStreamDescriptors = subHandle.get_streams();
                foreach (var sd in allStreamDescriptors)
                {
                    BaseStats.Output(Name(), $"stream_index={sd.streamIndex}, tile={sd.tileNumber}, orientation={sd.orientation}");
                }
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
                    BaseStats.Output(Name(), $"tile={tileNumber}, reader_enabled=1, tileIndex={tileIndex}, qualityIndex={qualityIndex}");
                    bool ok = subHandle.enable_stream(tileIndex, qualityIndex);
                    if (!ok)
                    {
                        Debug.LogError($"{Name()}: Could not enable quality {qualityIndex} for tile {tileNumber}, tileIndex={tileIndex}, qualityIndex={qualityIndex}");
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
                Debug.Log($"{Name()}: xxxjack _recomputeStreams: received tileDescriptors for {tileDescriptors.Length} tiles");
                if (tileDescriptors.Length != receivers.Length)
                {
                    Debug.LogError($"{Name()}: _recomputeStreams: {tileDescriptors.Length} tile descriptors but {receivers.Length} receivers");
                }
                for (int i = 0; i < tileDescriptors.Length; i++)
                {
                    TileDescriptor td = tileDescriptors[i];
                    ReceiverInfo ri = receivers[i];

                    List<sub.StreamDescriptor> streamDescriptorsPerTile = new List<sub.StreamDescriptor>();
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
