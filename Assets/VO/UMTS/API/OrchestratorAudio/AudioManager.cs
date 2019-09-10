using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    private AudioRecorder recorder;
    private List<AudioReceiver> receivers = new List<AudioReceiver>();

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }

        AudioConfiguration ac = AudioSettings.GetConfiguration();
        ac.sampleRate = 16000 * 3;
        ac.dspBufferSize = 320 * 3;
        AudioSettings.Reset(ac);
    }

    private IEnumerator Start()
    {
        while (OrchestratorGui.orchestratorWrapper == null)
        {
            yield return 0;
        }
        OrchestratorGui.orchestratorWrapper.OnAudioSentStart.AddListener(StartListeningAudio);
        OrchestratorGui.orchestratorWrapper.OnAudioSentStop.AddListener(StopListeningAudio);

        if(recorder == null)
        {
            recorder = gameObject.AddComponent<AudioRecorder>();
        }
    }

    public void StartRecordAudio()
    {
        recorder.StartRecordAudio();
    }

    public void StopRecordAudio()
    {
        recorder.StopRecordAudio();
        StopListeningAudio();
    }

    private void StartListeningAudio(string pUserID)
    {
        InstantiateAudioListener(pUserID);
    }

    private void StopListeningAudio(string pUserID = "")
    {
        for(int i=0; i<receivers.Count; i++)
        {
            if (string.IsNullOrEmpty(pUserID) || receivers[i].userID == pUserID)
            {
                if (receivers[i] != null)
                {
                    Destroy(receivers[i].gameObject);
                }

                receivers.Remove(receivers[i]);
            }
        }
    }

    private void InstantiateAudioListener(string pUserID)
    {
        GameObject lUserAudioReceiver = new GameObject("UserAudioReceiver_" + pUserID);
        lUserAudioReceiver.transform.parent = this.transform;

        AudioReceiver lAudioReceiver = lUserAudioReceiver.AddComponent<AudioReceiver>();
        lAudioReceiver.StartListeningAudio(pUserID);

        receivers.Add(lAudioReceiver);
    }
}