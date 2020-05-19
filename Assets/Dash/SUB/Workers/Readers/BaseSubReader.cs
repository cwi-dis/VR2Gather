using UnityEngine;

namespace Workers {
    public class BaseSubReader : BaseWorker {

        public delegate bool NeedsSomething();

        string url;
        protected int streamCount;
        protected uint[] stream4CCs;
        bool bDropFrames=false;
        sub.connection subHandle;
        bool isPlaying;
        sub.FrameInfo info = new sub.FrameInfo();
        int numberOfUnsuccessfulReceives;
//        object subLock = new object();
        System.DateTime subRetryNotBefore = System.DateTime.Now;

        protected QueueThreadSafe[] outQueues;
        protected int[] streamIndexes;

        // Mainly for debug messages:
        static int subCount;
        string subName;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        protected BaseSubReader(string _url, string _streamName, int _initialDelay, bool _bDropFrames = false) : base(WorkerType.Init) { // Orchestrator Based SUB
            bDropFrames = _bDropFrames;
            if (_url == "" || _url == null || _streamName == "")
            {
                Debug.LogError($"{this.GetType().Name}#{instanceNumber}: configuration error: url or streamName not set");
                throw new System.Exception($"{this.GetType().Name}#{instanceNumber}: configuration error: url or streamName not set");
            }
            if (_streamName != null)
            {
                if (!_streamName.Contains(".mpd")) _streamName += ".mpd";
                _url += _streamName;
            }
            url = _url;
            if (_initialDelay != 0)
            {
                // We do not try to start play straight away, to work around bugs when creating the SUB before
                // the dash data is stable. To be removed at some point in the future (Jack, 20200123)
                Debug.Log($"{this.GetType().Name}#{instanceNumber}: Delaying {_initialDelay} seconds before playing {url}");
                subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(_initialDelay);
            }
            Start();
        }

        public override void OnStop() {
            lock (this)
            {
                if (subHandle != null) subHandle.free();
                subHandle = null;
                isPlaying = false;
            }
            base.OnStop();
            Debug.Log($"{this.GetType().Name}#{instanceNumber} {subName} {url} Stopped");
        }

        protected void UnsuccessfulCheck(int _size) {
            if (_size == 0) {
                //
                // We want to delay a bit before retrying. Ideally we delay until we know the next frame will
                // be available, but that is difficult. 10ms is about 30% of a pointcloud frame duration. But it
                // may be far too long for audio. Need to check.
                numberOfUnsuccessfulReceives++;
                System.Threading.Thread.Sleep(10);
                if (numberOfUnsuccessfulReceives > 2000) {
                    lock (this) {
                        Debug.Log($"{this.GetType().Name}#{instanceNumber} {subName} {url}: Too many receive errors. Closing SUB player, will reopen.");
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

        protected void InitDash() {
            lock (this) {
                if (System.DateTime.Now < subRetryNotBefore) return;
                if (subHandle == null) {
                    subName = $"source_from_sub_{++subCount}";
                    subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(5);
                    subHandle = sub.create(subName);
                    if (subHandle == null) throw new System.Exception($"{this.GetType().Name}: sub_create({url}) failed");
                    Debug.Log($"{this.GetType().Name}#{instanceNumber} {subName}: retry sub.create({url}) successful.");
                }
                isPlaying = subHandle.play(url);
                if (!isPlaying) {
                    subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(5);
                    Debug.Log($"{this.GetType().Name}#{instanceNumber} {subName}: sub.play({url}) failed, will try again later");
                    return;
                }
                streamCount = subHandle.get_stream_count();
                stream4CCs = new uint[streamCount];
                for (int i = 0; i < streamCount; i++)
                {
                    stream4CCs[i] = subHandle.get_stream_4cc(i);
                }
                Debug.Log($"{this.GetType().Name}#{instanceNumber} {subName}: sub.play({url}) successful, {streamCount} streams.");
            }
        }

        protected override void Update() {
            base.Update();
            if (!isPlaying) {
                InitDash();
                return;
            }
            // We loop over all streams, reading data and pushing into the queue (or dropping)for every stream.
            for (int outIndex = 0; outIndex < outQueues.Length; outIndex++)
            {
                int streamNumber = streamIndexes[outIndex];
                QueueThreadSafe outQueue = outQueues[outIndex];

                // Ignore if this stream was not found in the MPD
                if (streamNumber < 0) continue;

                // Skip this stream if the output queue is full and we don't wan to drop frames.
                if (!outQueue.Free() && !bDropFrames) continue;

                lock (this)
                {
                    // Shoulnd't happen, but's let make sure
                    if (subHandle == null)
                    {
                        Debug.Log("{this.GetType().Name}#{instanceNumber} {subName}: subHandle was closed");
                        return;
                    }
                    // See how many bytes we need to allocate
                    int bytesNeeded = subHandle.grab_frame(streamNumber, System.IntPtr.Zero, 0, ref info);

                    // Sideline: count number of unsuccessful receives so we can try and repoen the stream
                    UnsuccessfulCheck(bytesNeeded);

                    // If not data is currently available on this stream there is nothing more to do (for this stream)
                    if (bytesNeeded == 0) continue;

                    // Allocate and read.
                    NativeMemoryChunk mc = new NativeMemoryChunk(bytesNeeded);
                    int bytesRead = subHandle.grab_frame(streamNumber, mc.pointer, mc.length, ref info);
                    if (bytesRead != bytesNeeded)
                    {
                        Debug.LogError($"{this.GetType().Name}#{instanceNumber} {subName}: sub_grab_frame returned {bytesRead} bytes after promising {bytesNeeded}");
                        mc.free();
                        continue;
                    }
                    if (!outQueue.Free())
                    {
                        Debug.Log($"{this.GetType().Name} {subName}: frame dropped.");
                        mc.free();
                    }

                    // Push to queue
                    mc.info = info;
                    outQueue.Enqueue(mc);

                    statsUpdate(bytesRead);
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

