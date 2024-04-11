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

    public class VoiceReceiver : MonoBehaviour
    {
        AsyncReader reader;
        AsyncWorker codec;
        AsyncVoicePreparer preparer;
        public AudioSource audioSource;

        [Tooltip("Object responsible for synchronizing playout")]
        public ISynchronizer synchronizer = null;

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
        public void Init(User user, string _streamName, int _streamNumber)
        {
#if VRT_WITH_STATS
            stats = new Stats(Name());
#endif
            if (synchronizer == null)
            {
                synchronizer = FindObjectOfType<VRTSynchronizer>();
            }
            AsyncVoiceReader.PrepareDSP(VRTConfig.Instance.audioSampleRate, 0);
            if (audioSource != null)
            {
                Debug.LogWarning($"{Name()}: already have an AudioSource, overriding");
            }
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialize = true;
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
        

        float[] tmpBuffer;
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (preparer == null)
            {
                return;
            }
            if (tmpBuffer == null)
            {
                tmpBuffer = new float[data.Length / channels];
            }
            int nZeroSamplesInserted = preparer.GetAudioBuffer(tmpBuffer, tmpBuffer.Length);
            if (nZeroSamplesInserted > 0)
            {
                for(int i=tmpBuffer.Length-nZeroSamplesInserted; i < tmpBuffer.Length; i++)
                {
                    tmpBuffer[i] = 0;
                }
            }
            for (int i=0; i<data.Length; i++)
            {
                data[i] += tmpBuffer[i / channels];
            }
#if VRT_WITH_STATS
            stats.statsUpdate(data.Length/channels, nZeroSamplesInserted, preparer.getCurrentTimestamp(), preparer.getQueueDuration());
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
