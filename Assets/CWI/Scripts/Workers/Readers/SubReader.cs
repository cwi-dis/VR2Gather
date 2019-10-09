using UnityEngine;

namespace Workers
{
    public class SUBReader : BaseWorker
    {

        public enum CCCC: int {
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
        int dampedSize = 0;
        System.Runtime.InteropServices.GCHandle gch;

        sub.FrameInfo info = new sub.FrameInfo();
        public SUBReader(Config._User._SUBConfig cfg, bool dropInitialData=false) : base(WorkerType.Init) { //ConfigFile Based SUB
            url = cfg.url;
            streamNumber = cfg.streamNumber;
            firstTime = dropInitialData;
            try {
                subHandle = sub.create("source_from_sub");
                if (subHandle != null) {
                    Debug.Log($"SubReader: sub.create({url}) successful.");
                    isPlaying = subHandle.play(url);
                    if (!isPlaying) {
                        Debug.Log($"SubReader: sub_play({url}) failed, will try again later");
                    } else {
                        streamCount = subHandle.get_stream_count();
                        Debug.Log($"streamCount {url} {streamCount}");
                    }
                    Start();                    
                }
                else
                    throw new System.Exception($"PCSUBReader: sub_create{url} failed");
            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public SUBReader(string cfg) : base(WorkerType.Init) { // Video SUB
            url = cfg;
            streamNumber = 0;
            try {
                //signals_unity_bridge_pinvoke.SetPaths();
                subHandle = sub.create("source_from_sub");
                if (subHandle != null) {
                    isPlaying = subHandle.play(url);
                    if (!isPlaying) {
                        Debug.Log($"SubReader: sub_play({url}) failed, will try again later");
                    } else {
                        streamCount = Mathf.Min(2, subHandle.get_stream_count());
                        if ((CCCC)subHandle.get_stream_4cc(0) == CCCC.AVC1) videoStream = 0;
                        else videoStream = 1;

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

        public SUBReader(Config._User._SUBConfig cfg, string _url, bool dropInitalData = false) : base(WorkerType.Init) { // Orchestrator Based SUB
            url = _url + cfg.streamName;
            streamNumber = cfg.streamNumber;
            firstTime = dropInitalData;
            try {
                subHandle = sub.create("source_from_sub");
                if (subHandle != null) {
                    Debug.Log("SubReader: sub.create() successful.");
                    isPlaying = subHandle.play(url);
                    if (!isPlaying) {
                        Debug.Log($"SubReader: sub_play({url}) failed, will try again later");
                    }
                    else {
                        streamCount = subHandle.get_stream_count();
                        Debug.Log($"streamCount {streamCount}");
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

        public override void OnStop()
        {
            subHandle = null;
            base.OnStop();
            Debug.Log($"SUBReader {url} Sopped");
        }
               
        float latTime = 0;
        bool firstTime = false;
        int numberOfUnsuccessfulReceives;

        protected void retryPlay() {
            if (isPlaying) return;
            Debug.Log($"Retrying connection with SUB {url}");
            if (subHandle == null) {
                subHandle = sub.create("source_from_sub");
                if (subHandle == null) {
                    Debug.LogWarning($"SubReader: retry sub.create({url}) call failed again.");
                    return;
                }
                else {
                    Debug.Log($"SubReader: retry sub.create({url}) successful.");
                }
            }
            if (subHandle != null) {
                isPlaying = subHandle.play(url);
                if (!isPlaying) {
                    Debug.LogWarning($"SubReader: retry sub_play({url}) failed, will try again later");
                }
                else {
                    Debug.LogWarning($"SubReader: retry sub.play({url}) successful.");
                    streamCount = subHandle.get_stream_count();
                    Debug.Log($"{url} streamCount {streamCount}");
                }
            }
        }

        protected void UnsuccessfulCheck(int _size) {
            if (_size == 0) {
                numberOfUnsuccessfulReceives++;
                if (numberOfUnsuccessfulReceives > 2000) {
                    Debug.LogWarning($"SubReader {url}: Too many receive errors. Closing SUB player, will reopen.");
                    subHandle = null;
                    isPlaying = false;
                    numberOfUnsuccessfulReceives = 0;
                }
                return;
            }
            numberOfUnsuccessfulReceives = 0;
        }

        protected void Cleaner() {
            if (gch.IsAllocated) gch.Free();
            dampedSize = 0;
            currentBufferArray = null;
            currentBuffer = System.IntPtr.Zero;
            info = new sub.FrameInfo {
                dsi = new byte[256],
                dsi_size = 256
            };
        }

        protected override void Update() {
            base.Update();
            if (token != null) {  // Wait for token
                // Start playing, if not already playing
                retryPlay();

                Cleaner();

                // Attempt to receive, if we are playing
                int bytesNeeded = subHandle.grab_frame(streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.

                // If we are not playing or if we didn't receive anything we restart after 1000 failures.
                UnsuccessfulCheck(bytesNeeded);                

                if (bytesNeeded != 0) {
                    if (currentBufferArray == null || bytesNeeded > currentBufferArray.Length) {
                        //dampedSize = (int)(bytesNeeded * Config.Instance.memoryDamping); // Reserves 30% more.
                        currentBufferArray = new byte[(int)bytesNeeded];
                        gch = System.Runtime.InteropServices.GCHandle.Alloc(currentBufferArray, System.Runtime.InteropServices.GCHandleType.Pinned);
                        currentBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(currentBufferArray, 0);
                    }

                    int bytesRead = subHandle.grab_frame(streamNumber, currentBuffer, bytesNeeded, ref info);
                    if (bytesRead == bytesNeeded) {
                        // All ok, yield to the next process
                        token.currentBuffer = currentBuffer;
                        token.currentByteArray = currentBufferArray;
                        token.currentSize = bytesRead;
                        token.info = info;
                        token.isVideo = streamNumber == videoStream;
                        Next();
                    }
                    else
                        Debug.LogError("PCSUBReader: sub_grab_frame returned " + bytesRead + " bytes after promising " + bytesNeeded);
                }
                else Debug.Log($"No data at {url} {streamNumber}");
                //if (streamCount > 0) {
                //    bytesNeeded = subHandle.grab_frame(1-streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                //    if (bytesNeeded != 0) {
                //        // Debug.Log($"{counter1++}  PCSUBReader({1 - streamNumber}): {bytesNeeded}!!!!");
                //        if (bytesNeeded > dampedSize) {
                //            dampedSize = (int)(bytesNeeded * Config.Instance.memoryDamping); // Reserves 30% more.
                //            currentBufferArray = new byte[dampedSize];
                //            currentBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(currentBufferArray, 0);
                //        }
                //        int bytesRead = subHandle.grab_frame(1 - streamNumber, currentBuffer, bytesNeeded, ref info);
                //        if (bytesRead == bytesNeeded) {
                //            // All ok, yield to the next process
                //            token.currentBuffer = currentBuffer;
                //            token.currentByteArray = currentBufferArray;
                //            token.currentSize = bytesRead;
                //            token.info = info;
                //            token.isVideo = 1 - streamNumber == videoStream;
                //            Next();
                //        } else
                //            Debug.LogError("PCSUBReader: sub_grab_frame returned " + bytesRead + " bytes after promising " + bytesNeeded);
                //    } 
                //    else Debug.Log($"No data at {1 - streamNumber}");
                //}
            }
        }
    }
}

