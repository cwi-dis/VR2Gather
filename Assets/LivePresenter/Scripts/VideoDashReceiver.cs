using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Video;
using VRT.Transport.Dash;
using VRT.Core;

namespace VRT.LivePresenter
{
    public class VideoDashReceiver : MonoBehaviour
    {
        new public Renderer renderer;

        BaseWorker reader;
        VideoDecoder decoder;
        VideoPreparer preparer;

        // xxxjack nothing is dropped here. Need to investigate what is the best idea.
        QueueThreadSafe videoDataQueue = new QueueThreadSafe("VideoDashReceiver");
        QueueThreadSafe audioDataQueue = new QueueThreadSafe("AudioDashReceiver");
        QueueThreadSafe videoCodecQueue = new QueueThreadSafe("VideoDashDecompressor");
        QueueThreadSafe audioCodecQueue = new QueueThreadSafe("AudioDashDecompressor");
        QueueThreadSafe videoPreparerQueue = new QueueThreadSafe("VideoDashPreparer", 5);
        QueueThreadSafe audioPreparerQueue = new QueueThreadSafe("AudioDashPreparer", 10);

        public string url = ""; //"https://www.gpac-licensing.com/downloads/VRTogether/vod/dashcastx.mpd";
        public string streamName = ""; //"https://www.gpac-licensing.com/downloads/VRTogether/vod/dashcastx.mpd";

        public Texture2D texture;
        WebCamTexture webcamTexture;
        AudioSource audioSource;
        Color32[] webcamColors;

        private void Start()
        {
            Init();

            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.Stop();
        }

        // Start is called before the first frame update
        public void Init(FFmpeg.AutoGen.AVCodecID codec = FFmpeg.AutoGen.AVCodecID.AV_CODEC_ID_H264)
        {
            try
            {
                if (Config.Instance.ffmpegDLLDir != "")
                {
                    FFmpeg.AutoGen.ffmpeg.RootPath = Config.Instance.ffmpegDLLDir;
                }
                decoder = new VideoDecoder(codec, videoCodecQueue, audioCodecQueue, videoPreparerQueue, audioPreparerQueue);
                preparer = new VideoPreparer(videoPreparerQueue, audioPreparerQueue);
                // xxxjack should set Synchronizer here
                reader = new AVSubReader(url, streamName, videoCodecQueue, audioCodecQueue);
            }
            catch (System.Exception e)
            {
                Debug.Log($"VideoDashReceiver.Init: Exception: {e.Message}");
                throw;
            }
        }

        bool firstFrame = true;
        float timeToWait = 0;
        float currentTime = 0;
        float lastFrame = 0;

        private void Update()
        {
            preparer.Synchronize();
        }
        void LateUpdate()
        {
            lock (preparer)
            {
                preparer.LatchFrame();
                if (preparer.availableVideo > 0)
                {
                    if (timeToWait < 0)
                    {
                        if (texture == null)
                        {
                            texture = new Texture2D(decoder.Width, decoder.Height, TextureFormat.RGB24, false, true);
                            renderer.material.mainTexture = texture;
                            renderer.transform.localScale = new Vector3(1, -1, decoder.Height / (float)decoder.Width);
                        }

                        if (firstFrame)
                        {
                            firstFrame = false;
                            audioSource.Play();
                            currentTime = timeToWait = 0;
                        }
                        lastFrame = Time.realtimeSinceStartup;
                        timeToWait += 1 / 30f;
                        currentTime += 1 / 30f;
                        texture.LoadRawTextureData(preparer.GetVideoPointer(decoder.videoDataSize), decoder.videoDataSize);
                        texture.Apply();
                    }
                }
                timeToWait -= Time.deltaTime;
            }
        }

        void OnDestroy()
        {
            Debug.Log("VideoDashReceiver: OnDestroy");
            reader?.StopAndWait();
            decoder?.StopAndWait();
            preparer?.StopAndWait();

            Debug.Log($"VideoDashReceiver: Queues references counting: videoCodecQueue {videoCodecQueue._Count} audioCodecQueue {audioCodecQueue._Count} videoPreparerQueue {videoPreparerQueue._Count} audioPreparerQueue {audioPreparerQueue._Count}");
            BaseMemoryChunkReferences.ShowTotalRefCount();
        }

        void OnAudioRead(float[] data)
        {
            if (preparer == null || !preparer.GetAudioBuffer(data, data.Length))
                System.Array.Clear(data, 0, data.Length);
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            preparer?.GetAudioBuffer(data, data.Length);
        }
    }
}