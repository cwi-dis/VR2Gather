using RabbitMQ.Client.Content;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace Workers { 
    public class WebCamReader : BaseWorker {
        MonoBehaviour   monoBehaviour;
        Coroutine       coroutine;
        QueueThreadSafe outQueue;
        VideoFilter     RGBA2RGBFilter;

        SemaphoreSlim       frameReady;
        CancellationTokenSource   isClosed;

        System.IntPtr data;
        Stopwatch stopWatch;

        public int width;
        public int height;
        public int fps;


        public WebCamReader(int width, int height, int fps, MonoBehaviour monoBehaviour, QueueThreadSafe _outQueue) : base(WorkerType.Init) {
            this.width = width;
            this.height = height;
            this.fps = fps;

            outQueue = _outQueue;
            this.monoBehaviour = monoBehaviour;
            frameReady = new SemaphoreSlim(0);
            isClosed = new CancellationTokenSource();

            Init();

            stopWatch = new Stopwatch();
            stopWatch.Start();

            coroutine = monoBehaviour.StartCoroutine(WebCamRecorder());
            Start();
        }

        protected override void Update() {
            base.Update();
            if (outQueue.IsClosed()) return;
            try {
                frameReady.Wait(isClosed.Token);
                if (!isClosed.IsCancellationRequested) {
                    Color32ArrayToByteArray(webcamColors, outQueue);
                } else {
                    UnityEngine.Debug.Log($"Stopped {stopWatch.ElapsedMilliseconds}");
                }
            } catch(OperationCanceledException e ){
//                frameReady.Release();
            }
        }

        public override void Stop() {
            base.Stop();
            webcamTexture.Stop();
            outQueue.Close();
            isClosed.Cancel();
            monoBehaviour.StopCoroutine(coroutine);
        }

        float                   timeToFrame;
        float                   frameTime;
        Color32[]               webcamColors;
        public WebCamTexture    webcamTexture { get; private set; }
        byte[] infoData;

        void Init() {
            WebCamDevice[] devices = WebCamTexture.devices;
            for (int i = 0; i < devices.Length; ++i) {
                var dev = devices[i];
                UnityEngine.Debug.Log($"{i} devices {dev.name} availableResolutions {dev.availableResolutions}");
                if (dev.availableResolutions != null) {
                    for (int j = 0; j < dev.availableResolutions.Length; ++j) {
                        UnityEngine.Debug.Log($"Res {dev.availableResolutions[j].width} {dev.availableResolutions[j].height} refreshRate {dev.availableResolutions[j].refreshRate}");
                    }
                }
            }

            webcamTexture = new WebCamTexture(width, height, fps);
            webcamTexture.Play();

            width = webcamTexture.width;
            height = webcamTexture.height;

            if (webcamTexture.isPlaying) webcamColors = webcamTexture.GetPixels32(webcamColors);
            else {
                webcamColors = new Color32[width * height];
                UnityEngine.Debug.LogWarning($"[FPA] Webcam not initialized");
            }

            RGBA2RGBFilter = new VideoFilter(width, height, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_RGBA, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_RGB24);

            frameTime = 1f / 12f;
            timeToFrame = Time.realtimeSinceStartup + frameTime;

            infoData = new byte[4 * 3];
            BitConverter.GetBytes(width).CopyTo(infoData, 0);
            BitConverter.GetBytes(height).CopyTo(infoData, 4);
            BitConverter.GetBytes(fps).CopyTo(infoData, 8);
        }

        IEnumerator WebCamRecorder() {
            while (true) {
//                lock (this) 
                {
                    if (timeToFrame < Time.realtimeSinceStartup && frameReady.CurrentCount == 0) {
                        if(webcamTexture.isPlaying)
                            webcamColors = webcamTexture.GetPixels32(webcamColors);
                        timeToFrame += frameTime;
                        frameReady.Release();
                    }
                }
                yield return null;
            }
        }

        void Color32ArrayToByteArray(Color32[] colors, QueueThreadSafe outQueue) {
            GCHandle handle = default;
            try {
                handle = GCHandle.Alloc(colors, GCHandleType.Pinned);
                NativeMemoryChunk chunk = RGBA2RGBFilter.Process(handle.AddrOfPinnedObject());
                chunk.info.dsi = infoData;
                chunk.info.dsi_size = 12;
                outQueue.Enqueue(chunk);
            } finally {
                if (handle != default(GCHandle))
                    handle.Free();
            }
        }


    }
}