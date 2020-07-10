using System.Collections;
using System.Collections.Generic;
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



        public WebCamReader(MonoBehaviour monoBehaviour, QueueThreadSafe _outQueue) : base(WorkerType.Init) {
            outQueue = _outQueue;
            this.monoBehaviour = monoBehaviour;
            frameReady = new SemaphoreSlim(0);
            isClosed = new CancellationTokenSource();

            Init();

            coroutine = monoBehaviour.StartCoroutine(WebCamRecorder());
            Start();
        }

        protected override void Update() {
            base.Update();
            frameReady.Wait(isClosed.Token);
            if (!isClosed.IsCancellationRequested) {
                Color32ArrayToByteArray(webcamColors, outQueue);
            }
        }

        public override void OnStop() {
            base.OnStop();
            isClosed.Cancel();
            monoBehaviour.StopCoroutine(coroutine);
            Debug.Log($"{Name()}: Stopped webcam.");
            outQueue.Close();
        }


        float                   timeToFrame;
        float                   frameTime;
        Color32[]               webcamColors;
        public WebCamTexture    webcamTexture { get; private set; }

        void Init() {
            WebCamDevice[] devices = WebCamTexture.devices;
            for (int i = 0; i < devices.Length; ++i) {
                var dev = devices[i];
                Debug.Log($"{i} devices {dev.name} availableResolutions {dev.availableResolutions}");
                if (dev.availableResolutions != null) {
                    for (int j = 0; j < dev.availableResolutions.Length; ++j) {
                        Debug.Log($"Res {dev.availableResolutions[j].width} {dev.availableResolutions[j].height} refreshRate {dev.availableResolutions[j].refreshRate}");
                    }
                }
            }

            webcamTexture = new WebCamTexture(1280, 720, 12);
            webcamTexture.Play();
            webcamColors = webcamTexture.GetPixels32(webcamColors);

            RGBA2RGBFilter = new VideoFilter(webcamTexture.width, webcamTexture.height, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_RGBA, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_RGB24);

            frameTime = 1f / 12f;
            timeToFrame = Time.realtimeSinceStartup + frameTime;


        }

        IEnumerator WebCamRecorder() {
            while (true) {
                lock (this) {
                    if (timeToFrame < Time.realtimeSinceStartup && frameReady.CurrentCount == 0) {
                        
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
                lock (outQueue) {
                    outQueue.Enqueue(chunk);
                }
            } finally {
                if (handle != default(GCHandle))
                    handle.Free();
            }
        }


    }
}