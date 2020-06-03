using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceDashSender : MonoBehaviour {
    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.BaseWorker writer;

    // xxxjack nothing is dropped here. Need to investigate what is the best idea.
    QueueThreadSafe encoderQueue = new QueueThreadSafe();
    QueueThreadSafe senderQueue = new QueueThreadSafe();

    // Start is called before the first frame update
    public void Init(string _url, string _streamName, int _segmentSize, int _segmentLife) {
        codec  = new Workers.VoiceEncoder(encoderQueue, senderQueue);
        reader = new Workers.VoiceReader(this, ((Workers.VoiceEncoder)codec).bufferSize, encoderQueue);
        Workers.B2DWriter.DashStreamDescription[] b2dStreams = new Workers.B2DWriter.DashStreamDescription[1];
        b2dStreams[0].inQueue = senderQueue;
        // xxxjack invented VR2a 4CC here. Is there a correct one?
        writer = new Workers.B2DWriter(_url,  _streamName, "VR2a", _segmentSize, _segmentLife, b2dStreams);
    }

    void OnDestroy() {
        reader?.Stop();
        codec?.Stop();
        writer?.Stop();
    }
}
