using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceSender : MonoBehaviour {
    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.BaseWriter writer;

    // xxxjack nothing is dropped here. Need to investigate what is the best idea.
    QueueThreadSafe encoderQueue = new QueueThreadSafe();
    QueueThreadSafe senderQueue = new QueueThreadSafe();

    // Start is called before the first frame update
    public void Init(OrchestratorWrapping.User user, string _streamName, int _segmentSize, int _segmentLife, bool UseDash) {
        codec  = new Workers.VoiceEncoder(encoderQueue, senderQueue);
        reader = new Workers.VoiceReader(user.userData.microphoneName, this, ((Workers.VoiceEncoder)codec).bufferSize, encoderQueue);
        Workers.B2DWriter.DashStreamDescription[] b2dStreams = new Workers.B2DWriter.DashStreamDescription[1];
        b2dStreams[0].inQueue = senderQueue;
        // xxxjack invented VR2a 4CC here. Is there a correct one?
        if(UseDash) writer = new Workers.B2DWriter(user.sfuData.url_audio, _streamName, "VR2a", _segmentSize, _segmentLife, b2dStreams);
        else        writer = new Workers.SocketIOWriter(user, _streamName, b2dStreams);
    }

    void OnDestroy() {
        reader?.Stop();
        codec?.Stop();
        writer?.Stop();
    }

    public SyncConfig.ClockCorrespondence GetSyncInfo()
    {
        return writer.GetSyncInfo();
    }
}
