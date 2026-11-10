using UnityEngine;
using VRT.Orchestrator;
using VRT.Core;
using Cwipc;
using UnityEngine.Windows.WebCam;
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
            // The login scene calls Init again on the same component every time it shows the Home dialog.
            // Stop the previous pipeline, or its reader thread keeps running with an unclosed queue (#380).
            StopPipeline();
            User user = (User)_user;
            string _streamName = "audio";
            string microphoneName = VRTConfig.Instance.RepresentationConfig.microphoneName;
            int minBufferSize = 0;
            if (microphoneName == "None" || microphoneName == "" || microphoneName == "Muted")
            {
                Debug.Log($"{Name()}: no microphone (\"{microphoneName}\"), other participants will not hear you");
                return;
            }
            if (!AsyncVoiceReader.CustomDeviceNames.Contains(microphoneName) && System.Array.IndexOf(Microphone.devices, microphoneName) < 0)
            {
                // The configured microphone is gone (unplugged, Bluetooth off): behave as "None" for now, but keep
                // the configured name, so it works again when the microphone is re-attached (#374).
                Debug.LogWarning($"{Name()}: microphone \"{microphoneName}\" not found, other participants will not hear you");
                return;
            }

            string audioCodec = SessionConfig.Instance.voiceCodec;
            bool audioIsEncoded = audioCodec == "VR2A";
            string proto = SessionConfig.Instance.protocolType;

            // Without a writer nobody reads senderQueue. A non-dropping senderQueue would then fill up and
            // block the encoder, so in that case there is no encoder and the reader writes to a dropping queue.
            // The reader still computes MicrophoneLevel, for the level meter in the preview.
            bool transmit = !preview && VRTOrchestratorSingleton.Comm.IsNetworked;

            QueueThreadSafe _readerOutputQueue = null;
            if (audioIsEncoded && transmit)
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

            try
            {
                reader = AsyncVoiceReader.Create(microphoneName, VRTConfig.Instance.VoiceConfig.AudioSampleRate, VRTConfig.Instance.VoiceConfig.audioFps, minBufferSize, this, _readerOutputQueue);
            }
            catch (System.Exception e)
            {
                // For example an app's own reader that can't open a microphone that has been unplugged.
                // Don't let it break the caller (the login menu, #374): continue without voice.
                Debug.LogError($"{Name()}: cannot open microphone \"{microphoneName}\", other participants will not hear you: {e.Message}");
                // The reader would have closed its output queue when stopping; close it ourselves so the
                // encoder (waiting on it) stops instead of timing out.
                _readerOutputQueue?.Close();
                StopPipeline();
                return;
            }
            int audioSamplesPerPacket = reader.getBufferSize();
            if (codec != null && audioSamplesPerPacket % codec.minSamplesPerFrame != 0)
            {
                Debug.LogWarning($"{Name()}: encoder wants {codec.minSamplesPerFrame} samples but we want {audioSamplesPerPacket}");
            }

            if (!transmit)
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
            StopPipeline();
        }

        void StopPipeline()
        {
            // Upstream first: each worker closes its own output queue when it stops, which also
            // ends the next worker in the chain. So the queues are not closed here (#380).
            reader?.StopAndWait();
            reader = null;
            codec?.StopAndWait();
            codec = null;
            writer?.StopAndWait();
            writer = null;
            encoderQueue = null;
            senderQueue = null;
        }

        public SyncConfig.ClockCorrespondence GetSyncInfo()
        {
            if (writer == null) return new SyncConfig.ClockCorrespondence();
            return writer.GetSyncInfo();
        }

        public void StartRecording(string filename)
        {
            if (reader == null)
            {
                Debug.LogError($"{Name()}: StartRecording: no voice reader");
                return;
            }
            if (string.IsNullOrEmpty(filename)) {
                Debug.LogError($"{Name()}: StartRecording: filename is empty");
                return;
            }
            reader.StartRecording(filename);
        }

        public void StopRecording()
        {
            if (reader == null) return;
            reader.StopRecording();
        }
    }
}