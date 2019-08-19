using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceDashSender : MonoBehaviour
{
    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.BaseWorker writer;


    // Start is called before the first frame update
    public void Init(Config._User._PCSelfConfig._Bin2Dash cfg) {
        codec = new Workers.VoiceEncoder();
        reader = new Workers.VoiceReader(this, ((Workers.VoiceEncoder)codec).bufferSize);
        writer = new Workers.B2DWriter(cfg);
        reader.AddNext(codec).AddNext(writer).AddNext(reader);
        reader.token = new Workers.Token(1);
    }

    // Start is called before the first frame update
    public void Init(Config._User._PCSelfConfig._Bin2Dash cfg, string id) {
        reader = new Workers.VoiceReader(this, ((Workers.VoiceEncoder)codec).bufferSize);
        codec = new Workers.VoiceEncoder();
        writer = new Workers.B2DWriter(cfg, id);
        reader.AddNext(codec).AddNext(writer).AddNext(reader);
        reader.token = new Workers.Token(1);
    }

    void OnDestroy() {
        reader?.Stop();
        codec?.Stop();
        writer?.Stop();
    }
}
