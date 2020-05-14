using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Workers {
    public class B2DWriter : BaseWorker {
        public struct DashStreamDescription
        {
            public uint tileNumber;
            public uint quality;
            public QueueThreadSafe inQueue;
        };

        bin2dash.connection uploader;
        string url;
        DashStreamDescription[] descriptions;
        System.Threading.Thread[] pusherThreads;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;


        public B2DWriter(string _url, string _streamName, string fourcc, int _segmentSize, int _segmentLife, DashStreamDescription[] _descriptions) : base(WorkerType.End)
        {
            if (_descriptions == null || _descriptions.Length == 0)
            {
                throw new System.Exception("B2DWriter: descriptions is null or empty");
            }
            if (_descriptions.Length != 1)
            {
                throw new System.Exception("B2DWriter: descriptions must have length 1");
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
                if ( string.IsNullOrEmpty(url) || string.IsNullOrEmpty(_streamName) )
                {
                    Debug.LogError($"B2DWriter#{instanceNumber}: configuration error: url or streamName not set");
                    throw new System.Exception($"B2DWriter#{instanceNumber}: configuration error: url or streamName not set");
                }
                // xxxjack Is this the correct way to initialize an array of structs?
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
                if (uploader != null) {
                    Debug.Log($"B2DWriter({url + _streamName}.mpd: started");
                    Start();
                }
                else
                    throw new System.Exception($"B2DWriter#{instanceNumber}: vrt_create: failed to create uploader {url + _streamName}.mpd");
            }
            catch (System.Exception e) {
                Debug.LogError($"B2DWriter#{instanceNumber}({url}:{e.Message}");
                throw e;
            }
        }

        protected override void Start()
        {
            base.Start();
            int nThreads = descriptions.Length;
            pusherThreads = new System.Threading.Thread[nThreads];
            for (int i = 0; i < nThreads; i++)
            {
                // Note: we need to copy i to a new variable, otherwise the lambda expression capture will bite us
                int stream_number = i;
                pusherThreads[i] = new System.Threading.Thread(
                    () => PusherThread(stream_number)
                    );
            }
            foreach (var t in pusherThreads)
            {
                t.Start();
            }
        }

        public override void OnStop() {
            // Signal that no more data is forthcoming to every pusher
            foreach(var d in descriptions)
            {
                d.inQueue.Closed = true;
            }
            // wait for pusherThreads to terminate
            foreach (var t in pusherThreads)
            {
                t.Join();
            }
            // Stop our thread
            var tmp = uploader;
            uploader = null;
            base.OnStop();
            tmp?.free();
            Debug.Log($"B2DWriter#{instanceNumber} {url} Stopped");
        }

        protected override void Update()
        {
            base.Update();
            // xxxjack anything to do?
            System.Threading.Thread.Sleep(10);
        }

        protected void PusherThread(int stream_index)
        {
            try
            {
                Debug.Log($"B2DWriter#{instanceNumber}.{stream_index}: PusherThread started");
                QueueThreadSafe queue = descriptions[stream_index].inQueue;
                while (!queue.Closed)
                {
                    if (queue.Count > 0)
                    {
                        NativeMemoryChunk mc = (NativeMemoryChunk)queue.Dequeue();
                        statsUpdate((int)mc.length); // xxxjack needs to be changed to be per-stream
                        if (!uploader.push_buffer(stream_index, mc.pointer, (uint)mc.length))
                            Debug.Log($"B2DWriter#{instanceNumber}.{stream_index}({url}): ERROR sending data");
                        mc.free();
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                }
                Debug.Log($"B2DWriter#{instanceNumber}.{stream_index}: PusherThread started");
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

        public void statsUpdate(int nBytes) {
            int stream_index = 999;
            if (statsLastTime == null) {
                statsLastTime = System.DateTime.Now;
                statsTotalBytes = 0;
                statsTotalPackets = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(10)) {
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: B2DWriter#{instanceNumber}.{stream_index}: {statsTotalPackets / 10} fps, {(int)(statsTotalBytes / statsTotalPackets)} bytes per packet");
                statsTotalBytes = 0;
                statsTotalPackets = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalBytes += nBytes;
            statsTotalPackets += 1;
        }
    }
}
