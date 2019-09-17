using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoDashReceiver : MonoBehaviour
{
    new public Renderer renderer;

    Workers.BaseWorker reader;
    Workers.VideoDecoder codec;
    Workers.BaseWorker preparer;
    public string url;

    public Texture2D texture;

    private void Start() {
        Init(url);
    }

    // Start is called before the first frame update
    public void Init(string url) {
        Debug.Log($"Config.Instance.memoryDamping {Config.Instance.memoryDamping}");
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
}
