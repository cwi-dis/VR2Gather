using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Orchestrator.Wrapping;
using VRT.Core;
using Cwipc;
using System;
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
        public void Init(bool isLocalPlayer, object _user, VRTConfig._User cfg, bool preview = false)
        //public void Init(User user, string _streamName, int _segmentSize, int _segmentLife)
        {
            User user = (User)_user;
            string _streamName = "audio";
            string micro = null;
            if (user != null && user.userData != null)
                micro = user.userData.microphoneName;
            int minBufferSize = 0;
            if (micro == "None")
            {
                Debug.LogError($"{Name()}: no microphone, other participants will not hear you");
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

            reader = new AsyncVoiceReader(micro, VRTConfig.Instance.audioSampleRate, VRTConfig.Instance.Voice.audioFps, minBufferSize, this, _readerOutputQueue);
            int audioSamplesPerPacket = reader.getBufferSize();
            if (codec != null && audioSamplesPerPacket % codec.minSamplesPerFrame != 0)
            {
                Debug.LogWarning($"{Name()}: encoder wants {codec.minSamplesPerFrame} samples but we want {audioSamplesPerPacket}");
            }

            OutgoingStreamDescription[] b2dStreams = new OutgoingStreamDescription[1];
            b2dStreams[0].inQueue = senderQueue;
            // We need some backward-compatibility hacks, depending on protocol type.
            string url = user.sfuData.url_gen;
            switch (proto)
            {
                case "tcp":
                    url = user.userData.userAudioUrl;
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