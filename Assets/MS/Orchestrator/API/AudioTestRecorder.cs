using UnityEngine;

public class AudioTestRecorder : MonoBehaviour
{
    private Workers.BaseWorker reader;
    private Workers.BaseWorker codec;
    private Workers.BaseWorker writer;

    private Config._User._PCSelfConfig._Bin2Dash cfg;

    private bool toggle = false;

    void Start()
    {
        cfg = new Config._User._PCSelfConfig._Bin2Dash();

        cfg.streamName = "audio";
        cfg.url = "";
        cfg.segmentSize = 1000;
        cfg.segmentLife = 30000;
        cfg.fileMirroring = true;
    }

    private void OnDestroy()
    {
        StopRecordAudio();
    }

    public void StartRecordAudio()
    {
        codec = new Workers.VoiceEncoder();
        reader = new Workers.VoiceReader(this, ((Workers.VoiceEncoder)codec).bufferSize);

        writer = new Workers.SocketIOWriter(null, 0);
        reader.AddNext(codec).AddNext(writer).AddNext(reader);
        reader.token = new Workers.Token(1);
    }

    public void StopRecordAudio()
    {
        reader?.Stop();
        codec?.Stop();
        writer?.Stop();
    }
}