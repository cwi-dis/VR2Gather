using UnityEngine;
using System.Collections.Generic;

namespace Workers {
    public class PCSubReader : BaseSubReader
    {

        public struct TileDescriptor
        {
            public QueueThreadSafe outQueue;
            public int tileNumber;
            public int currentQuality;
        }
        protected TileDescriptor[] tileDescriptors;
        protected sub.StreamDescriptor[] streamDescriptors;

        public PCSubReader(string _url, string _streamName, int _initialDelay, TileDescriptor[] _tileDescriptors)
        : base(_url, _streamName, _initialDelay)
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

        protected override void _streamInfoAvailable()
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

        public void setTileQuality(int tileNumber, int quality)
        {
            if (quality > 0)
            {
                Debug.Log($"{Name()}: xxxjack enable_stream({tileNumber}, {quality});");
                subHandle.enable_stream(tileNumber, quality);
            }
            else
            {
                Debug.Log($"{Name()}: xxxjack disable_stream({tileNumber});");
                subHandle.disable_stream(tileNumber);
            }
        }

        protected void _recomputeStreams()
        {
            //
            // We have both tile descriptions and stream descriptions. Match them up.
            //
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
