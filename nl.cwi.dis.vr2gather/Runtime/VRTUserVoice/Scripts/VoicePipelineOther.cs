using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Transport.TCP;
using VRT.Orchestrator.Wrapping;
using VRT.Core;
using Cwipc;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.UserRepresentation.Voice
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
    using BaseMemoryChunk = Cwipc.BaseMemoryChunk;

    public class VoicePipelineOther : MonoBehaviour
    {
        AsyncReader reader;
        AsyncWorker codec;
        AsyncVoicePreparer preparer;
        public AudioSource audioSource;

        [Tooltip("Object responsible for synchronizing playout")]
        public ISynchronizer synchronizer = null;

        [Tooltip("True if audio should be spatialized")]
        public bool spatialize = false;

        [Tooltip("Introspection: audio level")]
        public float currentAudioLevel = 0;
        public int currentAudioLevelCount = 0;
        public float currentFrameAudioLevel = 0;

        // xxxjack nothing is dropped here. Need to investigate what is the best idea.
        QueueThreadSafe decoderQueue;
        QueueThreadSafe preparerQueue;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        // Start is called before the first frame update
        public void Init(bool isLocalPlayer, object _user, VRTConfig._User cfg, bool preview = false)
        //public void Init(User user, string _streamName, int _streamNumber)
        {
            User user = (User)_user;
            const string _streamName = "audio";
            const int _streamNumber = 0;
            if (preview)
            {
                Debug.LogError($"{Name()}: preview==true not supported");
            }
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
            if (synchronizer == null)
            {
                synchronizer = FindObjectOfType<VRTSynchronizer>();
            }
            VoiceDspController.PrepareDSP(VRTConfig.Instance.audioSampleRate, 0);
            if (audioSource == null)
            {
                audioSource = gameObject.GetComponent<AudioSource>();
            }
            if (audioSource != null)
            {
                Debug.LogWarning($"{Name()}: already have an AudioSource, overriding");
            }
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialize = spatialize;
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 4f;
            audioSource.maxDistance = 100f;
            audioSource.loop = true;
            audioSource.Play();

            string audioCodec = SessionConfig.Instance.voiceCodec;
            bool audioIsEncoded = audioCodec == "VR2A";
            SessionConfig.ProtocolType proto = SessionConfig.Instance.protocolType;

            preparerQueue = new QueueThreadSafe("VoiceReceiverPreparer", 200, false);
            QueueThreadSafe _readerOutputQueue = preparerQueue;
            if (audioIsEncoded)
            {
                decoderQueue = new QueueThreadSafe("VoiceReceiverDecoder", 10, true);
                codec = new AsyncVoiceDecoder(decoderQueue, preparerQueue);
                _readerOutputQueue = decoderQueue;
            }

            if (proto == SessionConfig.ProtocolType.Dash)
            {
                reader = new AsyncSubReader(user.sfuData.url_audio, _streamName, _streamNumber, audioCodec, _readerOutputQueue);
#if VRT_WITH_STATS
                Statistics.Output(Name(), $"proto=dash, url={user.sfuData.url_audio}, streamName={_streamName}, streamNumber={_streamNumber}, codec={audioCodec}");
#endif
            }
            else
            if (proto == SessionConfig.ProtocolType.TCP)
            {
                reader = new AsyncTCPReader(user.userData.userAudioUrl, audioCodec, _readerOutputQueue);
#if VRT_WITH_STATS
                Statistics.Output(Name(), $"proto=tcp, url={user.userData.userAudioUrl}, codec={audioCodec}");
#endif
            }
            else
            {
                reader = new AsyncSocketIOReader(user, _streamName, audioCodec, _readerOutputQueue);
#if VRT_WITH_STATS
                Statistics.Output(Name(), $"proto=socketio, user={user}, streamName={_streamName}, codec={audioCodec}");
#endif
            }


            preparer = new AsyncVoicePreparer(preparerQueue);
            string synchronizerName = "none";
            if (synchronizer != null && synchronizer.isEnabled())
            {
                preparer.SetSynchronizer(synchronizer);
                if (!VRTConfig.Instance.Voice.ignoreSynchronizer)
                {
                    synchronizerName = synchronizer.Name();
                }
            }
            string decoderName = "none";
            if (codec != null)
            {
                decoderName = codec.Name();
            }
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"encoded={audioIsEncoded}, reader={reader.Name()}, decoder={decoderName}, preparer={preparer.Name()}, synchronizer={synchronizerName}");
#endif
        }

        private void Update()
        {
            lock(this)
            {
                if (currentAudioLevelCount == 0) currentAudioLevelCount = 1;
                currentFrameAudioLevel = currentAudioLevel / currentAudioLevelCount;
                currentAudioLevel = 0;
                currentAudioLevelCount = 0;


                float[] spectrum = new float[256];

                audioSource.GetSpectrumData(spectrum, 0, FFTWindow.Rectangular);

                for (int i = 1; i < spectrum.Length - 1; i++)
                {
                    Debug.DrawLine(new Vector3(i - 1, spectrum[i] + 10, 0), new Vector3(i, spectrum[i + 1] + 10, 0), Color.red);
                    Debug.DrawLine(new Vector3(i - 1, Mathf.Log(spectrum[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(spectrum[i]) + 10, 2), Color.cyan);
                    Debug.DrawLine(new Vector3(Mathf.Log(i - 1), spectrum[i - 1] - 10, 1), new Vector3(Mathf.Log(i), spectrum[i] - 10, 1), Color.green);
                    Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(spectrum[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(spectrum[i]), 3), Color.blue);
                }
            }
           
            preparer?.Synchronize();
            if (!audioSource.isPlaying)
            {
                Debug.Log($"{Name()}: AudioSource is not playing. Restarting.");
                audioSource.Play();
            }
        }

        private void LateUpdate()
        {
            preparer?.LatchFrame();
        }

        void OnDestroy()
        {
            reader?.StopAndWait();
            codec?.StopAndWait();
            preparer?.StopAndWait();
        }
        

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (preparer == null)
            {
                return;
            }
            Timestamp currentTimestamp = preparer.getCurrentTimestamp();
            int nZeroSamplesInserted = 0;
            if (channels == 1)
            {
                // Simple case: mono. Just copy.
                nZeroSamplesInserted = preparer.GetAudioBuffer(data, data.Length);
#if unused
                // xxxjack Unsure whether this is needed, maybe the buffer is already clear when we get here.
                if (nZeroSamplesInserted > 0)
                {
                    for(int i=data.Length-nZeroSamplesInserted; i<data.Length; i++)
                    {
                        data[i] = 0;
                    }
                }
#endif
            } 
            else
            {
                Debug.LogWarning($"{Name()}: Convert audio to {channels} channels");
                float[] tmpBuffer = new float[data.Length / channels];
                nZeroSamplesInserted = preparer.GetAudioBuffer(tmpBuffer, tmpBuffer.Length);
                if (nZeroSamplesInserted > 0)
                {
                    for (int i = tmpBuffer.Length - nZeroSamplesInserted; i < tmpBuffer.Length; i++)
                    {
                        tmpBuffer[i] = 0;
                    }
                }
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] += tmpBuffer[i / channels];
                }
            }
            float rmsCompute = 0;
            for (int i = 0; i < data.Length; i++)
            {
                rmsCompute += data[i] * data[i];
            }
            lock(this)
            {
                currentAudioLevel += Mathf.Sqrt(rmsCompute / data.Length);
                currentAudioLevelCount += 1;
            }
            
#if VRT_WITH_STATS
            stats.statsUpdate(data.Length/channels, nZeroSamplesInserted, currentTimestamp, preparer.getQueueDuration());
#endif
        }

        public void SetSyncInfo(SyncConfig.ClockCorrespondence _clockCorrespondence)
        {
            reader.SetSyncInfo(_clockCorrespondence);
        }

#if VRT_WITH_STATS
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name) { }

            double statsTotalAudioframeCount = 0;
            double statsTotalAudioSamples = 0;
            double statsTotalAudioZeroSamples = 0;
            int statsZeroInsertionCount = 0;
            double statsTotalLatency = 0;
            int statsTotalLatencyContributions = 0;
            double statsTotalQueueDuration = 0;

            public void statsUpdate(int nSamples, int nZeroSamples, Timestamp timestamp, Timedelta queueDuration)
            {
                
                statsTotalAudioframeCount++;
                statsTotalAudioSamples += nSamples;
                statsTotalAudioZeroSamples += nZeroSamples;
                if (nZeroSamples > 0) statsZeroInsertionCount++;
                if (timestamp > 0)
                {
                    System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                    Timestamp now = (Timestamp)sinceEpoch.TotalMilliseconds;
                    Timedelta latency = now - timestamp;
                    if (latency < 0 || latency > 1000000)
                    {
                        Debug.LogWarning($"{name}.Stats: preposterous latency {latency}");
                    }
                    statsTotalLatency += latency;
                    statsTotalLatencyContributions++;
                }

                statsTotalQueueDuration += queueDuration;
            
                if (ShouldOutput())
                {
                    double factor = (statsTotalAudioframeCount == 0 ? 1 : statsTotalAudioframeCount);
                    long latency_ms = statsTotalLatencyContributions == 0 ? 0 : (int)(statsTotalLatency / statsTotalLatencyContributions);
                    if (latency_ms < 0 || latency_ms > 1000000)
                    {
                        Debug.LogWarning($"{name}.Stats: preposterous average latency {latency_ms}");
                    }
                    Output($"latency_ms={latency_ms}, fps_output={statsTotalAudioframeCount / Interval():F2}, fps_zero_inserted={statsZeroInsertionCount / Interval():F2}, zero_inserted_percentage={(statsTotalAudioZeroSamples/statsTotalAudioSamples)*100:F2}, zero_inserted_samples={(int)statsTotalAudioZeroSamples}, voicereceiver_queue_ms={(int)(statsTotalQueueDuration / factor)}, samples_per_frame={(int)(statsTotalAudioSamples/factor)}, output_freq={statsTotalAudioSamples/Interval():F2}, timestamp={timestamp}");
                    Clear();
                    statsTotalAudioframeCount = 0;
                    statsTotalAudioSamples = 0;
                    statsTotalAudioZeroSamples = 0;
                    statsZeroInsertionCount = 0;
                    statsTotalLatency = 0;
                    statsTotalLatencyContributions = 0;
                    statsTotalQueueDuration = 0;
                }
            }
        }

        protected Stats stats;
#endif
    }
}
