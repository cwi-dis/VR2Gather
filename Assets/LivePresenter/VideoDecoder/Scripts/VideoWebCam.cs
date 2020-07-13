using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Runtime.InteropServices;
using System;
using System.Threading;

public class VideoWebCam : MonoBehaviour {
    public Renderer rendererOrg;
    public Renderer rendererDst;

    Workers.WebCamReader    reader;
    Workers.VideoEncoder    encoder;
    Workers.VideoDecoder    decoder;
    Workers.VideoPreparer   preparer;

    QueueThreadSafe         videoDataQueue = new QueueThreadSafe();
//    QueueThreadSafe         audioDataQueue = new QueueThreadSafe();
    QueueThreadSafe         videoCodecQueue = new QueueThreadSafe();
//    QueueThreadSafe         audioCodecQueue = new QueueThreadSafe();
    QueueThreadSafe         videoPreparerQueue = new QueueThreadSafe(5);
//    QueueThreadSafe         audioPreparerQueue = new QueueThreadSafe(10);

    Texture2D       texture;
    public int      width = 1280;
    public int      height = 720;
    public int      fps = 12;

    private void Start() {
        Init();

        rendererOrg.material.mainTexture = reader.webcamTexture;
        rendererOrg.transform.localScale = new Vector3(1, 1, reader.webcamTexture.height / (float)reader.webcamTexture.width);
    }

    // Start is called before the first frame update
    public void Init() {
        try {
            reader      = new Workers.WebCamReader(width, height, fps, this, videoDataQueue);
            encoder     = new Workers.VideoEncoder(videoDataQueue, null/*audioDataQueue*/, videoCodecQueue, null/*audioCodecQueue*/);
            decoder     = new Workers.VideoDecoder(videoCodecQueue, null/*audioCodecQueue*/, videoPreparerQueue, null/*audioPreparerQueue*/);
            preparer    = new Workers.VideoPreparer(videoPreparerQueue, null/*audioPreparerQueue*/);
        }
        catch (System.Exception e) {
            Debug.LogError($"VideoDashReceiver.Init: Exception: {e.Message}\n{e.StackTrace}");
            throw e;
        }
    }

    float timeToFrame = 0;
    void Update() {
        lock (preparer) {
            if (preparer.availableVideo > 0) {
                if (texture == null) {
                    texture = new Texture2D(width, height, TextureFormat.RGB24, false, true);
                    rendererDst.material.mainTexture = texture;
                    rendererDst.transform.localScale = new Vector3(1, 1, texture.height / (float)texture.width);
                }
                texture.LoadRawTextureData(preparer.GetVideoPointer(preparer.videFrameSize), preparer.videFrameSize);
                texture.Apply();
            }
        }
    }

    void OnDestroy() {
        Debug.Log("VideoDashReceiver: OnDestroy");
        encoder?.StopAndWait();
        reader?.StopAndWait();
        decoder?.StopAndWait();
        preparer?.StopAndWait();

//        Debug.Log($"VideoDashReceiver: Queues references counting: videoCodecQueue {videoCodecQueue._Count} audioCodecQueue {audioCodecQueue._Count} videoPreparerQueue {videoPreparerQueue._Count} audioPreparerQueue {audioPreparerQueue._Count}");
        Debug.Log($"VideoDashReceiver: Queues references counting: videoCodecQueue {videoCodecQueue._Count} videoPreparerQueue {videoPreparerQueue._Count} ");
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }
}
