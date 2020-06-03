using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Workers {
    public class B2DWriter : BaseWorker
    {
        public struct DashStreamDescription
        {
            public uint tileNumber;
            public uint quality;
            public QueueThreadSafe inQueue;
        };

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public bin2dash.connection uploader;
        public string url;
        DashStreamDescription[] descriptions;
        public class PusherThread
        {
            B2DWriter parent;
            int stream_index;
            DashStreamDescription description;
            System.Threading.Thread myThread;

            public PusherThread(B2DWriter _parent, int _stream_index, DashStreamDescription _description)
            {
                parent = _parent;
                stream_index = _stream_index;
                description = _description;
                myThread = new System.Threading.Thread(run);
            }

            public void Start()
            {
                myThread.Start();
            }

            public void Join()
            {
                myThread.Join();
            }

            protected void run()
            {
                try
                {
                    Debug.Log($"B2DWriter#{parent.instanceNumber}.{stream_index}: PusherThread started");
                    QueueThreadSafe queue = description.inQueue;
                    while (!queue.IsClosed())
                    {
                        if (queue._CanDequeue())
                        {
                            NativeMemoryChunk mc = (NativeMemoryChunk)queue.Dequeue();
                            statsUpdate((int)mc.length); // xxxjack needs to be changed to be per-stream
                            if (!parent.uploader.push_buffer(stream_index, mc.pointer, (uint)mc.length))
                                Debug.Log($"B2DWriter#{parent.instanceNumber}.{stream_index}({parent.url}): ERROR sending data");
                            mc.free();
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                    Debug.Log($"B2DWriter#{parent.instanceNumber}.{stream_index}: PusherThread stopped");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"B2DWriter#{stream_index}: Exception: {e.Message} Stack: {e.StackTrace}");
#if UNITY_EDITOR
                    if (UnityEditor.EditorUtility.DisplayDialog("Exception", "Exception in PusherThread", "Stop", "Continue"))
                        UnityEditor.EditorApplication.isPlaying = false;
#endif
                }

            }

            System.DateTime statsLastTime;
            double statsTotalBytes;
            double statsTotalPackets;

            public void statsUpdate(int nBytes)
            {
                if (statsLastTime == null)
                {
                    statsLastTime = System.DateTime.Now;
                    statsTotalBytes = 0;
                    statsTotalPackets = 0;
                }
                if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(10))
                {
                    Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: B2DWriter#{parent.instanceNumber}.{stream_index}: {statsTotalPackets / 10} fps, {(int)(statsTotalBytes / statsTotalPackets)} bytes per packet");
                    statsTotalBytes = 0;
                    statsTotalPackets = 0;
                    statsLastTime = System.DateTime.Now;
                }
                statsTotalBytes += nBytes;
                statsTotalPackets += 1;
            }
        }

        PusherThread[] pusherThreads;


        public B2DWriter(string _url, string _streamName, string fourcc, int _segmentSize, int _segmentLife, DashStreamDescription[] _descriptions) : base(WorkerType.End)
        {
            if (_descriptions == null || _descriptions.Length == 0)
            {
                throw new System.Exception("B2DWriter: descriptions is null or empty");
            }
            if (fourcc.Length != 4)
            {
                throw new System.Exception($"B2DWriter: 4CC is \"{fourcc}\" which is not exactly 4 characters");
            }
            uint fourccInt = bin2dash.VRT_4CC(fourcc[0], fourcc[1], fourcc[2], fourcc[3]);
            descriptions = _descriptions;
            try
            {
                //if (cfg.fileMirroring) bw = new BinaryWriter(new FileStream($"{Application.dataPath}/../{cfg.streamName}.dashdump", FileMode.Create));
                url = _url;
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(_streamName))
                {
                    Debug.LogError($"B2DWriter#{instanceNumber}: configuration error: url or streamName not set");
                    throw new System.Exception($"B2DWriter#{instanceNumber}: configuration error: url or streamName not set");
                }
                // xxxjack Is this the correct way to initialize an array of structs?
                Debug.Log($"xxxjack B2DWriter: {descriptions.Length} output streams");
                bin2dash.StreamDesc[] b2dDescriptors = new bin2dash.StreamDesc[descriptions.Length];
                for (int i = 0; i < descriptions.Length; i++)
                {
                    b2dDescriptors[i] = new bin2dash.StreamDesc
                    {
                        MP4_4CC = fourccInt,
                        tileNumber = descriptions[i].tileNumber,
                        quality = descriptions[i].quality
                    };
                    if (descriptions[i].inQueue == null)
                    {
                        throw new System.Exception($"B2DWriter#{instanceNumber}.{i}: inQueue");
                    }
                }
                uploader = bin2dash.create(_streamName, b2dDescriptors, url, _segmentSize, _segmentLife);
                if (uploader != null)
                {
                    Debug.Log($"B2DWriter({url + _streamName}.mpd: started");
                    Start();
                }
                else
                    throw new System.Exception($"B2DWriter#{instanceNumber}: vrt_create: failed to create uploader {url + _streamName}.mpd");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"B2DWriter#{instanceNumber}({url}:{e.Message}");
                throw e;
            }
        }

        protected override void Start()
        {
            base.Start();
            int nThreads = descriptions.Length;
            pusherThreads = new PusherThread[nThreads];
            for (int i = 0; i < nThreads; i++)
            {
                // Note: we need to copy i to a new variable, otherwise the lambda expression capture will bite us
                int stream_number = i;
                pusherThreads[i] = new PusherThread(this, i, descriptions[i]);
            }
            foreach (var t in pusherThreads)
            {
                t.Start();
            }
        }

        public override void OnStop()
        {
            // Signal that no more data is forthcoming to every pusher
            foreach (var d in descriptions)
            {
                d.inQueue.Close();
            }
            // Stop our thread
            base.OnStop();
            uploader?.free();
            uploader = null;
            // wait for pusherThreads to terminate
            foreach (var t in pusherThreads)
            {
                t.Join();
            }
            Debug.Log($"B2DWriter#{instanceNumber} {url} Stopped");
        }

        protected override void Update()
        {
            base.Update();
            // xxxjack anything to do?
            System.Threading.Thread.Sleep(10);
        }
    }
}
