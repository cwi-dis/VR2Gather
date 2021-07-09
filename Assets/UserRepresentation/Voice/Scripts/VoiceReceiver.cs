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
        public void Init(User user, string _streamName, int _streamNumber, int _initialDelay, Config.ProtocolType proto)
        {
            stats = new Stats(Name());
            if (synchronizer == null)
            {
                synchronizer = FindObjectOfType<Synchronizer>();
                Debug.Log($"{Name()}: xxxjack synchronizer {synchronizer}, {synchronizer?.Name()}");
            }
            VoiceReader.PrepareDSP();
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialize = true;
            audioSource.spatialBlend = 1.0f;
            audioSource.minDistance = 4f;
            audioSource.maxDistance = 100f;
            audioSource.loop = true;
            audioSource.Play();

            

            if (proto == Config.ProtocolType.Dash)
            {
                decoderQueue = new QueueThreadSafe("VoiceReceiverDecoder", 200, true);
                reader = new BaseSubReader(user.sfuData.url_audio, _streamName, _initialDelay, 0, decoderQueue);
                preparerQueue = new QueueThreadSafe("VoiceReceiverPreparer", 4, false);
                codec = new VoiceDecoder(decoderQueue, preparerQueue);
            }
            else
            if (proto == Config.ProtocolType.TCP)
            {
                preparerQueue = new QueueThreadSafe("VoiceReceiverPreparer", 50, true);
                Debug.Log($"xxxjack VoiceReceiver TCP URL={user.userData.userAudioUrl}");
                reader = new BaseTCPReader(user.userData.userAudioUrl, preparerQueue);
            }
            else
            {
                decoderQueue = new QueueThreadSafe("VoiceReceiverDecoder", 4, true);
                reader = new SocketIOReader(user, _streamName, decoderQueue);
                preparerQueue = new QueueThreadSafe("VoiceReceiverPreparer", 4, false);
                codec = new VoiceDecoder(decoderQueue, preparerQueue);
            }

            
            preparer = new VoicePreparer(preparerQueue);//, optimalAudioBufferSize);
            if (synchronizer != null) preparer.SetSynchronizer(synchronizer);
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
            if (tmpBuffer == null) tmpBuffer = new float[data.Length/channels];
            if (preparer != null && preparer.GetAudioBuffer(tmpBuffer, tmpBuffer.Length))
            {
                for(int i=0; i<data.Length; i++)
                {
                    data[i] += tmpBuffer[i / channels];
                }
                
                stats.statsUpdate(preparer.currentTimestamp, preparer.currentQueueSize, true);
            } else
            {
                stats.statsUpdate(0, 0, false);
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
            double statsTotalQueueSize = 0;

            public void statsUpdate(long timestamp, int queueSize, bool fresh)
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
                    return; //backport candidate
                }
                statsTotalQueueSize += queueSize;
 
                if (ShouldOutput())
                {
                    System.TimeSpan sinceEpoch = System.DateTime.UtcNow - new System.DateTime(1970, 1, 1);
                    Output($"fps={statsTotalAudioframeCount / Interval():F2}, fps_nodata={statsTotalUnavailableCount / Interval():F2}, latency_ms={statsTotalLatency/(statsTotalAudioframeCount==0?1:statsTotalAudioframeCount)}, avg_queuesize={statsTotalQueueSize/(statsTotalAudioframeCount+statsTotalUnavailableCount):F2}, timestamp={timestamp}");
                }
                if (ShouldClear())
                {
                    Clear();
                    statsTotalAudioframeCount = 0;
                    statsTotalUnavailableCount = 0;
                    statsTotalLatency = 0;
                    statsTotalQueueSize = 0;
                }
            }
        }

        protected Stats stats;
    }
}
