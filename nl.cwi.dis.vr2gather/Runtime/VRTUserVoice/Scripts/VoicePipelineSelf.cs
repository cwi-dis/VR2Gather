using UnityEngine;
using VRT.Orchestrator;
using VRT.Core;
using Cwipc;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.UserRepresentation.Voice
{
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
    using BaseMemoryChunk = Cwipc.BaseMemoryChunk;
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;

    public class VoicePipelineSelf : MonoBehaviour
    {
        [Tooltip("Current input level")]
        public float MicrophoneLevel
        {
            get
            {
                if (reader != null) return reader.MicrophoneLevel;
                return 0;
            }
        }

        AsyncVoiceReader reader;
        AsyncVoiceEncoder codec;
        ITransportProtocolWriter writer;

        // xxxjack nothing is dropped here. Need to investigate what is the best idea.
        QueueThreadSafe encoderQueue = null;
        QueueThreadSafe senderQueue = null;

        public string Name()
        {
            return $"{GetType().Name}";
        }

        // Start is called before the first frame update
        public void Init(bool isLocalPlayer, object _user, VRTConfig.RepresentationConfigType cfg, bool preview = false)
        //public void Init(User user, string _streamName, int _segmentSize, int _segmentLife)
        {
            User user = (User)_user;
            string _streamName = "audio";
            string microphoneName = VRTConfig.Instance.RepresentationConfig.microphoneName;
            int minBufferSize = 0;
            if (microphoneName == "None" || microphoneName == "")
            {
                Debug.Log($"{Name()}: no microphone, other participants will not hear you");
                return;
            }

            string audioCodec = SessionConfig.Instance.voiceCodec;
            bool audioIsEncoded = audioCodec == "VR2A";
            string proto = SessionConfig.Instance.protocolType;

            QueueThreadSafe _readerOutputQueue = null;
            if (audioIsEncoded)
            {
                encoderQueue = new QueueThreadSafe("VoiceSenderEncoder", 4, true);
                senderQueue = new QueueThreadSafe("VoiceSenderSender");
                codec = new AsyncVoiceEncoder(encoderQueue, senderQueue);
                minBufferSize = codec.minSamplesPerFrame;
                _readerOutputQueue = encoderQueue;
            }
            else
            {
                encoderQueue = null;
                codec = null;
                senderQueue = new QueueThreadSafe("VoiceSenderSender", 4, true);
                _readerOutputQueue = senderQueue;
            }

            reader = new AsyncVoiceReader(microphoneName, VRTConfig.Instance.VoiceConfig.AudioSampleRate, VRTConfig.Instance.VoiceConfig.audioFps, minBufferSize, this, _readerOutputQueue);
            int audioSamplesPerPacket = reader.getBufferSize();
            if (codec != null && audioSamplesPerPacket % codec.minSamplesPerFrame != 0)
            {
                Debug.LogWarning($"{Name()}: encoder wants {codec.minSamplesPerFrame} samples but we want {audioSamplesPerPacket}");
            }

            if (preview)
            {
                return;
            }
            OutgoingStreamDescription[] b2dStreams = new OutgoingStreamDescription[1];
            b2dStreams[0].inQueue = senderQueue;
            // We need some backward-compatibility hacks, depending on protocol type.
            string url = user.sfuData?.url_gen;
            switch (proto)
            {
                case "tcp":
                    url = VRTConfig.Instance.RepresentationConfig.userRepresentationTCPUrl;
                    break;
            }
            writer = TransportProtocol.NewWriter(proto).Init(url, user.userId, _streamName, audioCodec, b2dStreams);
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"proto={proto}, url={url}, streamName={_streamName}, codec={audioCodec}");
#endif

            string encoderName = "none";
            if (codec != null)
            {
                encoderName = codec.Name();
            }
#if VRT_WITH_STATS
            Statistics.Output(Name(), $"encoded={audioIsEncoded}, samples_per_buffer={audioSamplesPerPacket}, reader={reader.Name()}, encoder={encoderName}, writer={writer.Name()}");
#endif
        }


        void OnDestroy()
        {
            reader?.Stop();
            reader = null;
            codec?.Stop();
            codec = null;
            writer?.Stop();
            writer = null;
            encoderQueue?.Close();
            senderQueue?.Close();
        }

        public SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            if (writer == null) return new SyncConfig.ClockCorrespondence();
            return writer.GetSyncInfo();
        }
    }
}