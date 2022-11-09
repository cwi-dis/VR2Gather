using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.UserRepresentation.PointCloud
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class PCEncoder : AsyncWorker
    {
        cwipc.encodergroup encoderGroup;
        cwipc.encoder[] encoderOutputs;
        bool encodersAreBusy = false;
        PCEncoderOutputPusher[] pushers;
        System.IntPtr encoderBuffer;
        QueueThreadSafe inQueue;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;
        Queue<System.DateTime> mostRecentFeedTimes = new Queue<System.DateTime>();
        Queue<Timestamp> mostRecentFeedTimestamps = new Queue<Timestamp>();
        int nParallel = 0;
        
        public struct EncoderStreamDescription
        {
            public int octreeBits;
            public int tileNumber;
            public QueueThreadSafe outQueue;
        };
        EncoderStreamDescription[] outputs;

        public class PCEncoderOutputPusher
        {
            PCEncoder parent;
            int stream_number;
            cwipc.encoder encoder;
            QueueThreadSafe outQueue;
            NativeMemoryChunk curBuffer = null;
            Timedelta curEncodeDuration;

            public PCEncoderOutputPusher(PCEncoder _parent, int _stream_number)
            {
                parent = _parent;
                stream_number = _stream_number;
                encoder = parent.encoderOutputs[stream_number];
                outQueue = parent.outputs[stream_number].outQueue;
            }

            public void Start()
            {
            }

            public void Join()
            {
            }

            public bool LockBuffer()
            {
                lock (this)
                {
                    if (curBuffer != null) return true;
                    if (!encoder.available(false)) return false;
                    curBuffer = new NativeMemoryChunk(encoder.get_encoded_size());
                    curBuffer.info.timestamp = parent.mostRecentFeedTimestamps.Peek();
                    curEncodeDuration = (Timedelta)(System.DateTime.Now - parent.mostRecentFeedTimes.Peek()).TotalMilliseconds;
                    if (!encoder.copy_data(curBuffer.pointer, curBuffer.length))
                    {
                        Debug.LogError($"Programmer error: PCEncoder#{stream_number}: cwipc_encoder_copy_data returned false");
                    }
                    return true;
                }
            }

            public void PushBuffer()
            {
                lock(this)
                {
                    if (curBuffer == null) return;
                    Timedelta queuedDuration = outQueue.QueuedDuration();
                    bool dropped = !outQueue.Enqueue(curBuffer);
                    parent.stats.statsUpdate(dropped, curEncodeDuration, queuedDuration);
                    curBuffer = null;
                }
            }

            protected void run()
            {
                try
                {
                    Debug.Log($"PCEncoder#{stream_number}: PusherThread started");
                    // Get encoder and output queue for our stream
                    // Loop until feeder signals no more data is forthcoming
                    while (!encoder.eof())
                    {
                        if (LockBuffer())
                        {
                            PushBuffer();
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                    outQueue.Close();
                    Debug.Log($"PCEncoder#{stream_number}: PusherThread stopped");
                }
#pragma warning disable CS0168
                catch (System.Exception e)
                {
#if UNITY_EDITOR
                    throw;
#else
                Debug.Log($"PCEncoder#{stream_number}: Exception: {e.Message} Stack: {e.StackTrace}");
                Debug.LogError("Error while sending your representation to other participants.");
#endif
                }
            }
        }

        public PCEncoder(QueueThreadSafe _inQueue, EncoderStreamDescription[] _outputs) : base()
        {
            nParallel = VRT.Core.Config.Instance.PCs.encoderParallelism;
            if (_inQueue == null)
            {
                throw new System.Exception("{Name()}: inQueue is null");
            }
            inQueue = _inQueue;
            outputs = _outputs;
            int nOutputs = outputs.Length;
            encoderOutputs = new cwipc.encoder[nOutputs];
            try
            {
                encoderGroup = cwipc.new_encodergroup();
                for (int i = 0; i < nOutputs; i++)
                {
                    var op = outputs[i];
                    cwipc.encoder_params parms = new cwipc.encoder_params
                    {
                        octree_bits = op.octreeBits,
                        do_inter_frame = false,
                        exp_factor = 0,
                        gop_size = 1,
                        jpeg_quality = 75,
                        macroblock_size = 0,
                        tilenumber = op.tileNumber,
                        voxelsize = 0,
                        n_parallel = nParallel
                    };
                    var encoder = encoderGroup.addencoder(parms);
                    encoderOutputs[i] = encoder;

                }
                Start();
                Debug.Log($"{Name()}: Inited");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}: Exception during constructor: {e.Message}");
                throw;
            }
            stats = new Stats(Name());
        }
        public override string Name() {
            return $"{GetType().Name}#{instanceNumber}";
        }

        protected override void Start()
        {
            base.Start();
            int nThreads = encoderOutputs.Length;
            pushers = new PCEncoderOutputPusher[nThreads];
            for (int i = 0; i < nThreads; i++)
            {
                // Note: we need to copy i to a new variable, otherwise the lambda expression capture will bite us
                int stream_number = i;
                pushers[i] = new PCEncoderOutputPusher(this, stream_number);
            }
            foreach (var t in pushers)
            {
                t.Start();
            }
        }

        public override void OnStop()
        {
            // Signal end-of-data
            encoderGroup.close();
            // Wait for each pusherThread to see this and terminate
            foreach (var t in pushers)
            {
                t.Join();
            }
            // Clear our encoderGroup to signal the Update thread
            var tmp = encoderGroup;
            encoderGroup = null;
            // Stop the Update thread
            base.OnStop();
            // Clear the encoderGroup including all of its encoders
            tmp?.free();
            foreach (var eo in encoderOutputs)
            {
                eo.free();
            }
            Debug.Log($"{Name()}: Stopped");
            // xxxjack is encoderBuffer still used? Think not...
            if (encoderBuffer != System.IntPtr.Zero) { System.Runtime.InteropServices.Marshal.FreeHGlobal(encoderBuffer); encoderBuffer = System.IntPtr.Zero; }
        }

        protected override void Update()
        {
            base.Update();
            // If we do multi-threaded encoding we always try to obtain results
            // and we always try to feed.
            if(encodersAreBusy || nParallel > 1)
            {
                // See if encoders are done and we can feed the transmitters
                bool allDone = true;
                foreach(var t in pushers)
                {
                    if (!t.LockBuffer()) allDone = false;
                }
                if (allDone)
                {
                    foreach(var t in pushers)
                    {
                        t.PushBuffer();
                    }
                    encodersAreBusy = false;
                    mostRecentFeedTimes.Dequeue();
                    mostRecentFeedTimestamps.Dequeue();
                }
            }
            if (!encodersAreBusy || nParallel > 1)
            {
                // See if we can start encoding.
                cwipc.pointcloud pc = (cwipc.pointcloud)inQueue.Dequeue();
                if (pc != null)
                {
                    if (encoderGroup != null)
                    {
                        // Not terminating yet
                        mostRecentFeedTimes.Enqueue(System.DateTime.Now);
                        mostRecentFeedTimestamps.Enqueue(pc.timestamp());
                        encoderGroup.feed(pc);
                        encodersAreBusy = true;
                    }
                    pc.free();
                }
            }
        }


        protected class Stats : VRT.Core.BaseStats {
            public Stats(string name) : base(name) { }

            double statsTotalPointclouds = 0;
            double statsTotalDropped = 0;
            double statsTotalEncodeDuration = 0;
            double statsTotalQueuedDuration = 0;
            int statsAggregatePackets = 0;

            public void statsUpdate(bool dropped, Timedelta encodeDuration, Timedelta queuedDuration) {
                statsTotalPointclouds++;
                statsAggregatePackets++;
                statsTotalEncodeDuration += encodeDuration;
                statsTotalQueuedDuration += queuedDuration;
                if (dropped) statsTotalDropped++;

                if (ShouldOutput()) {
                    Output($"fps={statsTotalPointclouds / Interval():F2}, fps_dropped={statsTotalDropped / Interval():F2}, encoder_ms={(statsTotalEncodeDuration / statsTotalPointclouds):F2}, transmitter_queue_ms={(int)(statsTotalQueuedDuration / statsTotalPointclouds)}, aggregate_packets={statsAggregatePackets}");
                    Clear();
                    statsTotalPointclouds = 0;
                    statsTotalDropped = 0;
                    statsTotalEncodeDuration = 0;
                    statsTotalQueuedDuration = 0;
                }
            }
        }

        protected Stats stats;
    }
}