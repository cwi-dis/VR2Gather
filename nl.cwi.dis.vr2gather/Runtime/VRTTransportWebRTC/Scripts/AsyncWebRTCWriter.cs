using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;
using VRT.Core;
// using UnityEditor.MemoryProfiler;

#if !VRT_WITHOUT_STATS
using Statistics = Cwipc.Statistics;
#endif
using Cwipc;

namespace VRT.Transport.WebRTC
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;

    /// <summary>
    /// Class that writes frames over WebRTC
    ///
    /// The class supports sending tiled streams, by creating multiple servers (on increasing port numbers).
    /// </summary>
    public class AsyncWebRTCWriter : AsyncWriter, ITransportProtocolWriter
    {

        // xxxjack The next two types have to be replaced with whatever identifies our
        // outgoing connection to our peer and whetever identifies the thing that we use to transmit a
        // sequence of frames over
        protected class XxxjackPeerConnection { };
        protected class XxxjackTrackOrStream { };

       

        protected struct WebRTCStreamDescription
        {
            public int index;
            public int tileNumber;
            public int quality;
            public XxxjackTrackOrStream trackOrStream;
            public uint fourcc;
            public QueueThreadSafe inQueue;
        };

        protected XxxjackPeerConnection peerConnection;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        protected WebRTCStreamDescription[] descriptions;

        protected bool isAudio;

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        protected class WebRTCPushThread
        {
            AsyncWebRTCWriter parent;
            WebRTCStreamDescription description;
            System.Threading.Thread myThread;
            volatile bool stopping = false;
            int tile_number;
            int quality;
          
            public WebRTCPushThread(AsyncWebRTCWriter _parent, WebRTCStreamDescription _description, int _tile_number, int _quality)
            {
                parent = _parent;
                description = _description;
                myThread = new System.Threading.Thread(run);
                myThread.Name = Name();
                // A thread that doesn't stop must not keep the application from quitting (cwi-dis/cwipc_unity#82, rule 5).
                myThread.IsBackground = true;
                tile_number = _tile_number;
                quality = _quality;
#if !VRT_WITHOUT_STATS
                stats = new Stats(Name());
#endif
            }

            public string Name()
            {
                return $"{parent.Name()}.{description.index}";
            }

            public void Start()
            {
                myThread.Start();
            }

            public void Stop()
            {
                stopping = true;
            }

            public bool Join(int joinTimeout)
            {
                return AsyncWorker.JoinHelperThread(myThread, joinTimeout, parent.Name());
            }

            protected void run()
            {
                try
                {
                    Debug.Log($"{Name()}: thread started");
                    QueueThreadSafe queue = description.inQueue;
                    // Stops when told to, also if the producer never closes our input (cwi-dis/cwipc_unity#82, rule 2).
                    while (!stopping && !queue.IsClosed())
                    {
                        NativeMemoryChunk mc = (NativeMemoryChunk)queue.TryDequeue(AsyncWorker.DequeueTimeoutMs);
                        if (mc == null) continue;
#if !VRT_WITHOUT_STATS
                        stats.statsUpdate(mc.length);
#endif
                        if (parent.isAudio) {
                            parent.connection.SendAudioFrame(mc, description.fourcc);
                        }
                        else
                        {
                            parent.connection.SendTile(mc, tile_number, quality, description.fourcc);
                        }

                    }
                    Debug.Log($"{Name()}: Thread stopped");
                }
#pragma warning disable CS0168
                catch (System.Exception e)
                {
#if UNITY_EDITOR
                    throw;
#else
                    Debug.Log($"{Name()}: Exception: {e.Message} Stack: {e.StackTrace}");
                    Debug.LogError("Error while sending visual representation or audio to other participants");
#endif
                }
                finally
                {
                    // However this thread ends: its final stats record (#451).
#if !VRT_WITHOUT_STATS
                    stats.Final();
#endif
                }

            }

#if !VRT_WITHOUT_STATS
            protected class Stats : Statistics
            {
                public Stats(string name) : base(name) { }

                double statsTotalBytes = 0;
                double statsTotalPackets = 0;
                int statsAggregatePackets = 0;

                public void statsUpdate(int nBytes)
                {
                    lock (statsLock)
                    {
                        statsTotalBytes += nBytes;
                        statsTotalPackets++;
                        statsAggregatePackets++;
                        MaybeOutput();
                    }
                }

                protected override void OutputAggregate()
                {
                    OutputFields(
                        Rate("fps", statsTotalPackets),
                        Average("bytes_per_packet", statsTotalBytes, statsTotalPackets, "F0"),
                        Field("aggregate_packets", statsAggregatePackets),
                        Field("n_packets", statsTotalPackets),
                        Field("bytes", statsTotalBytes));
                    statsTotalBytes = 0;
                    statsTotalPackets = 0;
                }
            }

            protected Stats stats;
#endif
        }
 
        WebRTCPushThread[] pusherThreads;
        System.Diagnostics.Process process_writer;
        System.Diagnostics.Process process_reader;

    static public ITransportProtocolWriter Factory()
        {
            return new AsyncWebRTCWriter();
        }
        protected TransportProtocolWebRTC connection;

        /// <summary>
        /// Setup a WebRTC output (transmitter)
        /// Probably needs to open a connection to a WebRTC server.
        /// </summary>
        /// <param name="_url">Where the server should ser on</param>
        /// <param name="fourcc">4CC media type</param>
        /// <param name="_descriptions">Array of stream descriptions</param>
        public ITransportProtocolWriter Init(string _url, string userId, string streamName, string fourcc, OutgoingStreamDescription[] _descriptions)
        {
            NoUpdateCallsNeeded();
            connection = TransportProtocolWebRTC.Connect(_url);
            isAudio = (streamName == "audio");
            if (string.IsNullOrEmpty(_url))
            {
                throw new System.Exception($"{Name()}: No WebRTC SFU URL found in session description.");
            }
            if (_descriptions == null || _descriptions.Length == 0)
            {
                throw new System.Exception($"{Name()}: descriptions is null or empty");
            }
            if (fourcc.Length != 4)
            {
                throw new System.Exception($"{Name()}: 4CC is \"{fourcc}\" which is not exactly 4 characters");
            }
            uint fourccInt = StreamSupport.VRT_4CC(fourcc[0], fourcc[1], fourcc[2], fourcc[3]);
            connection = TransportProtocolWebRTC.Connect(_url);

            WebRTCStreamDescription[] ourDescriptions = new WebRTCStreamDescription[_descriptions.Length];
            // We use the lowest ports for the first quality, for each tile.
            // The the next set of ports is used for the next quality, and so on.
            int maxTileNumber = -1;
            for(int i=0; i<_descriptions.Length; i++)
            {
                if (_descriptions[i].tileNumber > maxTileNumber) maxTileNumber = (int)_descriptions[i].tileNumber;
            }
            int portsPerQuality = maxTileNumber+1;
            for(int i=0; i<_descriptions.Length; i++)
            {
                ourDescriptions[i] = new WebRTCStreamDescription
                {
                    index = (int)_descriptions[i].tileNumber + (portsPerQuality * _descriptions[i].qualityIndex),
                    tileNumber = (int)_descriptions[i].tileNumber,
                    quality = _descriptions[i].qualityIndex,
                    trackOrStream = new XxxjackTrackOrStream(),
                    fourcc = fourccInt,
                    inQueue = _descriptions[i].inQueue
                };
            }
            descriptions = ourDescriptions;
#if !VRT_WITHOUT_STATS
            Statistics.Output(Name(), $"url={_url}, stream={streamName}, nStream={descriptions.Length}");
#endif
            Start();
            return this;
        }

        protected override void Start()
        {
            int nTracks = descriptions.Length;

            int nTiles = 0;
            int nQualities = 0;
            foreach (WebRTCStreamDescription d in descriptions)
            {
                if (d.tileNumber + 1 > nTiles)
                {
                    nTiles = d.tileNumber + 1;
                }
                if (d.quality + 1 > nQualities)
                {
                    nQualities = d.quality + 1;
                }
            }
            Debug.Log($"{Name()}: nTracks={(uint)nTracks}, nTiles={(uint)nTiles}, nQualities={(uint)nQualities}");

            connection.RegisterTransmitter(nTracks, nTiles, nQualities);

            pusherThreads = new WebRTCPushThread[nTracks];
            for (int i = 0; i < nTracks; i++)
            {
                // Note: we need to copy i to a new variable, otherwise the lambda expression capture will bite us
                int stream_number = i;
                pusherThreads[i] = new WebRTCPushThread(this, descriptions[i], descriptions[i].tileNumber, descriptions[i].quality);
#if !VRT_WITHOUT_STATS
                Statistics.Output(Name(), $"pusher={pusherThreads[i].Name()}, stream={i}");
#endif
            }
            foreach (var t in pusherThreads)
            {
                t.Start();
            }
            // Everything the threads use exists before the worker thread starts; base.Start() last (rule 3).
            base.Start();
        }

        public override void Stop()
        {
            // Only signal here, on the main thread; the joining is done in AsyncOnStop() (cwi-dis/cwipc_unity#82, rule 4).
            // Our input queues belong to their producers; we don't close them (rule 1).
            base.Stop();
            foreach (var t in pusherThreads)
            {
                t.Stop();
            }
        }

        public override void AsyncOnStop()
        {
            // Join the push threads before unregistering: the last UnregisterTransmitter() cleans up the
            // native WebRTC connector, which the push threads may still be calling into, and they use
            // connection until they have stopped.
            // A thread that doesn't stop in time is left running (logged, stats record); we unregister anyway.
            foreach (var t in pusherThreads)
            {
                t.Join(joinTimeout);
            }
            connection.UnregisterTransmitter();
            connection = null;
            base.AsyncOnStop();
        }

        protected override void AsyncUpdate() {}

#if xxxjack_disabled
        public override SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
            return new SyncConfig.ClockCorrespondence
            {
                wallClockTime = (Timestamp)sinceEpoch.TotalMilliseconds,
                streamClockTime = uploader.get_media_time(1000)
            };
        }
#endif
    }
}
