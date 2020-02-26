using UnityEngine;

namespace Workers {
    public class SUBReader : BaseWorker {

        public delegate bool NeedsSomething();

        NeedsSomething needsVideo;
        NeedsSomething needsAudio;

        public enum CCCC : int {
            MP4A = 0x6134706D,
            AVC1 = 0x31637661
        };

        string url;
        int streamNumber;
        int streamCount;
        int videoStream = 0;
        sub.connection subHandle;
        bool isPlaying;
        byte[] currentBufferArray;
        System.IntPtr currentBuffer;
        System.Runtime.InteropServices.GCHandle gch;
        sub.FrameInfo info = new sub.FrameInfo();
        int numberOfUnsuccessfulReceives;
        int dampedSize = 0;
        static int subCount;
        string subName;
        object subLock = new object();
        System.DateTime subRetryNotBefore = System.DateTime.Now;


        public SUBReader(Config._User._SUBConfig cfg, string _url = "") : base(WorkerType.Init) { // Orchestrator Based SUB
            needsVideo = null;
            needsAudio = null;
            if (_url == string.Empty)
                url = cfg.url;
            else
                url = _url + cfg.streamName;
            streamNumber = cfg.streamNumber;
            if (cfg.initialDelay != 0)
            {
                // We do not try to start play straight away, to work around bugs when creating the SUB before
                // the dash data is stable. To be removed at some point in the future (Jack, 20200123)
                Debug.Log($"Delaying {cfg.initialDelay} seconds before playing {url}");
                subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(cfg.initialDelay);
                Debug.Log($"ctor xxxjack now={System.DateTime.Now} retryNotBefore={subRetryNotBefore}");
            }
            try {
                retryPlay();
                Start();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"xxxjack Exception {e.ToString()} caught in SUBReader constructor. Message={e.Message}, stacktrace={e.StackTrace}.");
                throw e;
            }
        }

        public SUBReader(string cfg, NeedsSomething needsVideo = null, NeedsSomething needsAudio = null) : base(WorkerType.Init) { // VideoDecoder Based SUB
            this.needsVideo = needsVideo;
            this.needsAudio = needsAudio;
            url = cfg;
            streamNumber = 0;
            try {
                //signals_unity_bridge_pinvoke.SetPaths();
                subName = $"source_from_sub_{++subCount}";
                subHandle = sub.create(subName);
                if (subHandle != null) {
                    //Debug.LogError("xxxjack very suspiciously-looking code in SUBReader called...");
                    Debug.Log($"SubReader: sub.create({url}) successful.");
                    isPlaying = subHandle.play(url);
                    if (!isPlaying) {
                        Debug.Log($"SubReader {subName}: sub_play({url}) failed, will try again later");
                    } else {
                        streamCount = Mathf.Min(2, subHandle.get_stream_count());
                        if ((CCCC)subHandle.get_stream_4cc(0) == CCCC.AVC1) videoStream = 0;
                        else videoStream = 1;
                        streamNumber = videoStream;
                    }
                    Start();
                }
                else
                    throw new System.Exception($"PCSUBReader: sub_create({url}) failed");
            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public override void OnStop() {
            lock (subLock)
            {
                if (subHandle != null) subHandle.free();
                subHandle = null;
                isPlaying = false;
            }
            base.OnStop();
            Cleaner();
            Debug.Log($"SUBReader {subName} {url} Stopped");
        }

        protected void UnsuccessfulCheck(int _size) {
            if (_size == 0) {
                numberOfUnsuccessfulReceives++;
                if (numberOfUnsuccessfulReceives > 2000) {
                    lock (subLock)
                    {
                        Debug.LogWarning($"SubReader {subName} {url}: Too many receive errors. Closing SUB player, will reopen.");
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
            lock (subLock)
            {
                if (isPlaying) return;

                if (System.DateTime.Now < subRetryNotBefore) return;

                if (subHandle == null)
                {
                    subName = $"source_from_sub_{++subCount}";
                    subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(5);
                    subHandle = sub.create(subName);
                    if (subHandle == null)
                    {
                        throw new System.Exception($"PCSUBReader: sub_create({url}) failed");
                    }
                    Debug.Log($"SubReader {subName}: retry sub.create({url}) successful.");
                }
                isPlaying = subHandle.play(url);
                if (!isPlaying)
                {
                    subRetryNotBefore = System.DateTime.Now + System.TimeSpan.FromSeconds(5);
                    Debug.Log($"SubReader {subName}: sub.play({url}) failed, will try again later");
                    return;
                }
                streamCount = subHandle.get_stream_count();
                Debug.Log($"SubReader {subName}: sub.play({url}) successful, {streamCount} streams.");
            }
        }

        protected void Cleaner() {
            if (gch.IsAllocated) gch.Free();
            currentBufferArray = null;
            currentBuffer = System.IntPtr.Zero;
            //info = new sub.FrameInfo { dsi = new byte[256], dsi_size = 256 };
        }

        protected override void Update() {
            base.Update();
            if (token != null) {  // Wait for token
                if (!isPlaying) retryPlay();
                else {
                    Cleaner();

                    // Try to read from audio.
                    if (streamCount > 1 && needsAudio != null && needsAudio()) {
                        // Attempt to receive, if we are playing
                        int bytesNeeded = subHandle.grab_frame(1 - streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                                                                                                                   // If we are not playing or if we didn't receive anything we restart after 1000 failures.
                                                                                                                   //UnsuccessfulCheck(bytesNeeded);
                        if (bytesNeeded != 0) {
                            if (currentBufferArray == null || bytesNeeded > currentBufferArray.Length) {
                                currentBufferArray = new byte[(int)bytesNeeded];
                                gch = System.Runtime.InteropServices.GCHandle.Alloc(currentBufferArray, System.Runtime.InteropServices.GCHandleType.Pinned);
                                currentBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(currentBufferArray, 0);
                            }
                            int bytesRead = subHandle.grab_frame(1 - streamNumber, currentBuffer, bytesNeeded, ref info);
                            if (bytesRead == bytesNeeded) {
                                lock (token) {
                                    // All ok, yield to the next process
                                    token.currentBuffer = currentBuffer;
                                    token.currentByteArray = currentBufferArray;
                                    token.currentSize = bytesRead;
                                    token.info = info;
                                    token.isVideo = false;
                                }
                                Next();
                                return;
                            }
                            else
                                Debug.LogError("PCSUBReader {subName}: sub_grab_frame returned " + bytesRead + " bytes after promising " + bytesNeeded);
                        }
                    }
                    if (needsVideo == null || needsVideo()) {
                        // Attempt to receive, if we are playing
                        int bytesNeeded = subHandle.grab_frame(streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                                                                                                               // If we are not playing or if we didn't receive anything we restart after 1000 failures.
                        UnsuccessfulCheck(bytesNeeded);
                        if (bytesNeeded != 0) {
                            if (currentBufferArray == null || bytesNeeded > currentBufferArray.Length) {
                                currentBufferArray = new byte[(int)bytesNeeded];
                                gch = System.Runtime.InteropServices.GCHandle.Alloc(currentBufferArray, System.Runtime.InteropServices.GCHandleType.Pinned);
                                currentBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(currentBufferArray, 0);
                            }
                            int bytesRead = subHandle.grab_frame(streamNumber, currentBuffer, bytesNeeded, ref info);
                            if (bytesRead == bytesNeeded) {
                                // All ok, yield to the next process
                                lock (token) {
                                    token.currentBuffer = currentBuffer;
                                    token.currentByteArray = currentBufferArray;
                                    token.currentSize = bytesRead;
                                    token.info = info;
                                    token.isVideo = true;
                                }
                                Next();
                                return;
                            }
                            else
                                Debug.LogError("PCSUBReader {subName}: sub_grab_frame returned " + bytesRead + " bytes after promising " + bytesNeeded);
                        }
                    }
                }
            }
        }
    }
}

