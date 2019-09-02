using UnityEngine;

namespace Workers
{
    public class SUBReader : BaseWorker
    {
        string url;     // The URL we want to play
        int streamNumber;        // The stream number in the Dash manifest we want to play
        sub.connection subHandle;    // Handle of the dash receiver
        bool isPlaying; // True as soon as sub_play() ever returned true
        byte[] currentBufferArray;
        System.IntPtr currentBuffer;
        int dampedSize = 0;

        sub.FrameInfo info = new sub.FrameInfo();
        public SUBReader(Config._User._SUBConfig cfg, bool dropInitalData=false) :base(WorkerType.Init) { 
            url = cfg.url;
            streamNumber = cfg.streamNumber;
            firstTime = dropInitalData;
            try {
                subHandle = sub.create("source_from_sub");
                if (subHandle != null)
                {
                    isPlaying = subHandle.play(url);
                    if (!isPlaying)
                    {
                        Debug.Log("SubReader: sub_play() failed, will try again later");
                    }
                    Start();
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

        public SUBReader(Config._User._SUBConfig cfg, string _url, bool dropInitalData = false) : base(WorkerType.Init) {
            url = _url + cfg.streamName;
            streamNumber = cfg.streamNumber;
            firstTime = dropInitalData;
            try {
                subHandle = sub.create("source_from_sub");
                if (subHandle != null) {
                    isPlaying = subHandle.play(url);
                    if (!isPlaying) {
                        Debug.Log("SubReader: sub_play() failed, will try again later");
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

        protected void retryPlay()
        {
            if (isPlaying) return;
            if (subHandle == null)
            {
                subHandle = sub.create("source_from_sub");
                if (subHandle == null)
                {
                    Debug.LogWarning("SubReader: retry sub.create() call failed again.");
                    return;
                }
            }
            if (subHandle != null)
            {
                isPlaying = subHandle.play(url);
                if (isPlaying)
                {
                    Debug.Log("SubReader: retry sub.play() successful.");
                }

            }
        }

        protected override void Update() {
            base.Update();
            if (token != null) {  // Wait for token
                // Start playing, if not already playing
                if (!isPlaying) retryPlay();
                // Attempt to receive, if we are playing
                int size = 0;
                if (isPlaying)
                {
                    size = subHandle.grab_frame(streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
                }
                // If we are not playing or if we didn't receive anything we restart after 1000 failures.
                if (size == 0)
                {
                    numberOfUnsuccessfulReceives++;
                    if (numberOfUnsuccessfulReceives > 1000)
                    {
                        Debug.Log("SubReader: Too many receive errors. Closing SUB player, will reopen.");
                        subHandle = null;
                        isPlaying = false;
                        numberOfUnsuccessfulReceives = 0;
                    }
                    return;
                }
                numberOfUnsuccessfulReceives = 0;

                if (firstTime && size!=0) {
                    byte[] tmpArray = new byte[size];
                    System.IntPtr tmpBuffer = System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(tmpArray, 0);
                    do {
                        subHandle.grab_frame(streamNumber, tmpBuffer, size, ref info);
                        size = subHandle.grab_frame(streamNumber, System.IntPtr.Zero, 0, ref info); // Get buffer length.
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

                        int bytesRead = subHandle.grab_frame(streamNumber, currentBuffer, size, ref info);
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

