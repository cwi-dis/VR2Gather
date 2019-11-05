using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoDashReceiver : MonoBehaviour
{
    new public Renderer renderer;

    Workers.BaseWorker reader;
    Workers.VideoDecoder codec;
    Workers.VideoPreparer preparer;
    Workers.Token token;
    public string url = ""; //"https://www.gpac-licensing.com/downloads/VRTogether/vod/dashcastx.mpd";

    public Texture2D texture;
    AudioSource audioSource;

    private void Start() {
        var pp =Config.Instance;
        Init(url);
        audioSource = gameObject.GetComponent<AudioSource>();
        if(audioSource==null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.Stop();
        //audioSource.Play();
    }

    // Start is called before the first frame update
    public void Init(string url) {
        int framesBuffered = 10;
        try {
            reader = new Workers.SUBReader(url, () => {
                bool val = false;
                lock (preparer) {
                    val = (preparer != null && preparer.availableVideo < codec.videoDataSize * framesBuffered) || codec.videoDataSize==0;
                }
                return val;
            }, ()=> {
                bool val = false;
                lock (preparer) {
                    val = preparer != null && preparer.availableAudio < ( 48000 / 30)* framesBuffered*4;
                }
                return val;
            } );
            codec = new Workers.VideoDecoder();
            preparer = new Workers.VideoPreparer();
            reader.AddNext(codec).AddNext(preparer).AddNext(reader);
            reader.token = token =  new Workers.Token();
        }
        catch (System.Exception e) {
            Debug.Log($">>ERROR {e}");
        }
    }


    bool firstFrame = true;
    float timeToWait = 0;
    float currentTime = 0;
    float lastFrame = 0;

    string log = "";

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
        reader?.Stop();
        codec?.Stop();
        preparer?.Stop();
        //System.IO.File.WriteAllText("c:/tmp/log.txt", log);
    }

    void OnAudioRead(float[] data) {
        if (preparer == null || !preparer.GetAudioBuffer(data, data.Length))
            System.Array.Clear(data, 0, data.Length);
    }

    void OnAudioFilterRead(float[] data, int channels) {
        preparer?.GetAudioBuffer(data, data.Length);
    }
}
