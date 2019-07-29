using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioTestWorker : MonoBehaviour {

    public int userID;
    Workers.BaseWorker reader;
    Workers.BaseWorker codec;
    Workers.BaseWorker writer;

    Config._User._PCSelfConfig._Bin2Dash cfg;

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

    private void StartRecordAudioDash()
    {
        new GameObject ("AudioRecorder").AddComponent<VoiceDashSender>().Init(cfg);
    }

    private void StartRecordAudio()
    {
        reader = new Workers.VoiceReader(this);
        codec = new Workers.VoiceEncoder();

        writer = new Workers.SocketIOWriter(null, userID);
        reader.AddNext(codec).AddNext(writer).AddNext(reader);
        reader.token = new Workers.Token(1);
    }

    private void StopRecordAudio()
    {
        reader?.Stop();
        codec?.Stop();
        writer?.Stop();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if (!toggle)
            {
                StartRecordAudio();
            }
            else
                StopRecordAudio();

            toggle = !toggle;
        }
    }
}