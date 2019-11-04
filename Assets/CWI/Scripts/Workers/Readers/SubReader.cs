using UnityEngine;

namespace Workers
{
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
        int dampedSize = 0;

        sub.FrameInfo info = new sub.FrameInfo();
        public SUBReader(Config._User._SUBConfig cfg, bool dropInitialData = false) : base(WorkerType.Init) {
            url = cfg.url;
            streamNumber = cfg.streamNumber;
            firstTime = dropInitialData;
            try {
                //              signals_unity_bridge_pinvoke.SetPaths();
                subHandle = sub.create("source_from_sub");
                if (subHandle != null) {
                    Debug.Log("SubReader: sub.create() successful.");
                    isPlaying = subHandle.play(url);
                    if (!isPlaying) {
                        Debug.Log("SubReader: sub_play() failed, will try again later");
                    } else {
                        streamCount = subHandle.get_stream_count();
                        Debug.Log($"streamCount {streamCount}");
                    }
                    Start();
                } else
                    throw new System.Exception($"PCSUBReader: sub_create failed");
            } catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public SUBReader(string cfg, NeedsSomething needsVideo = null, NeedsSomething needsAudio =null) : base(WorkerType.Init) {
            this.needsVideo = needsVideo;
            this.needsAudio = needsAudio;
            url = cfg;
            streamNumber = 0;
            try {
                //signals_unity_bridge_pinvoke.SetPaths();
                subHandle = sub.create("source_from_sub");
                if (subHandle != null) {
                    isPlaying = subHandle.play(url);
                    if (!isPlaying) {
                        Debug.Log("SubReader: sub_play() failed, will try again later");
                    } else {
                        streamCount = Mathf.Min(2, subHandle.get_stream_count());
                        if ((CCCC)subHandle.get_stream_4cc(0) == CCCC.AVC1) videoStream = 0;
                        else videoStream = 1;
                        streamNumber = videoStream;
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
            Debug.Log("Retrying connection with SUB");
            if (isPlaying) return;
            if (subHandle == null) {
                subHandle = sub.create("source_from_sub");
                if (subHandle == null) {
                    Debug.LogWarning("SubReader: retry sub.create() call failed again.");
                    return;
                }
                else {
                    Debug.Log("SubReader: retry sub.create() successful.");
                }
            }
            if (subHandle != null) {
                isPlaying = subHandle.play(url);
                if (!isPlaying) {
                    Debug.Log("SubReader: retry sub_play() failed, will try again later");
                }
                else {
                    Debug.Log("SubReader: retry sub.play() successful.");
                    streamCount = subHandle.get_stream_count();
                    Debug.Log($"streamCount {streamCount}");
                }
            }
        }
        protected override void Update() {
            base.Update();
            if (token != null) {  // Wait for token
                if (!isPlaying) retryPlay();
                else  {
                    // Try to read fron audio.
                    if (streamCount > 0 && needsAudio!=null && needsAudio() ) {
                        int size = subHandle.grab_frame(1-streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                        if (size != 0) {
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
                                token.isVideo = false;
                                Next();
                                return;
                            } else
                                Debug.LogError("PCSUBReader: sub_grab_frame returned " + bytesRead + " bytes after promising " + size);
                        } 
                        // else Debug.Log($"No data at {1 - streamNumber}");
                    }

                    if (needsVideo == null || needsVideo() ) {
                        info.dsi_size = 256;
                        // Try to read from video.
                        int size = subHandle.grab_frame(streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                        if (size != 0) {
                            if (size > dampedSize) {
                                dampedSize = (int)(size * Config.Instance.memoryDamping); // Reserves 30% more.
                                currentBufferArray = new byte[dampedSize];
                                currentBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(currentBufferArray, 0);
                            }
                            int bytesRead = subHandle.grab_frame(streamNumber, currentBuffer, size, ref info);
                            if (bytesRead == size) {
                                // All ok, yield to the next process
                                token.currentBuffer = currentBuffer;
                                token.currentByteArray = currentBufferArray;
                                token.currentSize = bytesRead;
                                token.info = info;
                                token.isVideo = true;
                                Next();
                                return;
                            } else
                                Debug.LogError("PCSUBReader: sub_grab_frame returned " + bytesRead + " bytes after promising " + size);
                        }
                    }
                }
            }
        }
    }
}

