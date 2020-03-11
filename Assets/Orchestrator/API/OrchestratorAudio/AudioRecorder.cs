using UnityEngine;
using OrchestratorWrapping;

public class AudioRecorder : MonoBehaviour
{
    public void StartRecordAudio()
    {
        #if TEST_BED
        StartRecord();
        #endif
    }

    public void StopRecordAudio()
    {
        #if TEST_BED
        StopRecord();
        #endif
    }

    #if TEST_BED

    private Workers.BaseWorker reader;
    private Workers.BaseWorker codec;
    private Workers.BaseWorker writer;

    private void StartRecord()
    {
        OrchestratorWrapper.instance.DeclareDataStream("AUDIO");

        codec = new Workers.VoiceEncoder();
        reader = new Workers.VoiceReader(this, ((Workers.VoiceEncoder)codec).bufferSize);

        writer = new Workers.SocketIOWriter(SendAudioPacket);

        reader.AddNext(codec).AddNext(writer).AddNext(reader);
        reader.token = new Workers.Token(1);
    }

    private void StopRecord()
    {
        OrchestratorWrapper.instance.RemoveDataStream("AUDIO");

        reader?.Stop();
        codec?.Stop();
        writer?.Stop();
    }

    private void SendAudioPacket(byte[] pPacket)
    {
        //OrchestratorWrapper.instance.PushAudioPacket(pPacket);
        OrchestratorWrapper.instance.SendData("AUDIO", pPacket);
    }
    private void OnDestroy()
    {
        StopRecordAudio();
    }

#endif
}