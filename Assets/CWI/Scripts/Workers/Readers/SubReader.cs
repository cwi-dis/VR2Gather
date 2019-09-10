using UnityEngine;

namespace Workers
{
    public class SUBReader : BaseWorker
    {
        string url;
        int streamNumber;
        int streamCount;
        uint fourccInfo;
        sub.connection subHandle;
        bool isPlaying;
        byte[] currentBufferArray;
        System.IntPtr currentBuffer;
        int dampedSize = 0;

        sub.FrameInfo info = new sub.FrameInfo();
        public SUBReader(Config._User._SUBConfig cfg, bool dropInitialData=false) : base(WorkerType.Init) {
            url = cfg.url;
            streamNumber = cfg.streamNumber;
            firstTime = dropInitialData;
            try {
//                signals_unity_bridge_pinvoke.SetPaths();
                subHandle = sub.create("source_from_sub");
                if (subHandle != null) {
                    isPlaying = subHandle.play(url);
                    if (!isPlaying) {
                        Debug.Log("SubReader: sub_play() failed, will try again later");
                    } else {
                        streamCount = subHandle.get_stream_count();
                        Debug.Log($"streamCount {streamCount}");

                    }
                    Start();
                    
                }
                else
                    throw new System.Exception($"PCSUBReader: sub_create failed");
            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public SUBReader(string cfg, int _streamNumber=0) : base(WorkerType.Init) {
            url = cfg;
            streamNumber = _streamNumber;
            try {
                //signals_unity_bridge_pinvoke.SetPaths();
                subHandle = sub.create("source_from_sub");
                if (subHandle != null) {
                    isPlaying = subHandle.play(url);

                    isPlaying = subHandle.play(url);
                    if (!isPlaying) {
                        Debug.Log("SubReader: sub_play() failed, will try again later");
                    } else {
                        streamCount = subHandle.get_stream_count();
                        fourccInfo = subHandle.get_stream_4cc(0);
                        Debug.Log($"{url} streamCount {streamCount} fourccInfo {fourccInfo}");
                    }
                    Start();
                }
                else
                    throw new System.Exception($"PCSUBReader: sub_create failed");
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
            Debug.Log("SUBReader Sopped");
        }



        float latTime = 0;
        bool firstTime = false;
        int numberOfUnsuccessfulReceives;

        protected void retryPlay() {
            if (isPlaying) return;
            if (subHandle == null) {
                subHandle = sub.create("source_from_sub");
                if (subHandle == null) {
                    Debug.LogWarning("SubReader: retry sub.create() call failed again.");
                    return;
                }
            }
            if (subHandle != null) {
                isPlaying = subHandle.play(url);
                if (isPlaying) {
                    Debug.Log("SubReader: retry sub.play() successful.");
                    streamCount = subHandle.get_stream_count();
                    Debug.Log($"streamCount {streamCount}");

                }

            }
        }

        int counter0 = 0;
        int counter1 = 0;
        long lastTimeStamp=0;
        protected override void Update() {
            base.Update();

            if (token != null) {  // Wait for token
                if (!isPlaying) retryPlay();
                info.dsi_size = 256;
                int size = subHandle.grab_frame(streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                if (size != 0) {
                    // Debug.Log($"{counter0++}  PCSUBReader({streamNumber}): {size}!!!!");
                    if (size > dampedSize) {
                        dampedSize = (int)(size * Config.Instance.memoryDamping); // Reserves 30% more.
                        currentBufferArray = new byte[dampedSize];
                        currentBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(currentBufferArray, 0);
                    }

                    int bytesRead = subHandle.grab_frame(streamNumber, currentBuffer, size, ref info);
                    if (bytesRead == size) {
                        if (lastTimeStamp != 0 ) {
                            Debug.Log($"{url} -> DIFF {info.timestamp - lastTimeStamp}");
                            if (lastTimeStamp > info.timestamp) Debug.Log($"ERROR lastTimeStamp {lastTimeStamp} current {info.timestamp}");
                        }
                        lastTimeStamp = info.timestamp;
                        // All ok, yield to the next process
                        token.currentBuffer = currentBuffer;
                        token.currentByteArray = currentBufferArray;
                        token.currentSize = bytesRead;
                        token.info = info;
                        Next();
                    } else
                        Debug.LogError("PCSUBReader: sub_grab_frame returned " + bytesRead + " bytes after promising " + size);
                }
                if (streamCount > 0) {
                    size = subHandle.grab_frame(1-streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                    if (size != 0) {
                        // Debug.Log($"{counter1++}  PCSUBReader({1 - streamNumber}): {size}!!!!");
                        if (size > dampedSize) {
                            dampedSize = (int)(size * Config.Instance.memoryDamping); // Reserves 30% more.
                            currentBufferArray = new byte[dampedSize];
                            currentBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(currentBufferArray, 0);
                        }
                        int bytesRead = subHandle.grab_frame(1 - streamNumber, currentBuffer, size, ref info);
                        if (bytesRead == size) {
                            // All ok, yield to the next process
                            token.currentBuffer = currentBuffer;
                            token.currentByteArray = currentBufferArray;
                            token.currentSize = bytesRead;
                            token.info = info;
                            Next();
                        } else
                            Debug.LogError("PCSUBReader: sub_grab_frame returned " + bytesRead + " bytes after promising " + size);
                    }
                }
            }
        }
    }
}

