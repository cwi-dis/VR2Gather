using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVoiceSender : MonoBehaviour
{
    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.BaseWorker writer;

    // Start is called before the first frame update
    void Start() {
        reader = new Workers.VoiceReader(this);
        codec = new Workers.VoiceEncoder();
        writer = new Workers.B2DWriter(Config.Instance.PCs[0].AudioBin2Dash);

        reader.AddNext(codec).AddNext(writer).AddNext(reader);
        reader.token = new Workers.Token(1);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        if (reader != null) reader.Stop();
        if (codec != null) codec.Stop();
        if (writer != null) writer.Stop();
    }
}
