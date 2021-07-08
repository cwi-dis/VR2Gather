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
        BaseWorker reader;
        BaseWorker codec;
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
            if (micro == "None")
            {
                Debug.LogError("VoiceSender: no microphone, other participants will not hear you");
                return;
            }
            if (proto == Config.ProtocolType.Dash)
            {
                encoderQueue = new QueueThreadSafe("VoiceSenderEncoder");
                senderQueue = new QueueThreadSafe("VoiceSenderSender");
                codec = new VoiceEncoder(encoderQueue, senderQueue);
                reader = new VoiceReader(micro, this, ((VoiceEncoder)codec).bufferSize, encoderQueue);
                B2DWriter.DashStreamDescription[] b2dStreams = new B2DWriter.DashStreamDescription[1];
                b2dStreams[0].inQueue = senderQueue;
                // xxxjack invented VR2a 4CC here. Is there a correct one?
                writer = new B2DWriter(user.sfuData.url_audio, _streamName, "VR2a", _segmentSize, _segmentLife, b2dStreams);
            } 
            else if (proto == Config.ProtocolType.TCP)
            {
                senderQueue = new QueueThreadSafe("VoiceSenderSender");
                const int TCPAudioSamplesPerPacket = 960;
                reader = new VoiceReader(micro, this, TCPAudioSamplesPerPacket, senderQueue);
                B2DWriter.DashStreamDescription[] b2dStreams = new B2DWriter.DashStreamDescription[1];
                b2dStreams[0].inQueue = senderQueue;
                Debug.Log($"xxxjack VoiceSender TCP URL={user.userData.userAudioUrl}");
                writer = new TCPWriter(user.userData.userAudioUrl, "VR2A", b2dStreams);
            }
            else
            {
                encoderQueue = new QueueThreadSafe("VoiceSenderEncoder");
                senderQueue = new QueueThreadSafe("VoiceSenderSender");
                codec = new VoiceEncoder(encoderQueue, senderQueue);
                reader = new VoiceReader(micro, this, ((VoiceEncoder)codec).bufferSize, encoderQueue);
                B2DWriter.DashStreamDescription[] b2dStreams = new B2DWriter.DashStreamDescription[1];
                b2dStreams[0].inQueue = senderQueue;
                writer = new SocketIOWriter(user, _streamName, b2dStreams);
            }
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