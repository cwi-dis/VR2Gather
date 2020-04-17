using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceDashSender : MonoBehaviour {
    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.BaseWorker writer;

    QueueThreadSafe encoderQueue = new QueueThreadSafe();
    QueueThreadSafe senderQueue = new QueueThreadSafe();

    // Start is called before the first frame update
    public void Init(Config._User._PCSelfConfig._Bin2Dash cfg, string _url = "") {
        codec  = new Workers.VoiceEncoder(encoderQueue, senderQueue);
        reader = new Workers.VoiceReader(this, ((Workers.VoiceEncoder)codec).bufferSize, encoderQueue);
        writer = new Workers.B2DWriter(cfg, _url, senderQueue);
    }

    void OnDestroy() {
        reader?.Stop();
        codec?.Stop();
        writer?.Stop();
    }
}
