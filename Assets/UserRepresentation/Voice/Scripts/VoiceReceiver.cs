using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Transport.SocketIO;
using VRT.Transport.Dash;
using VRT.Transport.TCP;
using VRT.Orchestrator.Wrapping;
using VRT.Core;

namespace VRT.UserRepresentation.Voice
{
    public class VoiceReceiver : MonoBehaviour
    {
        BaseReader reader;
        BaseWorker codec;
        VoicePreparer preparer;

        [Tooltip("Object responsible for synchronizing playout")]
        public Synchronizer synchronizer = null;

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
        public void Init(User user, string _streamName, int _streamNumber, Config.ProtocolType proto)
        {
            stats = new Stats(Name());
            if (synchronizer == null)
            {
                synchronizer = FindObjectOfType<Synchronizer>();
            }
            VoiceReader.PrepareDSP();
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialize = true;
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 4f;
            audioSource.maxDistance = 100f;
            audioSource.loop = true;
            audioSource.Play();

            string audioCodec = Config.Instance.audioCodec;
            bool audioIsEncoded = audioCodec == "VR2A";

            preparerQueue = new QueueThreadSafe("VoiceReceiverPreparer", 200, false);
            QueueThreadSafe _readerOutputQueue = preparerQueue;
            if (audioIsEncoded)
            {
                decoderQueue = new QueueThreadSafe("VoiceReceiverDecoder", 10, true);
                codec = new VoiceDecoder(decoderQueue, preparerQueue);
                _readerOutputQueue = decoderQueue;
            }

            if (proto == Config.ProtocolType.Dash)
            {
                reader = new BaseSubReader(user.sfuData.url_audio, _streamName, _streamNumber, audioCodec, _readerOutputQueue);
            }
            else
            if (proto == Config.ProtocolType.TCP)
            {
                reader = new BaseTCPReader(user.userData.userAudioUrl, audioCodec, _readerOutputQueue);
            }
            else
            {
                reader = new SocketIOReader(user, _streamName, audioCodec, _readerOutputQueue);
            }

            
            preparer = new VoicePreparer(preparerQueue);
            if (synchronizer != null) preparer.SetSynchronizer(synchronizer);
            BaseStats.Output(Name(), $"encoded={audioIsEncoded}, reader={reader.Name()}");
        }

        private void Update()
        {
            preparer?.Synchronize();
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
        /*
        void OnAudioRead(float[] data) {
            if (preparer == null || !preparer.GetAudioBuffer(data, data.Length))
                System.Array.Clear(data, 0, data.Length);
        }
    */

        float[] tmpBuffer;
        void OnAudioFilterRead(float[] data, int channels)
        {
            if (tmpBuffer == null) tmpBuffer = new float[data.Length/channels];
            if (preparer != null && preparer.GetAudioBuffer(tmpBuffer, tmpBuffer.Length))
            {
                for(int i=0; i<data.Length; i++)
                {
                    data[i] += tmpBuffer[i / channels];
                }
                
                stats.statsUpdate(data.Length, preparer.currentTimestamp, preparer.getQueueDuration(), true);
            } else
            {
                stats.statsUpdate(0, 0, 0, false);
            }
        }

        public void SetSyncInfo(SyncConfig.ClockCorrespondence _clockCorrespondence)
        {
            reader.SetSyncInfo(_clockCorrespondence);
        }

        protected class Stats : VRT.Core.BaseStats
        {
            public Stats(string name) : base(name) { }

            double statsTotalAudioframeCount = 0;
            double statsTotalAudioSamples = 0;
            double statsTotalUnavailableCount = 0;
            double statsTotalLatency = 0;
            double statsTotalQueueDuration = 0;

            public void statsUpdate(int nSamples, ulong timestamp, ulong queueDuration, bool fresh)
            {
                if (fresh)
                {
                    statsTotalAudioframeCount++;
                    statsTotalAudioSamples += nSamples;
                    System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                    long now = (long)sinceEpoch.TotalMilliseconds;
                    long latency = now - (long)timestamp;
                    statsTotalLatency += latency;
                } else
                {
                    statsTotalUnavailableCount++;
                    // return; //backport candidate
                }
                statsTotalQueueDuration += queueDuration;
            
                if (ShouldOutput())
                {
                    double factor = (statsTotalAudioframeCount == 0 ? 1 : statsTotalAudioframeCount);
                    Output($"fps={statsTotalAudioframeCount / Interval():F2}, latency_ms={(int)(statsTotalLatency / factor)}, fps_nodata={statsTotalUnavailableCount / Interval():F2}, voicereceiver_queue_ms={(int)(statsTotalQueueDuration / factor)}, samples_per_frame={(int)(statsTotalAudioSamples/factor)}, timestamp={timestamp}");
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalAudioframeCount = 0;
                    statsTotalAudioSamples = 0;
                    statsTotalUnavailableCount = 0;
                    statsTotalLatency = 0;
                    statsTotalQueueDuration = 0;
                }
            }
        }

        protected Stats stats;
    }
}
