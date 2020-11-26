using Dash;
using Orchestrator;
using SocketIO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTCore;

public class VoiceSender : MonoBehaviour {
    BaseWorker reader;
    BaseWorker codec;
    BaseWriter writer;

    // xxxjack nothing is dropped here. Need to investigate what is the best idea.
    QueueThreadSafe encoderQueue = new QueueThreadSafe("VoiceSenderEncoder");
    QueueThreadSafe senderQueue = new QueueThreadSafe("VoiceSenderSender");

    // Start is called before the first frame update
    public void Init(User user, string _streamName, int _segmentSize, int _segmentLife, bool UseDash) {
        string micro = null;
        if (user != null && user.userData != null)
            micro = user.userData.microphoneName;
        if (micro == "None") {
            Debug.LogError("VoiceSender: no microphone, other participants will not hear you");
            return;
        }

        codec  = new Workers.VoiceEncoder(encoderQueue, senderQueue);
        reader = new Workers.VoiceReader(micro, this, ((Workers.VoiceEncoder)codec).bufferSize, encoderQueue);
        B2DWriter.DashStreamDescription[] b2dStreams = new B2DWriter.DashStreamDescription[1];
        b2dStreams[0].inQueue = senderQueue;
        // xxxjack invented VR2a 4CC here. Is there a correct one?
        if(UseDash) writer = new B2DWriter(user.sfuData.url_audio, _streamName, "VR2a", _segmentSize, _segmentLife, b2dStreams);
        else        writer = new SocketIOWriter(user, _streamName, b2dStreams);
    }

    void OnDestroy() {
        reader?.Stop();
        reader = null;
        codec?.Stop();
        codec = null;
        writer?.Stop();
        writer = null;
    }

    public SyncConfig.ClockCorrespondence GetSyncInfo() {
        if(writer==null) return new SyncConfig.ClockCorrespondence();
        return writer.GetSyncInfo();
    }
}
