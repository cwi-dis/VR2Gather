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
        //
        // Debug code to test what is going wrong with audio.
        // Setting debugReplaceByTone will replace all incoming audio data with a 440Hz tone
        // Setting debugAddTone will add the tone.
        const bool debugReplaceByTone = false;
        const bool debugAddTone = false;
        ToneGenerator debugToneGenerator = null;

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
            if (debugAddTone || debugReplaceByTone)
            {
                debugToneGenerator = new ToneGenerator();
            }
            stats = new Stats(Name());
            if (synchronizer == null)
            {
                synchronizer = FindObjectOfType<Synchronizer>();
            }
            VoiceReader.PrepareDSP(Config.Instance.audioSampleRate, 0);
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

        public void Init(User user, QueueThreadSafe queue)
        {
            if (debugAddTone || debugReplaceByTone)
            {
                debugToneGenerator = new ToneGenerator();
            }
            stats = new Stats(Name());
            if (synchronizer == null)
            {
                synchronizer = FindObjectOfType<Synchronizer>();
            }
            VoiceReader.PrepareDSP(Config.Instance.audioSampleRate, 0);
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialize = true;
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 4f;
            audioSource.maxDistance = 100f;
            audioSource.loop = true;
            audioSource.Play();

            string audioCodec = Config.Instance.audioCodec;
            bool audioIsEncoded = audioCodec == "VR2A";

            preparerQueue = null;
            QueueThreadSafe _readerOutputQueue = queue;
            if (audioIsEncoded)
            {
                preparerQueue = new QueueThreadSafe("voicePreparer", 4, true);
                codec = new VoiceDecoder(queue, preparerQueue);
                _readerOutputQueue = preparerQueue;
            }

            preparer = new VoicePreparer(_readerOutputQueue);
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
            if (preparer == null)
            {
                return;
            }
            if (tmpBuffer == null)
            {
                tmpBuffer = new float[data.Length / channels];
            }
            int nZeroSamplesInserted = preparer.GetAudioBuffer(tmpBuffer, tmpBuffer.Length);
            if (debugReplaceByTone)
            {
                for (int i = 0; i < tmpBuffer.Length; i++) tmpBuffer[i] = 0;
                for (int i = 0; i < data.Length; i++) data[i] = 0;
            }
            if (debugAddTone || debugReplaceByTone)
            {
                debugToneGenerator.addTone(tmpBuffer);
            }
            for (int i=0; i<data.Length; i++)
            {
                data[i] += tmpBuffer[i / channels];
            }
            stats.statsUpdate(data.Length, nZeroSamplesInserted, preparer.currentTimestamp, preparer.getQueueDuration());
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
            double statsTotalAudioZeroSamples = 0;
            int statsZeroInsertionCount = 0;
            double statsTotalLatency = 0;
            double statsTotalQueueDuration = 0;

            public void statsUpdate(int nSamples, int nZeroSamples, ulong timestamp, ulong queueDuration)
            {
                
                statsTotalAudioframeCount++;
                statsTotalAudioSamples += nSamples;
                statsTotalAudioZeroSamples += nZeroSamples;
                if (nZeroSamples > 0) statsZeroInsertionCount++;
                System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                long now = (long)sinceEpoch.TotalMilliseconds;
                long latency = now - (long)timestamp;
                statsTotalLatency += latency;
            
                statsTotalQueueDuration += queueDuration;
            
                if (ShouldOutput())
                {
                    double factor = (statsTotalAudioframeCount == 0 ? 1 : statsTotalAudioframeCount);
                    Output($"latency_ms={(int)(statsTotalLatency / factor)}, fps_output={statsTotalAudioframeCount / Interval():F2} fps_dropout={statsZeroInsertionCount / Interval():F2}, dropout_percentage={(statsTotalAudioZeroSamples/statsTotalAudioSamples)*100:F2}, dropout_samples={(int)statsTotalAudioZeroSamples}, voicereceiver_queue_ms={(int)(statsTotalQueueDuration / factor)}, samples_per_frame={(int)(statsTotalAudioSamples/factor)}, timestamp={timestamp}");
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalAudioframeCount = 0;
                    statsTotalAudioSamples = 0;
                    statsTotalAudioZeroSamples = 0;
                    statsZeroInsertionCount = 0;
                    statsTotalLatency = 0;
                    statsTotalQueueDuration = 0;
                }
            }
        }

        protected Stats stats;
    }
}
