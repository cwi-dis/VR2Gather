using UnityEngine;
using OrchestratorWrapping;

public class AudioReceiver : MonoBehaviour
{
    public string userID;

    public void StartListeningAudio(string pUserID)
    {
        #if TEST_BED
        StartListening(pUserID);
        #endif
    }

    public void StopListeningAudio()
    {
        #if TEST_BED
        StopListening();
        #endif
    }

    #if TEST_BED

    private Workers.BaseWorker reader;
    private Workers.BaseWorker codec;
    private Workers.AudioPreparer preparer;
    private Workers.Token token;

    private float[] tmpBuffer;
    private AudioSource audioSource;

    private ISocketReader socketIOreader;

    private void StartListening(string pUserID)
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.clip = AudioClip.Create("clip_" + pUserID, 320, 1, 16000, false);
        audioSource.loop = true;
        audioSource.Play();

        OrchestratorWrapper.instance.RegisterForDataStream(pUserID, "AUDIO");
        OrchestratorWrapper.instance.OnDataStreamReceived += OnAudioPacketReceived;
        //OrchestratorWrapper.instance.OnAudioSent += OnAudioPacketReceived;

        reader = new Workers.SocketIOReader();
        socketIOreader = (ISocketReader)reader;
        codec = new Workers.VoiceDecoder(null,null); // TODO(FPA): Fix new Queue mode.
        preparer = new Workers.AudioPreparer(null); // TODO(FPA): Fix new Queue mode.

        userID = pUserID;
    }

    private void StopListening()
    {
        OrchestratorWrapper.instance.UnregisterFromDataStream(userID, "AUDIO");
        userID = "";

        reader?.Stop();
        codec?.Stop();
        preparer?.Stop();
    }

    private void OnAudioPacketReceived(UserAudioPacket pPacket)
    {
        if(pPacket.userID == userID)
        {
            socketIOreader.OnData(pPacket.audioPacket);
        }
    }

    private void OnAudioPacketReceived(UserDataStreamPacket pPacket)
    {
        if (pPacket.dataStreamUserID == userID)
        {
            socketIOreader.OnData(pPacket.dataStreamPacket);
        }
    }

    // Buffer is filled 2.5 times per second (every 400ms). 
    private void OnAudioRead(float[] data)
    {
        if (preparer == null || !preparer.GetAudioBuffer(data, data.Length))
            System.Array.Clear(data, 0, data.Length);
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (tmpBuffer == null)
        {
            tmpBuffer = new float[data.Length];
        }
        if (preparer != null && preparer.GetAudioBuffer(tmpBuffer, tmpBuffer.Length))
        {
            int cnt = 0;
            do { data[cnt] += tmpBuffer[cnt]; } while (++cnt < data.Length);
        }
    }

    private void OnDestroy()
    {
        StopListeningAudio();
    }

#endif
}