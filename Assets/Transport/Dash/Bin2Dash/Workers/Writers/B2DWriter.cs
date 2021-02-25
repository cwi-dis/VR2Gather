using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VRTCore;

namespace VRT.Transport.Dash
{

    public class B2DWriter : BaseWriter
    {
        public struct DashStreamDescription
        {
            public string name;
            public uint tileNumber;
            public uint quality;
            public QueueThreadSafe inQueue;
        };

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public bin2dash.connection uploader;
        public string url;
        DashStreamDescription[] descriptions;
        public class B2DPushThread
        {
            B2DWriter parent;
            int stream_index;
            DashStreamDescription description;
            System.Threading.Thread myThread;

            public B2DPushThread(B2DWriter _parent, int _stream_index, DashStreamDescription _description)
            {
                parent = _parent;
                stream_index = _stream_index;
                description = _description;
                myThread = new System.Threading.Thread(run);
                myThread.Name = Name();
                stats = new Stats(Name());
            }

            public string Name()
            {
                return $"{parent.Name()}.{stream_index}";
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
                    Debug.Log($"{Name()}: thread started");
                    QueueThreadSafe queue = description.inQueue;
                    while (!queue.IsClosed())
                    {
                        NativeMemoryChunk mc = (NativeMemoryChunk)queue.Dequeue();
                        if (mc == null) continue; // Probably closing...
                        stats.statsUpdate(mc.length); // xxxjack needs to be changed to be per-stream
                        if (!parent.uploader.push_buffer(stream_index, mc.pointer, (uint)mc.length))
                            Debug.Log($"{Name()}({parent.url}): ERROR sending data");
                        mc.free();
                    }
                    Debug.Log($"{Name()}: thread stopped");
                }
                catch (System.Exception e)
                {
                    Debug.Log($"{Name()}: Exception: {e.Message} Stack: {e.StackTrace}");
                    Debug.LogError("Error while sending visual representation or audio to other participants");
#if UNITY_EDITOR
                    if (UnityEditor.EditorUtility.DisplayDialog("Exception", "Exception in PusherThread", "Stop", "Continue"))
                        UnityEditor.EditorApplication.isPlaying = false;
#endif
                }

            }

            protected class Stats : VRT.Core.BaseStats
            {
                public Stats(string name) : base(name) { }

                double statsTotalBytes = 0;
                double statsTotalPackets = 0;

                public void statsUpdate(int nBytes)
                {
                    if (ShouldClear())
                    {
                        Clear();
                        statsTotalBytes = 0;
                        statsTotalPackets = 0;
                    }

                    statsTotalBytes += nBytes;
                    statsTotalPackets += 1;

                    if (ShouldOutput())
                    {
                        Output($"fps={statsTotalPackets / Interval()}, bytes_per_packet={(int)(statsTotalBytes / (statsTotalPackets == 0 ? 1 : statsTotalPackets))}");
                        statsTotalBytes = 0;
                        statsTotalPackets = 0;
                        statsLastTime = System.DateTime.Now;
                    }
                }
            }

            protected Stats stats;
        }
 
        B2DPushThread[] pusherThreads;


        public B2DWriter(string _url, string _streamName, string fourcc, int _segmentSize, int _segmentLife, DashStreamDescription[] _descriptions) : base(WorkerType.End)
        {
            if (_descriptions == null || _descriptions.Length == 0)
            {
                throw new System.Exception($"{Name()}: descriptions is null or empty");
            }
            if (fourcc.Length != 4)
            {
                throw new System.Exception($"{Name()}: 4CC is \"{fourcc}\" which is not exactly 4 characters");
            }
            uint fourccInt = bin2dash.VRT_4CC(fourcc[0], fourcc[1], fourcc[2], fourcc[3]);
            descriptions = _descriptions;
            try
            {
                //if (cfg.fileMirroring) bw = new BinaryWriter(new FileStream($"{Application.dataPath}/../{cfg.streamName}.dashdump", FileMode.Create));
                url = _url;
                if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(_streamName))
                {
                    Debug.LogError($"{Name()}: configuration error: url or streamName not set");
                    throw new System.Exception($"{Name()}: configuration error: url or streamName not set");
                }
                // xxxjack Is this the correct way to initialize an array of structs?
                Debug.Log($"xxxjack {Name()}: {descriptions.Length} output streams");
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
                        throw new System.Exception($"{Name()}.{i}: inQueue");
                    }
                }
                uploader = bin2dash.create(_streamName, b2dDescriptors, url, _segmentSize, _segmentLife);
                if (uploader != null)
                {
                    Debug.Log($"{Name()}: started {url + _streamName}.mpd");
                    Start();
                }
                else
                    throw new System.Exception($"{Name()}: vrt_create: failed to create uploader {url + _streamName}.mpd");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{Name()}({url}) Exception:{e.Message}");
                throw e;
            }
        }

        public override string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }


        protected override void Start()
        {
            base.Start();
            int nThreads = descriptions.Length;
            pusherThreads = new B2DPushThread[nThreads];
            for (int i = 0; i < nThreads; i++)
            {
                // Note: we need to copy i to a new variable, otherwise the lambda expression capture will bite us
                int stream_number = i;
                pusherThreads[i] = new B2DPushThread(this, i, descriptions[i]);
            }
            foreach (var t in pusherThreads)
            {
                t.Start();
            }
        }

        public override void OnStop()
        {
            // Signal that no more data is forthcoming to every pusher
            for (int i = 0; i < descriptions.Length; i++)
            {
                var d = descriptions[i];
                if (!d.inQueue.IsClosed())
                {
                    Debug.LogWarning($"{Name()}.{i}: input queue not closed. Closing.");
                    d.inQueue.Close();
                }
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
            Debug.Log($"{Name()} {url} Stopped");
        }

        protected override void Update()
        {
            base.Update();
            // xxxjack anything to do?
            System.Threading.Thread.Sleep(10);
        }

        public override SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);

            return new SyncConfig.ClockCorrespondence
            {
                wallClockTime = (long)sinceEpoch.TotalMilliseconds,
                streamClockTime = uploader.get_media_time(1000)
            };
        }
    }
}
