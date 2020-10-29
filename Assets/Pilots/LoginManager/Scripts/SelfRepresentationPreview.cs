using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OrchestratorWrapping;
using System;

public class SelfRepresentationPreview : MonoBehaviour{
    public static SelfRepresentationPreview Instance { get; private set; }
    public float MicrophoneLevel { get; private set; }

    public PlayerManager player;
    string currentMicrophoneName = "None";
    AudioClip recorder;
    float[] buffer = new float[320 * 3];
    int readPosition = 0;
    int samples = 16000;

    // Start is called before the first frame update
    void Start() {
        if (Instance == null) {
            Instance = this;
        }
    }

    void Update() {
        if (currentMicrophoneName != "None") {
            int writePosition = Microphone.GetPosition(currentMicrophoneName);
            int available;
            if (writePosition < readPosition) available = (samples - readPosition) + writePosition;
            else available = writePosition - readPosition;

            if (available >= buffer.Length) {
                float total = 0;
                if (recorder.GetData(buffer, readPosition)) {
                    readPosition = (readPosition + buffer.Length) % samples;
                    for (int i = 0; i < buffer.Length; ++i)
                        total += Mathf.Abs(buffer[i]*4);
                }
                MicrophoneLevel = total / (float)buffer.Length;
            }
        }
    }

    public void Stop() {
        player.avatar.SetActive(false);
        if (player.webcam.TryGetComponent(out WebCamPipeline web))
            Destroy(web);
        player.webcam.SetActive(false);
        if (player.pc.TryGetComponent(out EntityPipeline pointcloud))
            Destroy(pointcloud);
        if (player.pc.TryGetComponent(out Workers.PointBufferRenderer renderer))
            Destroy(renderer);
        player.pc.SetActive(false);
        player.tvm.gameObject.SetActive(false);
    }

    public void ChangeMicrophone(string microphoneName) {
        StopMicrophone();
        currentMicrophoneName = microphoneName;
        if (currentMicrophoneName != "None") {
            Workers.VoiceReader.PrepareDSP();
            recorder = Microphone.Start(currentMicrophoneName, true, 1, samples);
            readPosition = 0;
        }
    }

    public void StopMicrophone() {
        if (currentMicrophoneName != "None") {
            Microphone.End(currentMicrophoneName);
            currentMicrophoneName = "None";
        }
    }


    public void ChangeRepresentation(UserData.eUserRepresentationType representation, string webcamName) {
        if (OrchestratorController.Instance == null || OrchestratorController.Instance.SelfUser == null) return;
        player.userName.text = OrchestratorController.Instance.SelfUser.userName;
        player.gameObject.SetActive(true);
        Stop();

        switch (representation) {
            case UserData.eUserRepresentationType.__NONE__:
                player.gameObject.SetActive(false);
                break;
            case UserData.eUserRepresentationType.__2D__:
                player.webcam.SetActive(true);
                if (webcamName != "None") {
                    WebCamPipeline wcPipeline = player.webcam.AddComponent<WebCamPipeline>();
                    wcPipeline.Init(new User() { userData = new UserData() { webcamName = webcamName, microphoneName = "None" } }, Config.Instance.LocalUser, false, true);
                }

                break;
            case UserData.eUserRepresentationType.__AVATAR__:
                player.avatar.SetActive(true);
                break;
            case UserData.eUserRepresentationType.__TVM__:
                //player.tvm.gameObject.SetActive(true);
                Debug.Log("TVM PREVIEW");
                break;
            case UserData.eUserRepresentationType.__PCC_CWI_:
            case UserData.eUserRepresentationType.__PCC_CWIK4A_:
            case UserData.eUserRepresentationType.__PCC_PROXY__:
            case UserData.eUserRepresentationType.__PCC_SYNTH__:
            case UserData.eUserRepresentationType.__PCC_CERTH__:
                player.pc.SetActive(true);
                player.pc.AddComponent<EntityPipeline>().Init(new User() { userData = new UserData() { userRepresentationType = representation } }, Config.Instance.LocalUser, true);
                break;
            case UserData.eUserRepresentationType.__SPECTATOR__:
                player.gameObject.SetActive(false);
                break;
            default:
                break;
        }
    }
}
