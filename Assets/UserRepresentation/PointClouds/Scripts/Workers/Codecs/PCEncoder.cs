using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Workers {
    public class PCEncoder : BaseWorker {
        cwipc.encodergroup encoderGroup;
        cwipc.encoder[] encoderOutputs;
        System.Threading.Thread[] pusherThreads;
        System.IntPtr encoderBuffer;
        QueueThreadSafe inQueue;
        public struct EncoderStreamDescription
        {
            public int octreeBits;
            public int tileNumber;
            public QueueThreadSafe outQueue;
        };
        EncoderStreamDescription[] outputs;

        public PCEncoder(QueueThreadSafe _inQueue, EncoderStreamDescription[] _outputs ) :base(WorkerType.Run) {
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
                        voxelsize = 0
                    };
                    var encoder = encoderGroup.addencoder(parms);
                    encoderOutputs[i] = encoder;

                }
                Start();
                Debug.Log($"{Name()}: Inited");
            }
            catch (System.Exception e) {
                Debug.Log($"{Name()}: Exception during constructor: {e.Message}");
                throw e;
            }
        }

        protected override void Start()
        {
            base.Start();
            int nThreads = encoderOutputs.Length;
            pusherThreads = new System.Threading.Thread[nThreads];
            for (int i=0; i<nThreads; i++)
            {
                // Note: we need to copy i to a new variable, otherwise the lambda expression capture will bite us
                int stream_number = i;
                pusherThreads[i] = new System.Threading.Thread(
                    () => PusherThread(stream_number)
                    );
            }
            foreach(var t in pusherThreads)
            {
                t.Start();
            }
        }

        public override void OnStop() {
            // Signal end-of-data
            encoderGroup.close();
            // Wait for each pusherThread to see this and terminate
            foreach(var t in pusherThreads)
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
            foreach(var eo in encoderOutputs)
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
            cwipc.pointcloud pc = (cwipc.pointcloud)inQueue.Dequeue();
            if (pc == null) return; // Terminating
            if (encoderGroup != null) {
                // Not terminating yet
                encoderGroup.feed(pc);
            }
            pc.free();
        }

        protected  void PusherThread(int stream_number)
        {
            try
            {
                Debug.Log($"PCEncoder#{stream_number}: PusherThread started");
                // Get encoder and output queue for our stream
                cwipc.encoder encoder = encoderOutputs[stream_number];
                QueueThreadSafe outQueue = outputs[stream_number].outQueue;
                // Loop until feeder signals no more data is forthcoming
                while (!encoder.eof())
                {
                    if (encoder.available(true))
                    {
                        NativeMemoryChunk mc = new NativeMemoryChunk(encoder.get_encoded_size());
                        if (encoder.copy_data(mc.pointer, mc.length))
                        {
                            outQueue.Enqueue(mc);
                        }
                        else
                        {
                            Debug.LogError($"Programmer error: PCEncoder#{stream_number}: cwipc_encoder_copy_data returned false");
                        }
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                }
                outQueue.Close();
                Debug.Log($"PCEncoder#{stream_number}: PusherThread stopped");
            }
            catch (System.Exception e)
            {
                Debug.Log($"PCEncoder#{stream_number}: Exception: {e.Message} Stack: {e.StackTrace}");
                Debug.LogError("Error while sending your representation to other participants.");
#if UNITY_EDITOR
                if (UnityEditor.EditorUtility.DisplayDialog("Exception", "Exception in PusherThread", "Stop", "Continue"))
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
            }
        }
    }
}