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
        public SUBReader(Config._User._SUBConfig cfg, bool dropInitalData=false) :base(WorkerType.Init) { 
            url = cfg.url;
            streamNumber = cfg.streamNumber;
            firstTime = dropInitalData;
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
            if (subHandle != System.IntPtr.Zero) signals_unity_bridge_pinvoke.sub_destroy(subHandle);
            base.OnStop();
            Debug.Log("SUBReader Sopped");

        }
        float latTime = 0;
        bool firstTime = false;
        protected override void Update() {
            base.Update();
            if (token != null) {  // Wait for token
                int size = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                if (firstTime && size!=0) {
                    byte[] tmpArray = new byte[size];
                    System.IntPtr tmpBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tmpArray, 0);
                    do {
                        signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, streamNumber, tmpBuffer, size, ref info);
                        size = signals_unity_bridge_pinvoke.sub_grab_frame(subHandle, streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                    } while (size != 0);
                    firstTime = false;
                }

                if (size != 0) {
                    if (!firstTime) 
                    {
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
                            Next();
                        } else
                            Debug.LogError("PCSUBReader: sub_grab_frame returned " + bytesRead + " bytes after promising " + size);
                    }
                } 
            }
        }
    }
}

