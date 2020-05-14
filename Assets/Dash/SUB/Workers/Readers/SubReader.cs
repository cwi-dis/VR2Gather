using UnityEngine;

namespace Workers {
    public class SUBReader : BaseWorker {

        public delegate bool NeedsSomething();

        NeedsSomething needsVideo;
        NeedsSomething needsAudio;
        bool isFunnyAVSub = false;

        public enum CCCC : int {
            MP4A = 0x6134706D,
            AVC1 = 0x31637661,
            AAC = 0x5f636161,
            H264 = 0x34363268
        };

        const int sleepWhenNoDataReceived = 10;     // Sleep 10ms when SUB has no data for us
        const int maxUnsuccessfulReceives = 2000;   // After 2000 times not receiving data we give up.
        const int subRetryWaitTime = 2;             // After unsuccessful start() we wait 2 seconds before retrying.

        string url;
        int streamNumber;
        int streamCount;
        int videoStream = 0;
        bool bDropFrames=false;
        sub.connection subHandle;
        bool isPlaying;
        sub.FrameInfo info = new sub.FrameInfo();
        int numberOfUnsuccessfulReceives;
        int dampedSize = 0;
        static int subCount;
        string subName;
//        object subLock = new object();
        System.DateTime subRetryNotBefore = System.DateTime.Now;

        QueueThreadSafe outQueue;
        QueueThreadSafe out2Queue;

        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public SUBReader(string _url, string _streamName, int _streamNumber, int _initialDelay, QueueThreadSafe _outQueue, bool _bDropFrames=false) : base(WorkerType.Init) { // Orchestrator Based SUB
            if (_outQueue == null)
            {
                throw new System.Exception("SUBReader: outQueue is null");
            }
            needsVideo = null;
            needsAudio = null;
            outQueue = _outQueue;
            out2Queue = null;
            bDropFrames = _bDropFrames;
            if (_url == "" || _url == null || _streamName == "" || _streamName == null)
            {
                Debug.LogError($"SUBReader#{instanceNumber}: configuration error: url or streamName not set");
                throw new System.Exception($"SUBReader#{instanceNumber}: configuration error: url or streamName not set");
            }
            if (!_streamName.Contains(".mpd")) _streamName += ".mpd";
            url = _url + _streamName;
            streamNumber = _streamNumber;
            if (_initialDelay != 0)
            {
                // We do not try to start play straight away, to work around bugs when creating the SUB before
                // the dash data is stable. To be removed at some point in the future (Jack, 20200123)
                Debug.Log($"SUBReader#{instanceNumber}: Delaying {_initialDelay} seconds before playing {url}");
                subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(_initialDelay);
            }
            try {
                Start();
            }
            catch (System.Exception e)
            {
                throw e;
            }
        }

        public SUBReader(string cfg, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue, NeedsSomething needsVideo = null, NeedsSomething needsAudio = null, bool _bDropFrames = false) : base(WorkerType.Init) { // VideoDecoder Based SUB
            this.needsVideo = needsVideo;
            this.needsAudio = needsAudio;
            isFunnyAVSub = true;
            outQueue = _outQueue;
            out2Queue = _out2Queue;
            url = cfg;
            streamNumber = 0;
            bDropFrames = _bDropFrames;
            try {
                //signals_unity_bridge_pinvoke.SetPaths();
                subName = $"source_from_sub_{++subCount}";
                subHandle = sub.create(subName);
                if (subHandle != null) {
                    Debug.Log($"SUBReader#{instanceNumber}: sub.create({url}) successful.");
                    isPlaying = subHandle.play(url);
                    if (!isPlaying) {
                        Debug.Log($"SUBReader#{instanceNumber}: sub_play({url}) failed, will try again later");
                    } else {
                        streamCount = Mathf.Min(2, subHandle.get_stream_count());
                        CCCC cc;
                        for (int i = 0; i < streamCount; ++i) {
                            cc = (CCCC)subHandle.get_stream_4cc(i);
                            Debug.Log(cc);
                        }
                        if ((CCCC)subHandle.get_stream_4cc(0) == CCCC.AVC1 || (CCCC)subHandle.get_stream_4cc(0) == CCCC.H264) videoStream = 0;
                        else videoStream = 1;
                        streamNumber = videoStream;
                    }
                    Start();
                }
                else
                    throw new System.Exception($"SUBReader#{instanceNumber}: sub_create({url}) failed");
            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public override void OnStop() {
            lock (this)
            {
                if (subHandle != null) subHandle.free();
                subHandle = null;
                isPlaying = false;
            }
            base.OnStop();
            Debug.Log($"SUBReader#{instanceNumber} {subName} {url} Stopped");
        }

        protected void UnsuccessfulCheck(int _size) {
            if (_size == 0) {
                //
                // We want to delay a bit before retrying. Ideally we delay until we know the next frame will
                // be available, but that is difficult. 10ms is about 30% of a pointcloud frame duration. But it
                // may be far too long for audio. Need to check.
                numberOfUnsuccessfulReceives++;
                System.Threading.Thread.Sleep(sleepWhenNoDataReceived);
                if (numberOfUnsuccessfulReceives > maxUnsuccessfulReceives) {
                    lock (this) {
                        Debug.Log($"SUBReader#{instanceNumber} {subName} {url}: Too many receive errors. Closing SUB player, will reopen.");
                        if (subHandle != null) subHandle.free();
                        subHandle = null;
                        isPlaying = false;
                        subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(5);
                        numberOfUnsuccessfulReceives = 0;
                    }
                }
                return;
            }
            numberOfUnsuccessfulReceives = 0;
        }

        protected void retryPlay() {
            lock (this) {
                if (System.DateTime.Now < subRetryNotBefore) return;
                if (subHandle == null) {
                    subName = $"source_from_sub_{++subCount}";
                    subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(subRetryWaitTime);
                    subHandle = sub.create(subName);
                    if (subHandle == null) throw new System.Exception($"PCSUBReader: sub_create({url}) failed");
                    Debug.Log($"SUBReader#{instanceNumber} {subName}: retry sub.create({url}) successful.");
                }
                isPlaying = subHandle.play(url);
                if (!isPlaying) {
                    subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(5);
                    Debug.Log($"SUBReader#{instanceNumber} {subName}: sub.play({url}) failed, will try again later");
                    return;
                }
                streamCount = subHandle.get_stream_count();
                Debug.Log($"SUBReader#{instanceNumber} {subName}: sub.play({url}) successful, {streamCount} streams.");
            }
        }

        protected override void Update() {
            int bytesNeeded;
            base.Update();
            if (!isPlaying) retryPlay();
            else {
                // Try to read from audio.
                if (isFunnyAVSub && streamCount > 1 && ( out2Queue.Free() || bDropFrames)) {
                    // Attempt to receive, if we are playing
                    bytesNeeded = subHandle.grab_frame(1 - streamNumber, System.IntPtr.Zero, 0, ref info);
                    if (bytesNeeded != 0 && out2Queue != null) {
                        NativeMemoryChunk mc = new NativeMemoryChunk(bytesNeeded);
                        int bytesRead = subHandle.grab_frame(1 - streamNumber, mc.pointer, mc.length, ref info);
                        if (bytesRead == bytesNeeded) {
                            if (out2Queue.Free()) {
                                mc.info = info;
                                //while(bRunning && out2Queue.Count > 2) { System.Threading.Thread.Sleep(10); }
                                out2Queue.Enqueue(mc);
                            } else {
                                Debug.LogError($"PCSUBReader#{instanceNumber} {subName}: frame dropped.");
                                mc.free();
                            }
                        } else
                            Debug.LogError($"PCSUBReader {subName}: sub_grab_frame returned {bytesRead} bytes after promising {bytesNeeded}");
                    }
                }
                if (outQueue.Free() || bDropFrames) {
                    // Attempt to receive, if we are playing
                    bytesNeeded = subHandle.grab_frame(streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                                                                                                       // If we are not playing or if we didn't receive anything we restart after 1000 failures.
                    UnsuccessfulCheck(bytesNeeded);
                    if (bytesNeeded != 0) {
                        NativeMemoryChunk mc = new NativeMemoryChunk(bytesNeeded);
                        int bytesRead = subHandle.grab_frame(streamNumber, mc.pointer, mc.length, ref info);
                        if (bytesRead == bytesNeeded) {
                            if (outQueue.Free()) {
                                mc.info = info;
                                statsUpdate(bytesRead);
                                outQueue.Enqueue(mc);
                            } else {
                                Debug.Log($"PCSUBReader {subName}: frame droped.");
                                mc.free();
                            }
                        } else
                            Debug.LogError($"SUBReader#{instanceNumber} {subName}: sub_grab_frame returned {bytesRead} bytes after promising {bytesNeeded}");
                    }
                }
            }
        }

        System.DateTime statsLastTime;
        double statsTotalBytes;
        double statsTotalPackets;

        public void statsUpdate(int nBytes) {
            if (statsLastTime == null) {
                statsLastTime = System.DateTime.Now;
                statsTotalBytes = 0;
                statsTotalPackets = 0;
            }
            if (System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(10)) {
                Debug.Log($"stats: ts={(int)System.DateTime.Now.TimeOfDay.TotalSeconds}: SubReader#{instanceNumber}: {statsTotalPackets / 10} fps, {(int)(statsTotalBytes / statsTotalPackets)} bytes per packet");
                statsTotalBytes = 0;
                statsTotalPackets = 0;
                statsLastTime = System.DateTime.Now;
            }
            statsTotalBytes += nBytes;
            statsTotalPackets += 1;
        }
    }
}

