using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Runtime.InteropServices;
using System;
using System.Threading;
using UnityEngine.Networking.NetworkSystem;

public class VideoWebCam : MonoBehaviour {
    public Renderer rendererOrg;
    public Renderer rendererDst;

    Workers.WebCamReader    recorder;
    Workers.VideoEncoder    encoder;
    Workers.BaseWorker      writer;
    Workers.BaseWorker      reader;

    Workers.VideoDecoder    decoder;
    Workers.VideoPreparer   preparer;

    QueueThreadSafe         videoDataQueue = new QueueThreadSafe();
    //    QueueThreadSafe         audioDataQueue = new QueueThreadSafe();
    QueueThreadSafe         writerQueue = new QueueThreadSafe();
    QueueThreadSafe         videoCodecQueue = new QueueThreadSafe();
    //    QueueThreadSafe         audioCodecQueue = new QueueThreadSafe();
    QueueThreadSafe videoPreparerQueue = new QueueThreadSafe(5);
//    QueueThreadSafe         audioPreparerQueue = new QueueThreadSafe(10);

    Texture2D       texture;
    public int      width = 1280;
    public int      height = 720;
    public int      fps = 12;
    bool            ready = false;

    public bool     useSocketIO = true;

    private IEnumerator Start() {
        ready = false;
        while (OrchestratorController.Instance==null || OrchestratorController.Instance.MySession==null) yield return null;

        Init();

        rendererOrg.material.mainTexture = recorder.webcamTexture;
        rendererOrg.transform.localScale = new Vector3(1, 1, recorder.webcamTexture.height / (float)recorder.webcamTexture.width);
    }

    // Start is called before the first frame update
    public void Init() {
        string uuid = System.Guid.NewGuid().ToString();
        string remoteURL = "https://vrt-evanescent.viaccess-orca.com/" + uuid + "/wcss/";
        string remoteStream = "webcam";
        try {
            recorder = new Workers.WebCamReader(width, height, fps, this, videoDataQueue);
            encoder  = new Workers.VideoEncoder(videoDataQueue, null/*audioDataQueue*/, writerQueue, null/*audioCodecQueue*/);
            Workers.B2DWriter.DashStreamDescription[] b2dStreams = new Workers.B2DWriter.DashStreamDescription[1] {
                new Workers.B2DWriter.DashStreamDescription() {
                    tileNumber = 0,
                    quality = 0,
                    inQueue = writerQueue
                }
            };
            if(useSocketIO) writer = new Workers.SocketIOWriter(remoteURL, remoteStream, b2dStreams);
            else            writer = new Workers.B2DWriter(remoteURL, remoteStream, "wcss", 2000, 10000, b2dStreams);

            Workers.PCSubReader.TileDescriptor[] tiles = new Workers.PCSubReader.TileDescriptor[1] {
                new Workers.PCSubReader.TileDescriptor() {
                        outQueue = videoCodecQueue,
                        tileNumber = 0
                    }
            };
            if (useSocketIO) reader = new Workers.SocketIOReader(remoteURL, remoteStream, tiles);
            else             reader = new Workers.PCSubReader(remoteURL, remoteStream, 1, tiles);

            decoder = new Workers.VideoDecoder(videoCodecQueue, null/*audioCodecQueue*/, videoPreparerQueue, null/*audioPreparerQueue*/);
            preparer = new Workers.VideoPreparer(videoPreparerQueue, null/*audioPreparerQueue*/);
        }
        catch (System.Exception e) {
            Debug.LogError($"VideoWebCam.Init: Exception: {e.Message}\n{e.StackTrace}");
            throw e;
        }
        ready = true;
    }
    float timeToFrame = 0;
    void Update() {
        if (ready) {
            lock (preparer) {
                if (preparer.availableVideo > 0) {
                    if (texture == null) {
                        texture = new Texture2D(decoder.Width, decoder.Height, TextureFormat.RGB24, false, true);
                        rendererDst.material.mainTexture = texture;
                        rendererDst.transform.localScale = new Vector3(1, 1, decoder.Height / (float)decoder.Width);
                    }
                    texture.LoadRawTextureData(preparer.GetVideoPointer(preparer.videFrameSize), preparer.videFrameSize);
                    texture.Apply();
                }
            }
        }
    }

    void OnDestroy() {
        Debug.Log("VideoDashReceiver: OnDestroy");
        encoder?.StopAndWait();
        recorder?.StopAndWait();
        decoder?.StopAndWait();
        preparer?.StopAndWait();

//        Debug.Log($"VideoDashReceiver: Queues references counting: videoCodecQueue {videoCodecQueue._Count} audioCodecQueue {audioCodecQueue._Count} videoPreparerQueue {videoPreparerQueue._Count} audioPreparerQueue {audioPreparerQueue._Count}");
        Debug.Log($"VideoDashReceiver: Queues references counting: videoCodecQueue {videoCodecQueue._Count} videoPreparerQueue {videoPreparerQueue._Count} ");
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
