﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using VRT.Core;
using Cwipc;
using VRT.Orchestrator.Wrapping;
using VRT.UserRepresentation.WebCam;
using VRT.UserRepresentation.Voice;
using VRT.UserRepresentation.PointCloud;
using VRT.Pilots.Common;

using PointCloudRenderer = Cwipc.PointCloudRenderer;
namespace VRT.Pilots.LoginManager
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

        void Update()
        {
            // See if we can already initialize player self representation
            if (!playerHasBeenInitialized)
            {
                User user = OrchestratorController.Instance.SelfUser;
                if (user != null)
                {
                    UserData userData = user.userData;
                    if (userData != null)
                    {
                        ChangeRepresentation(userData.userRepresentationType, userData.webcamName);
                    }
                }
            }
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
                AsyncVoiceReader.PrepareDSP(VRTConfig.Instance.audioSampleRate, 0);
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
            if (!playerHasBeenInitialized)
            {
                tmpSelfUser.userName = OrchestratorController.Instance.SelfUser.userName;
                player.SetUpPlayerController(true, tmpSelfUser);
                //player.setupInputOutput(true); // xxxjack needed for preview?
                playerHasBeenInitialized = true;
            }
            player.SetRepresentation(representation, permanent: true);
        }
    }
}