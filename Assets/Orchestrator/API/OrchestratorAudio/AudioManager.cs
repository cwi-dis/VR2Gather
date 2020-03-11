using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OrchestratorWrapping;

public class AudioManager : MonoBehaviour
{
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
        OrchestratorController.Instance.OnSessionJoinedEvent += StartRecordAudio;
        OrchestratorController.Instance.OnLeaveSessionEvent += StopRecordAudio;
        OrchestratorController.Instance.OnUserJoinSessionEvent += StartListeningAudio;
        OrchestratorController.Instance.OnUserLeaveSessionEvent += StopListeningAudio;
    }

    #endregion

    #region Record Audio

    private void StartRecordAudio()
    {
        recorder.StartRecordAudio();
    }

    private void StopRecordAudio()
    {
        recorder.StopRecordAudio();
        StopListeningAudio();
    }

    #endregion

    #region Listen Audio

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

    #endregion
}