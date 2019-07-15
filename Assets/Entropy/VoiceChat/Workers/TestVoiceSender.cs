using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestVoiceSender : MonoBehaviour
{

    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.BaseWorker writer;

    public SocketIOConnection socketIOConnection;

    // Start is called before the first frame update
    IEnumerator Start() {
        if (socketIOConnection != null)  yield return socketIOConnection.WaitConnection();
        reader = new Workers.VoiceReader(this);
        codec = new Workers.VoiceEncoder();

        if (socketIOConnection == null) writer = new Workers.B2DWriter(Config.Instance.PCs[0].AudioBin2Dash);
        else writer = new Workers.SocketIOWriter(socketIOConnection.socket);

        reader.AddNext(codec).AddNext(writer).AddNext(reader);
        reader.token = new Workers.Token(1);
    }

    void OnDestroy() {
        reader?.Stop();
        codec?.Stop();
        writer?.Stop();
    }
}
