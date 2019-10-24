using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OrchestratorWrapping;

public class AudioManager : MonoBehaviour, IUserSessionEventsListener
{
    public static AudioManager instance;

    private AudioRecorder recorder;
    private List<AudioReceiver> receivers = new List<AudioReceiver>();

    #region Unity

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
        while (OrchestratorWrapper.instance == null)
        {
            yield return 0;
        }

        OrchestratorWrapper.instance.AddUserSessionEventLister(this);

        if(recorder == null)
        {
            recorder = gameObject.AddComponent<AudioRecorder>();
        }

        DontDestroyOnLoad(this);
    }

    #endregion

    #region Orchestrator Listeners

    public void OnUserJoinedSession(string userID)
    {
        StartListeningAudio(userID);
    }

    public void OnUserLeftSession(string userID)
    {
        StopListeningAudio();
    }

    #endregion

    #region Record Audio

    public void StartRecordAudio()
    {
        recorder.StartRecordAudio();
    }

    public void StopRecordAudio()
    {
        recorder.StopRecordAudio();
        StopListeningAudio();
    }

    #endregion

    #region Listen Audio

    public void StartListeningAudio(string pUserID)
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

    #endregion
}