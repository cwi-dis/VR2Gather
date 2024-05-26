using UnityEngine;
using VRT.Core;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.Voice;
using VRT.Pilots.Common;

namespace VRT.Login
{

    public class SelfRepresentationPreview : MonoBehaviour
    {
        public float MicrophoneLevel { get; private set; }

        [Tooltip("Player used for this preview (capture and display only)")]
        public PlayerControllerSelf player;
        bool playerHasBeenInitialized = false;
        string currentMicrophoneName = "None";
        AudioClip recorder;
        float[] buffer = new float[320 * 3];
        int readPosition = 0;
        int samples = 16000;

        // Start is called before the first frame update
        void Start()
        {
        }

        public void InitializeSelfPlayer()
        {
            User user = OrchestratorController.Instance.SelfUser;
            if (OrchestratorController.Instance?.SelfUser?.userData != null)
            {
                // ChangeRepresentation(userData.userRepresentationType, userData.webcamName);
                UpdateSelfPlayer(null);
            }
        }

        void UpdateSelfPlayer(User user)
        {
            if (user == null)
            {
                user = OrchestratorController.Instance.SelfUser;
            }
            else
            {
                user.userName = OrchestratorController.Instance.SelfUser.userName;
            }
            if (!playerHasBeenInitialized)
            {
                // We set HasBeenInitialized early, because SetupPlayerController may raise an exception
                // and revert the representation to avatar if there are problems with the chosen representation
                // (for example no cameras found).
                // This way we don't get into an error message loop.
                playerHasBeenInitialized = true;
                player.SetUpPlayerController(true, user);
            }
            player.SetRepresentation(user.userData.userRepresentationType, permanent: true);
        }    

        void Update()
        {
            // See if we can already initialize player self representation
            if (!playerHasBeenInitialized)
            {
                InitializeSelfPlayer();
            }
            UpdateMicrophoneLevel();
        }

        void UpdateMicrophoneLevel()
        { 
            // See if we need to listen to audio for VU-meter.
            if (currentMicrophoneName != "None")
            {
                int writePosition = Microphone.GetPosition(currentMicrophoneName);
                int available;
                if (writePosition < readPosition) available = (samples - readPosition) + writePosition;
                else available = writePosition - readPosition;

                if (available >= buffer.Length)
                {
                    float total = 0;
                    if (recorder.GetData(buffer, readPosition))
                    {
                        readPosition = (readPosition + buffer.Length) % samples;
                        for (int i = 0; i < buffer.Length; ++i)
                            total += Mathf.Abs(buffer[i] * 4);
                    }
                    MicrophoneLevel = total / (float)buffer.Length;
                }
            }
        }

        public void ChangeMicrophone(string microphoneName)
        {
            StopMicrophone();
            currentMicrophoneName = microphoneName;
            if (currentMicrophoneName != "None")
            {
                VoiceDspController.PrepareDSP(VRTConfig.Instance.audioSampleRate, 0);
                recorder = Microphone.Start(currentMicrophoneName, true, 1, samples);
                readPosition = 0;
            }
        }

        public void StopMicrophone()
        {
            if (currentMicrophoneName != "None")
            {
                Microphone.End(currentMicrophoneName);
                currentMicrophoneName = "None";
            }
        }


        public void ChangeRepresentation(UserRepresentationType representation, string webcamName)
        {
            Debug.Log($"SelfRepresentationPreview: representation={representation}, webCamName={webcamName}");
            if (OrchestratorController.Instance == null || OrchestratorController.Instance.SelfUser == null) return;
            User tmpSelfUser = new User()
            {
                userData = new UserData()
                {
                    microphoneName = "None",
                    webcamName = webcamName,
                    userRepresentationType = representation
                }
            };
            UpdateSelfPlayer(tmpSelfUser);
        }
    }
}