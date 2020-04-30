using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OrchestratorWrapping;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                if (GameObject.Find("AudioManager") != null) {
                    instance = GameObject.Find("AudioManager").GetComponent<AudioManager>();
                }
                else {
                    instance = new GameObject("AudioManager").AddComponent<AudioManager>();
                }
            }
            return instance;
        }
    }

    private static AudioManager instance;
    private AudioRecorder recorder;
    private List<AudioReceiver> receivers = new List<AudioReceiver>();

    #region Unity

    private void Awake()
    {
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

        SubscribeToOrchestratorEvents();

        if(recorder == null)
        {
            recorder = gameObject.AddComponent<AudioRecorder>();
        }
    }

    #endregion

    #region Orchestrator Listeners

    private void SubscribeToOrchestratorEvents()
    {
        OrchestratorController.Instance.OnSessionJoinedEvent += StartRecord;
        OrchestratorController.Instance.OnLeaveSessionEvent += StopRecord;
        OrchestratorController.Instance.OnUserJoinSessionEvent += StartListening;
        OrchestratorController.Instance.OnUserLeaveSessionEvent += StopListening;
    }

    #endregion

    #region Public

    public void StartRecordAudio()
    {
        StartRecord();
    }

    public void StopRecordAudio()
    {
        StopRecord();
    }

    public void StartListeningAudio2(string pUserID)
    {
        StartListening(pUserID);
    }

    public void StopListeningAudio2(string pUserID)
    {
        StopListening(pUserID);
    }

    #endregion

    #region Record Audio

    private void StartRecord()
    {
        recorder.StartRecordAudio();        
    }

    private void StopRecord()
    {
        recorder.StopRecordAudio();
        StopListening();
    }

    #endregion

    #region Listen Audio

    private void StartListening(string pUserID)
    {
        InstantiateAudioListener(pUserID);
    }

    private void StopListening(string pUserID = "")
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