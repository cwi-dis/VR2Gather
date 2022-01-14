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
    public class VoiceSender : MonoBehaviour
    {
        VoiceReader reader;
        VoiceEncoder codec;
        BaseWriter writer;

        // xxxjack nothing is dropped here. Need to investigate what is the best idea.
        QueueThreadSafe encoderQueue = null;
        QueueThreadSafe senderQueue = null;

        // Start is called before the first frame update
        public void Init(User user, string _streamName, int _segmentSize, int _segmentLife, Config.ProtocolType proto)
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

            string audioCodec = Config.Instance.audioCodec;
            bool audioIsEncoded = audioCodec == "VR2A";

            QueueThreadSafe _readerOutputQueue = null;
            if (audioIsEncoded)
            {
                encoderQueue = new QueueThreadSafe("VoiceSenderEncoder", 4, true);
                senderQueue = new QueueThreadSafe("VoiceSenderSender");
                codec = new VoiceEncoder(encoderQueue, senderQueue);
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

            reader = new VoiceReader(micro, Config.Instance.audioSampleRate, Config.Instance.audioFps, minBufferSize, this, _readerOutputQueue);
            int audioSamplesPerPacket = reader.getBufferSize();
            if (codec != null && audioSamplesPerPacket % codec.minSamplesPerFrame != 0)
            {
                Debug.LogWarning($"VoiceSender: encoder wants {codec.minSamplesPerFrame} samples but we want {audioSamplesPerPacket}");
            }

            B2DWriter.DashStreamDescription[] b2dStreams = new B2DWriter.DashStreamDescription[1];
            b2dStreams[0].inQueue = senderQueue;

            if (proto == Config.ProtocolType.Dash)
            {
                writer = new B2DWriter(user.sfuData.url_audio, _streamName, audioCodec, _segmentSize, _segmentLife, b2dStreams);
            } 
            else if (proto == Config.ProtocolType.TCP)
            {
                writer = new TCPWriter(user.userData.userAudioUrl, audioCodec, b2dStreams);
            }
            else
            {
                writer = new SocketIOWriter(user, _streamName, audioCodec, b2dStreams);
            }
            BaseStats.Output("VoiceSender", $"encoded={audioIsEncoded}, samples_per_buffer={audioSamplesPerPacket}, writer={writer.Name()}");
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