using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceIOSender : MonoBehaviour {
    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.BaseWorker writer;

    // xxxjack nothing is dropped here. Need to investigate what is the best idea.
    QueueThreadSafe encoderQueue = new QueueThreadSafe();
    QueueThreadSafe senderQueue = new QueueThreadSafe();

    // Start is called before the first frame update
    public void Init(string remoteURL, string remoteStream) {
        codec  = new Workers.VoiceEncoder(encoderQueue, senderQueue);
        reader = new Workers.VoiceReader(this, ((Workers.VoiceEncoder)codec).bufferSize, encoderQueue);
        Workers.B2DWriter.DashStreamDescription[] streams = new Workers.B2DWriter.DashStreamDescription[1] {
                new Workers.B2DWriter.DashStreamDescription() {
                    tileNumber = 0,
                    quality = 0,
                    inQueue = senderQueue
                }
            };


        writer = new Workers.SocketIOWriter(remoteURL, remoteStream, streams);
    }

    void OnDestroy() {
        reader?.Stop();
        codec?.Stop();
        writer?.Stop();
    }
}
