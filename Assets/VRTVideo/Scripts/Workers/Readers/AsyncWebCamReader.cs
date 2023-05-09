using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using VRT.Core;
using Cwipc;

namespace VRT.Video
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using QueueThreadSafe = Cwipc.QueueThreadSafe;

    public class AsyncWebCamReader : AsyncWorker
    {
        MonoBehaviour monoBehaviour;
        Coroutine coroutine;
        QueueThreadSafe outQueue;
        VideoFilter RGBA2RGBFilter;

        SemaphoreSlim frameReady;
        bool isFrameReady = false;
        CancellationTokenSource isClosed;

        IntPtr data;

        public int width;
        public int height;
        public int fps;


        public AsyncWebCamReader(string deviceName, int width, int height, int fps, MonoBehaviour monoBehaviour, QueueThreadSafe _outQueue) : base()
        {

            if (VRTConfig.Instance.ffmpegDLLDir != "")
            {
                FFmpeg.AutoGen.ffmpeg.RootPath = VRTConfig.Instance.ffmpegDLLDir;
            }
            if (string.IsNullOrEmpty(deviceName) || deviceName == "None") return;

            this.width = width;
            this.height = height;
            this.fps = fps;

            outQueue = _outQueue;
            this.monoBehaviour = monoBehaviour;
            frameReady = new SemaphoreSlim(0);
            isClosed = new CancellationTokenSource();

            Init(deviceName);

            coroutine = monoBehaviour.StartCoroutine(WebCamRecorder(deviceName));
            Start();
        }

        protected override void AsyncUpdate()
        {
            if (outQueue.IsClosed()) return;
            try
            {
                lock (this)
                {
                    //frameReady.Wait(isClosed.Token);
                    //if (!isClosed.IsCancellationRequested)
                    if (isFrameReady)
                        Color32ArrayToByteArray(webcamColors, outQueue);
                }
            }
            catch (OperationCanceledException)
            {
                //                frameReady.Release();
            }
        }

        public override void Stop()
        {
            base.Stop();
            webcamTexture?.Stop();
            outQueue.Close();
            isClosed.Cancel();
            monoBehaviour.StopCoroutine(coroutine);
        }

        void Color32ArrayToByteArray(Color32[] colors, QueueThreadSafe outQueue)
        {
            GCHandle handle = default;
            try
            {
                handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
                NativeMemoryChunk chunk = RGBA2RGBFilter.Process(handle.AddrOfPinnedObject());
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                Timestamp now = (Timestamp)sinceEpoch.TotalMilliseconds;
                chunk.metadata = new FrameMetadata()
                {
                    timestamp = now,
                    dsi = infoData,
                    dsi_size = 12
                };
                outQueue.Enqueue(chunk);
                isFrameReady = false;
            }
            finally
            {
                if (handle != default)
                    handle.Free();
            }
        }

        float timeToFrame;
        float frameTime;
        Color32[] webcamColors;
        public WebCamTexture webcamTexture { get; private set; }
        byte[] infoData;

        void Init(string deviceName)
        {
#if WEBCAM_DEBUG_PRINT_RESOLUTIONS
            WebCamDevice[] devices = WebCamTexture.devices;
            for (int i = 0; i < devices.Length; ++i) {
                var dev = devices[i];
                UnityEngine.Debug.Log($"[FPA] {i} devices {dev.name} availableResolutions {dev.availableResolutions}");
                if (dev.availableResolutions != null) {
                    for (int j = 0; j < dev.availableResolutions.Length; ++j) {
                        UnityEngine.Debug.Log($"Res {dev.availableResolutions[j].width} {dev.availableResolutions[j].height} refreshRate {dev.availableResolutions[j].refreshRate}");
                    }
                }
            }
#endif
            webcamTexture = new WebCamTexture(deviceName, width, height, fps);
            webcamTexture.Play();
#if WEBCAM_AUTO_RESOLUTION
            width = webcamTexture.width;
            height = webcamTexture.height;

            if (webcamTexture.isPlaying)
            {
                webcamColors = webcamTexture.GetPixels32(webcamColors);
                UnityEngine.Debug.Log($"[FPA] Webcam initialized, got {webcamColors.Length} pixel buffer");
            }
            else
            {
                webcamColors = new Color32[width * height];
                UnityEngine.Debug.LogWarning($"[FPA] Webcam not initialized, allocated {webcamColors.Length} pixel buffer");
            }
#endif

            RGBA2RGBFilter = new VideoFilter(width, height, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_RGBA, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_RGB24);

            frameTime = 1f / fps;
            timeToFrame = Time.realtimeSinceStartup + frameTime;

            infoData = new byte[4 * 3];
            BitConverter.GetBytes(width).CopyTo(infoData, 0);
            BitConverter.GetBytes(height).CopyTo(infoData, 4);
            BitConverter.GetBytes(fps).CopyTo(infoData, 8);
        }

        IEnumerator WebCamRecorder(string deviceName)
        {
            while (true)
            {
                lock (this)
                {
                    if (!isFrameReady && timeToFrame < Time.realtimeSinceStartup)
                    {//&& frameReady.CurrentCount == 0) {
                        if (webcamTexture.isPlaying)
                        {
                            webcamColors = webcamTexture.GetPixels32(webcamColors);
                            if (webcamColors.Length < width*height)
                            {
                                UnityEngine.Debug.Log($"xxxjack WebCamReader: drop short videoframe of length {webcamColors.Length}");
                                webcamColors = null;
                                yield return null;
                            }
                            isFrameReady = true;
                        }
                        //                        frameReady.Release();
                        timeToFrame = frameTime + Time.realtimeSinceStartup;
                    }
                }
                yield return null;
            }
        }
    }
}