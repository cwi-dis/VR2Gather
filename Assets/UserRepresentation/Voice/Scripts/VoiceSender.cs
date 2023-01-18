﻿using System.Collections;
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
    using QueueThreadSafe = Cwipc.QueueThreadSafe;
    using BaseMemoryChunk = Cwipc.BaseMemoryChunk;
    using OutgoingStreamDescription = Cwipc.StreamSupport.OutgoingStreamDescription;

    public class VoiceSender : MonoBehaviour
    {
        AsyncVoiceReader reader;
        AsyncVoiceEncoder codec;
        AsyncWriter writer;

        // xxxjack nothing is dropped here. Need to investigate what is the best idea.
        QueueThreadSafe encoderQueue = null;
        QueueThreadSafe senderQueue = null;

        // Start is called before the first frame update
        public void Init(User user, string _streamName, int _segmentSize, int _segmentLife, VRTConfig.ProtocolType proto)
        {
            string micro = null;
            if (user != null && user.userData != null)
                micro = user.userData.microphoneName;
            int minBufferSize = 0;
            if (micro == "None")
            {
                Debug.LogError("VoiceSender: no microphone, other participants will not hear you");
                return;
            }

            string audioCodec = VRTConfig.Instance.Voice.Codec;
            bool audioIsEncoded = audioCodec == "VR2A";

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
                Debug.LogWarning($"VoiceSender: encoder wants {codec.minSamplesPerFrame} samples but we want {audioSamplesPerPacket}");
            }

            OutgoingStreamDescription[] b2dStreams = new OutgoingStreamDescription[1];
            b2dStreams[0].inQueue = senderQueue;

            if (proto == VRTConfig.ProtocolType.Dash)
            {
                writer = new AsyncB2DWriter(user.sfuData.url_audio, _streamName, audioCodec, _segmentSize, _segmentLife, b2dStreams);
            } 
            else if (proto == VRTConfig.ProtocolType.TCP)
            {
                writer = new AsyncTCPWriter(user.userData.userAudioUrl, audioCodec, b2dStreams);
            }
            else
            {
                writer = new AsyncSocketIOWriter(user, _streamName, audioCodec, b2dStreams);
            }
            string encoderName = "none";
            if (codec != null)
            {
                encoderName = codec.Name();
            }
#if VRT_WITH_STATS
            Statistics.Output("VoiceSender", $"encoded={audioIsEncoded}, samples_per_buffer={audioSamplesPerPacket}, reader={reader.Name()}, encoder={encoderName}, writer={writer.Name()}");
#endif
        }

        public void Init(User user, QueueThreadSafe queue)
        {
            string micro = null;
            if (user != null && user.userData != null)
                micro = user.userData.microphoneName;
            int minBufferSize = 0;
            if (micro == "None")
            {
                Debug.LogError("VoiceSender: no microphone, other participants will not hear you");
                return;
            }

            string audioCodec = VRTConfig.Instance.Voice.Codec;
            bool audioIsEncoded = audioCodec == "VR2A";

            QueueThreadSafe _readerOutputQueue = null;
            if (audioIsEncoded)
            {
                encoderQueue = new QueueThreadSafe("VoiceSenderEncoder", 4, true);
                senderQueue = queue;
                codec = new AsyncVoiceEncoder(encoderQueue, senderQueue);
                minBufferSize = codec.minSamplesPerFrame;
                _readerOutputQueue = encoderQueue;
            }
            else
            {
                encoderQueue = null;
                codec = null;
                senderQueue = queue;
                _readerOutputQueue = senderQueue;
            }

            reader = new AsyncVoiceReader(micro, VRTConfig.Instance.audioSampleRate, VRTConfig.Instance.Voice.audioFps, minBufferSize, this, _readerOutputQueue);
            int audioSamplesPerPacket = reader.getBufferSize();
            if (codec != null && audioSamplesPerPacket % codec.minSamplesPerFrame != 0)
            {
                Debug.LogWarning($"VoiceSender: encoder wants {codec.minSamplesPerFrame} samples but we want {audioSamplesPerPacket}");
            }

#if VRT_WITH_STATS
            Statistics.Output("VoiceSender", $"encoded={audioIsEncoded}, samples_per_buffer={audioSamplesPerPacket}, writer=none");
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