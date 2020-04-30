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

    QueueThreadSafe encoderQueue = new QueueThreadSafe();
    QueueThreadSafe senderQueue = new QueueThreadSafe();


    private void StartRecord()
    {
        OrchestratorWrapper.instance.DeclareDataStream("AUDIO");

        codec = new Workers.VoiceEncoder(encoderQueue, senderQueue);
        reader = new Workers.VoiceReader(this, ((Workers.VoiceEncoder)codec).bufferSize, encoderQueue);

        writer = new Workers.SocketIOWriter(SendAudioPacket); // TODO(FPA): Fix new Queue mode.
    }

    private void StopRecord() {
        OrchestratorWrapper.instance.RemoveDataStream("AUDIO");

        reader?.StopAndWait();
        codec?.StopAndWait();
        writer?.StopAndWait();
    }

    private void SendAudioPacket(byte[] pPacket) {
        //OrchestratorWrapper.instance.PushAudioPacket(pPacket);
        OrchestratorWrapper.instance.SendData("AUDIO", pPacket);
    }
    private void OnDestroy()
    {
        StopRecordAudio();
    }

#endif
}