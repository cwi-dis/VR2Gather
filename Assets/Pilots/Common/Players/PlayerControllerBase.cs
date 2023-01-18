using UnityEngine;
using System.Reflection;
using TMPro;
using VRT.Core;
using System;
using VRT.UserRepresentation.Voice;

namespace VRT.Pilots.Common
{

    abstract public class PlayerControllerBase : MonoBehaviour
    {
        [Tooltip("Main camera, if this is the local player and not using a holodisplay")]
        [SerializeField] protected Camera cam;
        [Tooltip("Main camera if this is the local user and we are using a holo display")]
        [SerializeField] protected GameObject holoCamera;
        [Tooltip("CameraOffset of camera")]
        [SerializeField] protected Transform cameraOffset;
        [Tooltip("Avatar representation of this user")]
        [SerializeField] protected GameObject avatar;
        [Tooltip("Video webcam avator representation of this user")]
        [SerializeField] protected GameObject webcam;
        [Tooltip("Point cloud representation of this user")]
        [SerializeField] protected GameObject pointcloud;
        [Tooltip("Audio representation of this user")]
        [SerializeField] protected GameObject voice;
        [Tooltip("User name is filled into this TMPro field")]
        [SerializeField] protected TextMeshProUGUI userNameText; 
        [Tooltip("True if this is the local player (debug/introspection only)")]
        [DisableEditing] [SerializeField] protected bool isLocalPlayer;
        [Tooltip("True if this user has a visual representation")]
        [DisableEditing] [SerializeField] private bool _isVisible;
        public bool isVisible
        {
            get => _isVisible;
            protected set { _isVisible = value; }
        }
        [Tooltip("True if this user has an audio representation")]
        private bool _isAudible;
        public bool isAudible
        {
            get => _isAudible;
            protected set { _isAudible = value; }
        }
        [Tooltip("Human-readable name of this user")]
        private string _userName;
        public string userName
        {
            get => _userName;
            protected set { _userName = value; }
        }
        [Tooltip("Set to true to enable logging of position/orientation, for debugging tiling decisions")]
        [SerializeField] protected bool debugTiling = false;

        virtual public string Name()
        {
            return $"{GetType().Name}";
        }

        public abstract void SetUpPlayerController(bool _isLocalPlayer, VRT.Orchestrator.Wrapping.User user, BaseConfigDistributor[] configDistributors);
        protected void _SetupCommon(VRT.Orchestrator.Wrapping.User user, BaseConfigDistributor[] configDistributors)
        {
            
            userName = user.userName;
            if (userNameText != null)
            {
                userNameText.text = userName;
            }

            
            SetRepresentation(user.userData.userRepresentationType, user, null, configDistributors);


            if (user.userData.userRepresentationType != UserRepresentationType.__NONE__)
            {
                isAudible = true;

                // Audio
                voice.SetActive(true);
                try
                {
                    LoadAudio(user);
                }
                catch (Exception e)
                {
                    Debug.Log($"[SessionPlayersManager] Exception occured when trying to load audio for user {user.userName} - {user.userId}: " + e);
                    Debug.LogError($"Cannot receive audio from participant {user.userName}");
                    throw;
                }
            }
        }

        public void SetRepresentation(UserRepresentationType type, Orchestrator.Wrapping.User user, VRTConfig._User userCfg, BaseConfigDistributor[] configDistributors=null)
        {
            // Delete old pipelines, if any   
            if (webcam.TryGetComponent(out BasePipeline webpipeline))
                Destroy(webpipeline);
            if (pointcloud.TryGetComponent(out BasePipeline pcpipeline))
                Destroy(pcpipeline);
            // Disable all representations
            webcam.SetActive(false);
            this.pointcloud.SetActive(false);
            avatar.SetActive(false);
            // Enable and initialize the correct representation
            switch (user.userData.userRepresentationType)
            {
                case UserRepresentationType.__2D__:
                    isVisible = true;
                    webcam.SetActive(true);
                    if (userCfg == null)
                    {
                        userCfg = isLocalPlayer ? VRTConfig.Instance.LocalUser : null;
                    }
                    BasePipeline wcPipeline = BasePipeline.AddPipelineComponent(webcam, user.userData.userRepresentationType, isLocalPlayer);
                    wcPipeline?.Init(isLocalPlayer, user, userCfg);
                    break;
                case UserRepresentationType.__AVATAR__:
                    isVisible = true;
                    avatar.SetActive(true);
                    break;
                case UserRepresentationType.__PCC_SYNTH__:
                case UserRepresentationType.__PCC_PRERECORDED__:
                case UserRepresentationType.__PCC_CWIK4A_:
                case UserRepresentationType.__PCC_PROXY__:
                case UserRepresentationType.__PCC_CWI_: // PC
                    isVisible = true;
                    this.pointcloud.SetActive(true);
           
                    userCfg = isLocalPlayer ? VRTConfig.Instance.LocalUser : null;
                    BasePipeline pcPipeline = BasePipeline.AddPipelineComponent(this.pointcloud, user.userData.userRepresentationType, isLocalPlayer);
                    pcPipeline?.Init(isLocalPlayer, user, userCfg);
                    if (configDistributors != null)
                    {
                        if (configDistributors.Length == 0)
                        {
                            Debug.LogError("Programmer Error: No tilingConfigDistributor, you may not be able to see other participants");
                        }
                        // Register for distribution of tiling and sync configurations
                        foreach (var cd in configDistributors)
                        {
                            cd?.RegisterPipeline(user.userId, pcPipeline);
                        }
                    }

                    break;
                default:
                    isVisible = false;
                    break;
            }
        }

        public void LoadAudio(VRT.Orchestrator.Wrapping.User user)
        {
            if (user.userData.microphoneName == "None")
            {
                Debug.LogWarning($"SessionPlayersManager: user {user.userId} has no microphone, skipping audio.");
                return;
            }
            if (isLocalPlayer)
            { // Sender
                var AudioBin2Dash = VRTConfig.Instance.LocalUser.PCSelfConfig.AudioBin2Dash;
                if (AudioBin2Dash == null)
                    throw new Exception("PointCloudPipeline: missing self-user PCSelfConfig.AudioBin2Dash config");
                try
                {
                    voice.AddComponent<VoiceSender>().Init(user, "audio", AudioBin2Dash.segmentSize, AudioBin2Dash.segmentLife, VRTConfig.Instance.protocolType); //Audio Pipeline
                }
                catch (EntryPointNotFoundException e)
                {
                    Debug.Log("PointCloudPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                    throw new Exception("PointCloudPipeline: VoiceDashSender.Init() raised EntryPointNotFound exception, skipping voice encoding\n" + e);
                }
            }
            else
            { // Receiver
                const int audioStreamNumber = 0;
                voice.AddComponent<VoiceReceiver>().Init(user, "audio", audioStreamNumber, VRTConfig.Instance.protocolType); //Audio Pipeline
            }
        }
    

    

      

    
    }
}