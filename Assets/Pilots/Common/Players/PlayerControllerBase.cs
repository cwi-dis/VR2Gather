using UnityEngine;
using System.Reflection;
using VRT.Core;
using System;
using VRT.UserRepresentation.Voice;

namespace VRT.Pilots.Common
{

    public class PlayerControllerBase : MonoBehaviour
    {
        public int id;
        public string orchestratorId;
        public UserRepresentationType userRepresentationType;
        public TMPro.TextMeshProUGUI userName;
        public Camera cam;
        public GameObject holoCamera;
        public GameObject avatar;
        public GameObject webcam;
        public GameObject pc;
        public GameObject voice;
        public GameObject[] localPlayerOnlyObjects;

        //Looks like this could very well be internal to the PlayerManager? 
        public void SetUpPlayerController(bool isLocalPlayer, VRT.Orchestrator.Wrapping.User user, BaseConfigDistributor[] configDistributors)
        {

            orchestratorId = user.userId;
            userName.text = user.userName;

            setupInputOutput(isLocalPlayer);
            Transform cameraTransform = null;
            if (isLocalPlayer)
            {
                cameraTransform = getCameraTransform();
            }


            if (user.userData.userRepresentationType != UserRepresentationType.__NONE__)
            {
                switch (user.userData.userRepresentationType)
                {
                    case UserRepresentationType.__2D__:
                        // FER: Implementacion representacion de webcam.
                        webcam.SetActive(true);
                        Config._User userCfg = isLocalPlayer ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
                        BasePipeline wcPipeline = BasePipeline.AddPipelineComponent(webcam, user.userData.userRepresentationType, isLocalPlayer);
                        wcPipeline?.Init(isLocalPlayer, user, userCfg);
                        break;
                    case UserRepresentationType.__AVATAR__:
                        avatar.SetActive(true);
                        break;
                    case UserRepresentationType.__PCC_SYNTH__:
                    case UserRepresentationType.__PCC_PRERECORDED__:
                    case UserRepresentationType.__PCC_CWIK4A_:
                    case UserRepresentationType.__PCC_PROXY__:
                    case UserRepresentationType.__PCC_CWI_: // PC
                        pc.SetActive(true);
                        if (cameraTransform)
                        {
                            Vector3 pos = new Vector3(PlayerPrefs.GetFloat("pcs_pos_x", 0), PlayerPrefs.GetFloat("pcs_pos_y", 0), PlayerPrefs.GetFloat("pcs_pos_z", 0));
                            Vector3 rot = new Vector3(PlayerPrefs.GetFloat("pcs_rot_x", 0), PlayerPrefs.GetFloat("pcs_rot_y", 0), PlayerPrefs.GetFloat("pcs_rot_z", 0));
                            Debug.Log($"SessionPlayersManager: self-camera pos={pos}, rot={rot}");
                            cam.gameObject.transform.parent.localPosition = pos;
                            cam.gameObject.transform.parent.localRotation = Quaternion.Euler(rot);
                        }
                        userCfg = isLocalPlayer ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
                        BasePipeline pcPipeline = BasePipeline.AddPipelineComponent(pc, user.userData.userRepresentationType, isLocalPlayer);
                        pcPipeline?.Init(isLocalPlayer, user, userCfg);
                        if (configDistributors == null || configDistributors.Length == 0)
                        {
                            Debug.LogError("Programmer Error: No tilingConfigDistributor, you may not be able to see other participants");
                        }
                        // Register for distribution of tiling and sync configurations
                        foreach (var cd in configDistributors)
                        {
                            cd?.RegisterPipeline(user.userId, pcPipeline);
                        }

                        break;
                    default:
                        break;


                }

                // Audio
                voice.SetActive(true);
                try
                {
                    LoadAudio(isLocalPlayer, user);
                }
                catch (Exception e)
                {
                    Debug.Log($"[SessionPlayersManager] Exception occured when trying to load audio for user {user.userName} - {user.userId}: " + e);
                    Debug.LogError($"Cannot receive audio from participant {user.userName}");
                    throw;
                }
            }
        }

        public void LoadAudio(bool isLocalPlayer, VRT.Orchestrator.Wrapping.User user)
        {
            if (user.userData.microphoneName == "None")
            {
                Debug.LogWarning($"SessionPlayersManager: user {user.userId} has no microphone, skipping audio.");
                return;
            }
            if (isLocalPlayer)
            { // Sender
                var AudioBin2Dash = Config.Instance.LocalUser.PCSelfConfig.AudioBin2Dash;
                if (AudioBin2Dash == null)
                    throw new Exception("PointCloudPipeline: missing self-user PCSelfConfig.AudioBin2Dash config");
                try
                {
                    voice.AddComponent<VoiceSender>().Init(user, "audio", AudioBin2Dash.segmentSize, AudioBin2Dash.segmentLife, Config.Instance.protocolType); //Audio Pipeline
                }
                catch (EntryPointNotFoundException e)
                {
                    Debug.Log("PointCloudPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                    throw new Exception("PointCloudPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                }
            }
            else
            { // Receiver
                var AudioSUBConfig = Config.Instance.RemoteUser.AudioSUBConfig;
                if (AudioSUBConfig == null)
                    throw new Exception("PointCloudPipeline: missing other-user AudioSUBConfig config");
                voice.AddComponent<VoiceReceiver>().Init(user, "audio", AudioSUBConfig.streamNumber, Config.Instance.protocolType); //Audio Pipeline
            }
        }
        //
        // Enable camera (or camera-like object) and input handling.
        // If not the local player most things will be disabled.
        // If disableInput is true the input handling will be disabled (probably because we are in the calibration
        // scene or some other place where input is handled differently than through the PFB_Player).
        //
        public void setupInputOutput(bool isLocalPlayer, bool disableInput = false)
        {

            // Unity has two types of null. We need the C# null.
            if (holoCamera == null) holoCamera = null;
            // Enable either the normal camera or the holodisplay camera for the local user.
            // Enable various other objects only for the local user
            // xxxjack This currentaly always enables the normal camera and disables the holoCamera.
            // xxxjack to be fixed at some point.
            bool useLocalHoloDisplay = isLocalPlayer && false;
            bool useLocalNormalCam = isLocalPlayer && true;
            if (useLocalNormalCam)
            {
                cam.gameObject.SetActive(true);
                holoCamera?.SetActive(false);
            }
            else if (useLocalHoloDisplay)
            {
                cam.gameObject.SetActive(false);
                holoCamera.SetActive(true);
            }
            else
            {
                cam.gameObject.SetActive(false);
                holoCamera?.SetActive(false);
            }

            // Enable various other objects only for the local user
            foreach (var obj in localPlayerOnlyObjects)
            {
                obj.SetActive(isLocalPlayer);
            }
        }

        public Transform getCameraTransform()
        {
            if (holoCamera != null && holoCamera.activeSelf)
            {
                return holoCamera.transform;
            }
            else
            {
                return cam.transform;
            }
        }
    }
}