using UnityEngine;

namespace Workers
{
    public class SUBReader : BaseWorker
    {
        string url;
        int streamNumber;
        int streamCount;
        System.IntPtr subHandle;
        byte[] currentBufferArray;
        System.IntPtr currentBuffer;
        int dampedSize = 0;

        signals_unity_bridge_pinvoke.FrameInfo info = new signals_unity_bridge_pinvoke.FrameInfo();
        public SUBReader(Config._User._SUBConfig cfg) : base(WorkerType.Init) {
            url = cfg.url;
            streamNumber = cfg.streamNumber;
            try {
                signals_unity_bridge_pinvoke.SetPaths();
                subHandle = signals_unity_bridge_pinvoke.sub_create("source_from_sub");
                if (subHandle != System.IntPtr.Zero) {
                    if (signals_unity_bridge_pinvoke.sub_play(subHandle, url)) {
                        streamCount = signals_unity_bridge_pinvoke.sub_get_stream_count(subHandle);
                        Debug.Log($"streamCount {streamCount}");
                        Start();
                    }
                    else
                        throw new System.Exception($"PCSUBReader: sub_play failed for {url}");
                }
                else
                    throw new System.Exception($"PCSUBReader: sub_create failed");
            }
            catch (System.Exception e) {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public SUBReader(string cfg) : base(WorkerType.Init) {
            url = cfg;
            streamNumber = 1;
            try {
                signals_unity_bridge_pinvoke.SetPaths();
                subHandle = signals_unity_bridge_pinvoke.sub_create("source_from_sub");
                if (subHandle != System.IntPtr.Zero) {
                    if (signals_unity_bridge_pinvoke.sub_play(subHandle, url)) {
                        streamCount = signals_unity_bridge_pinvoke.sub_get_stream_count(subHandle);
                        Debug.Log($"streamCount {streamCount}");
                        Start();
                    }
                    else
                        throw new System.Exception($"PCSUBReader: sub_play failed for {url}");
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
            if (subHandle != System.IntPtr.Zero) signals_unity_bridge_pinvoke.sub_destroy(subHandle);
            base.OnStop();
            Debug.Log("SUBReader Sopped");

        }
        float latTime = 0;

        protected override void Update() {
            base.Update();
            if (token != null) {  // Wait for token
                info.dsi_size = 256;
                var sizeS = System.Runtime.InteropServices.Marshal.SizeOf(typeof(signals_unity_bridge_pinvoke.FrameInfo));

              //  Debug.Log($"Read from {streamNumber}");
                int size = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                if (size != 0) {
                    Debug.Log($"PCSUBReader: {streamNumber}!!!!");
                    if (size > dampedSize) {
                        dampedSize = (int)(size * Config.Instance.memoryDamping); // Reserves 30% more.
                        currentBufferArray = new byte[dampedSize];
                        currentBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(currentBufferArray, 0);
                    }

                    int bytesRead = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, streamNumber, currentBuffer, size, ref info);
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
                if (streamCount > 0) {
                    size = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, 1-streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                    if (size != 0) {
                        Debug.Log($"PCSUBReader: {1 - streamNumber}!!!!");
                        if (size > dampedSize) {
                            dampedSize = (int)(size * Config.Instance.memoryDamping); // Reserves 30% more.
                            currentBufferArray = new byte[dampedSize];
                            currentBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(currentBufferArray, 0);
                        }
                        int bytesRead = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, 1 - streamNumber, currentBuffer, size, ref info);


                    }
                }
            }
        }
    }
}

