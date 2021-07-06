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
        public void Init(User user, string _streamName, int _streamNumber, int _initialDelay, Config.ProtocolType proto)
        {
            stats = new Stats(Name());
            VoiceReader.PrepareDSP();
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialize = true;
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 4f;
            audioSource.maxDistance = 100f;
            audioSource.loop = true;
            audioSource.Play();

            preparerQueue = new QueueThreadSafe("VoiceReceiverPreparer", 4, false);

            if (proto == Config.ProtocolType.Dash)
            {
                decoderQueue = new QueueThreadSafe("VoiceReceiverDecoder", 200, true);
                reader = new BaseSubReader(user.sfuData.url_audio, _streamName, _initialDelay, 0, decoderQueue);
            }
            else
            if (proto == Config.ProtocolType.TCP)
            {
                decoderQueue = new QueueThreadSafe("VoiceReceiverDecoder", 200, true);
                Debug.Log($"xxxjack VoiceReceiver TCP URL={user.userData.userAudioUrl}");
                reader = new BaseTCPReader(user.userData.userAudioUrl, decoderQueue);
            }
            else
            {
                decoderQueue = new QueueThreadSafe("VoiceReceiverDecoder", 4, true);
                reader = new SocketIOReader(user, _streamName, decoderQueue);
            }

            codec = new VoiceDecoder(decoderQueue, preparerQueue);
            preparer = new VoicePreparer(preparerQueue);//, optimalAudioBufferSize);
            // xxxjack should set Synchronizer here
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
            if (tmpBuffer == null) tmpBuffer = new float[data.Length];
            if (preparer != null && preparer.GetAudioBuffer(tmpBuffer, tmpBuffer.Length))
            {
                int cnt = 0;
                do
                {
                    data[cnt] += tmpBuffer[cnt];
                } while (++cnt < data.Length);
                stats.statsUpdate(preparer.currentTimestamp, true);
            } else
            {
                stats.statsUpdate(0, false);
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
            double statsTotalUnavailableCount = 0;
            double statsTotalLatency = 0;

            public void statsUpdate(long timestamp, bool fresh)
            {
                if (fresh)
                {
                    statsTotalAudioframeCount++;
                    System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                    long now = (long)sinceEpoch.TotalMilliseconds;
                    long latency = now - timestamp;
                    statsTotalLatency += latency;
                } else
                {
                    statsTotalUnavailableCount++;
                }
 
                if (ShouldOutput())
                {
                    System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                    Output($"fps={statsTotalAudioframeCount / Interval():F2}, fps_nodata={statsTotalUnavailableCount / Interval():F2}, latency_ms={statsTotalLatency/(statsTotalAudioframeCount==0?1:statsTotalAudioframeCount)}, timestamp={timestamp}");
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalAudioframeCount = 0;
                    statsTotalUnavailableCount = 0;
                    statsTotalLatency = 0;
                }
            }
        }

        protected Stats stats;
    }
}
