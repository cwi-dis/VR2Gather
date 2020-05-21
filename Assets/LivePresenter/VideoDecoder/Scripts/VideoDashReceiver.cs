using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoDashReceiver : MonoBehaviour {
    new public Renderer renderer;

    Workers.BaseWorker      reader;
    Workers.VideoDecoder    codec;
    Workers.VideoPreparer   preparer;

    QueueThreadSafe         videoCodecQueue = new QueueThreadSafe();
    QueueThreadSafe         audioCodecQueue = new QueueThreadSafe();
    QueueThreadSafe         videoPreparerQueue = new QueueThreadSafe(5);
    QueueThreadSafe         audioPreparerQueue = new QueueThreadSafe(10);

    Workers.Token token;
    public string url = ""; //"https://www.gpac-licensing.com/downloads/VRTogether/vod/dashcastx.mpd";
    public string streamName = ""; //"https://www.gpac-licensing.com/downloads/VRTogether/vod/dashcastx.mpd";

    public Texture2D texture;
    AudioSource audioSource;

    private void Start() {
        var pp = Config.Instance;
        Init();
        audioSource = gameObject.GetComponent<AudioSource>();
        if(audioSource==null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.Stop();
    }

    // Start is called before the first frame update
    public void Init() {
        try {
            codec = new Workers.VideoDecoder(videoCodecQueue, audioCodecQueue, videoPreparerQueue, audioPreparerQueue);
            preparer = new Workers.VideoPreparer(videoPreparerQueue, audioPreparerQueue);
            reader = new Workers.AVSubReader(url, streamName, videoCodecQueue, audioCodecQueue);
        }
        catch (System.Exception e) {
            Debug.LogError($"VideoDashReceiver.Init: Exception: {e.Message}\n{e.StackTrace}");
            throw e;
        }
    }

    bool firstFrame = true;
    float timeToWait = 0;
    float currentTime = 0;
    float lastFrame = 0;

    void Update() {
        lock (preparer) {
            if (preparer.availableVideo > 0) {
                if (timeToWait < 0) {
                    if (texture == null) {
                        texture = new Texture2D(codec.Width, codec.Height, TextureFormat.RGB24, false, true);
                        renderer.material.mainTexture = texture;
                        renderer.transform.localScale = new Vector3(1, -1, codec.Height / (float)codec.Width);
                    }

                    if (firstFrame) {
                        firstFrame = false;
                        audioSource.Play();
                        currentTime = timeToWait = 0;
                    }
                    lastFrame = Time.realtimeSinceStartup;
                    timeToWait += 1 / 30f;
                    currentTime += 1 / 30f;
                    texture.LoadRawTextureData(preparer.GetVideoPointer(codec.videoDataSize), codec.videoDataSize);
                    texture.Apply();
                }
            }
            timeToWait -= Time.deltaTime;
        }
    }

    void OnDestroy() {
        Debug.Log("VideoDashReceiver: OnDestroy");
        reader?.StopAndWait();
        codec?.StopAndWait();
        preparer?.StopAndWait();

        Debug.Log($"VideoDashReceiver: Queues references counting: videoCodecQueue {videoCodecQueue.Count} audioCodecQueue {audioCodecQueue.Count} videoPreparerQueue {videoPreparerQueue.Count} audioPreparerQueue {audioPreparerQueue.Count}");
        BaseMemoryChunkReferences.ShowTotalRefCount();
    }

    void OnAudioRead(float[] data) {
        if (preparer == null || !preparer.GetAudioBuffer(data, data.Length))
            System.Array.Clear(data, 0, data.Length);
    }

    void OnAudioFilterRead(float[] data, int channels) {
        preparer?.GetAudioBuffer(data, data.Length);
    }
}
