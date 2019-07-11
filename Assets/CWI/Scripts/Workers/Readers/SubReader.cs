using UnityEngine;

namespace Workers
{
    public class SUBReader : BaseWorker
    {
        string url;
        int streamNumber;        
        System.IntPtr subHandle;
        byte[] currentBufferArray;
        System.IntPtr currentBuffer;
        int dampedSize = 0;

        signals_unity_bridge_pinvoke.FrameInfo info = new signals_unity_bridge_pinvoke.FrameInfo();
        public SUBReader(Config._PCs._SUBConfig cfg) :base(WorkerType.Init) { 
            url = cfg.url;
            streamNumber = cfg.streamNumber;
            try {
                signals_unity_bridge_pinvoke.SetPaths();
                subHandle = signals_unity_bridge_pinvoke.sub_create("source_from_sub");
                if (subHandle != System.IntPtr.Zero)
                {
                    if (signals_unity_bridge_pinvoke.sub_play(subHandle, url))
                    {
                        Start();
                    }
                    else
                        throw new System.Exception($"PCSUBReader: sub_play failed for {url}");
                }
                else
                    throw new System.Exception($"PCSUBReader: sub_create failed");
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
                throw e;
            }
        }

        public override void OnStop()
        {
            base.OnStop();
            if (subHandle != System.IntPtr.Zero) signals_unity_bridge_pinvoke.sub_destroy(subHandle);
        }


        protected override void Update() {
            base.Update();
            if (token != null) {  // Wait for token
                int size = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                if (size != 0) {
                    if (size > dampedSize) {
                        Debug.Log("DATA!!!");
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
                        Next();
                    }
                    else
                        Debug.LogError("PCSUBReader: sub_grab_frame returned " + bytesRead + " bytes after promising " + size);
                }
            }
        }
    }
}

