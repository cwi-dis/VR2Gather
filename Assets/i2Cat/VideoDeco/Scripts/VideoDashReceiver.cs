using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoDashReceiver : MonoBehaviour
{
    new public Renderer renderer;

    Workers.BaseWorker reader;
    Workers.VideoDecoder codec;
    Workers.BaseWorker preparer;
    public string url = "https://www.gpac-licensing.com/downloads/VRTogether/vod/dashcastx.mpd";

    public Texture2D texture;
    AudioSource audioSource;

    private void Start() {
        var pp =Config.Instance;
        Init(url);
        audioSource = gameObject.GetComponent<AudioSource>();
        if(audioSource==null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.Play();
    }

    // Start is called before the first frame update
    public void Init(string url) {
        try {
            reader = new Workers.SUBReader(url);
            codec = new Workers.VideoDecoder();
            preparer = new Workers.VideoPreparer();
            reader.AddNext(codec).AddNext(preparer).AddNext(reader);
            reader.token = new Workers.Token();
        }
        catch (System.Exception e) {
            Debug.Log($">>ERROR {e}");
        }
    }
    bool buffering = true;
    void Update() {
        if (codec.videoIsReady) {
            if (texture == null) {
                texture = new Texture2D(codec.Width, codec.Height, TextureFormat.RGB24, false, true);
                renderer.material.mainTexture = texture;
            }
            texture.LoadRawTextureData(codec.videoData, codec.videoDataSize);
            texture.Apply();

            codec.videoIsReady = false;
        }
    }

    void OnDestroy() {
        reader?.Stop();
        codec?.Stop();
        preparer?.Stop();
    }

    void OnAudioRead(float[] data) {
        if (preparer == null || !preparer.GetBuffer(data, data.Length))
            System.Array.Clear(data, 0, data.Length);
    }

    void OnAudioFilterRead(float[] data, int channels) {
        preparer?.GetBuffer(data, data.Length);
    }
}
