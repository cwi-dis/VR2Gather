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
        [Tooltip("Network controller for this player (default: on same GameObject)")]
        PlayerNetworkControllerBase playerNetworkController = null;
        [Tooltip("Main camera, if this is the local player and not using a holodisplay")]
        [SerializeField] protected Camera cam;
        [Tooltip("Main camera if this is the local user and we are using a holo display")]
        [SerializeField] protected GameObject holoCamera;
        [Tooltip("CameraOffset of camera")]
        [SerializeField] protected Transform cameraOffset;
        [Tooltip("Current visual representation of this user")]
        public UserRepresentationType userRepresentation;
        [Tooltip("Avatar representation of this user")]
        [SerializeField] protected GameObject avatar;
        [Tooltip("Video webcam avatar representation of this user")]
        [SerializeField] protected GameObject webcam;
        [Tooltip("Point cloud representation of this user")]
        [SerializeField] protected GameObject pointcloud;
        [Tooltip("Experience-defined representation of this user")]
        [SerializeField] protected GameObject altRepOne;
        [Tooltip("Experience-defined representation of this user")]
        [SerializeField] protected GameObject altRepTwo;
        [Tooltip("Audio representation of this user")]
        [SerializeField] protected GameObject voice;
        [Tooltip("Charachter controller, will be disabled for no-representation users")]
        [SerializeField] protected CharacterController charControl;
        [Tooltip("User name is filled into this TMPro field")]
        [SerializeField] protected TextMeshProUGUI userNameText; 
        [Tooltip("True if this is the local player (debug/introspection only)")]
        [DisableEditing] [SerializeField] protected bool isLocalPlayer;
        [Tooltip("True if this user has a visual representation")]
        [DisableEditing] [SerializeField] private bool _isVisible;
        [Tooltip("Orchestrator User structure for this player")]
        [DisableEditing][SerializeField] protected VRT.Orchestrator.Wrapping.User user;
        protected bool isInitialized = false;

        // May be set by subclasses to indicate this player should not transmit
        // any data streams.
        protected bool isPreviewPlayer = false;

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

        protected virtual void Update()
        {
        }

        public abstract void SetUpPlayerController(bool _isLocalPlayer, VRT.Orchestrator.Wrapping.User user);
        protected void _SetupCommon(VRT.Orchestrator.Wrapping.User _user)
        {
            if (playerNetworkController == null)
            {
                playerNetworkController = GetComponent<PlayerNetworkControllerBase>();
            }
            user = _user;
            userName = user.userName;
            if (userNameText != null)
            {
                userNameText.text = userName;
            }
            playerNetworkController.SetupPlayerNetworkController(this, isLocalPlayer, user.userId);
            
            SetRepresentation(user.userData.userRepresentationType);

            // xxxjack Don't like this special case here: it means that everyone except 
            // NoRepresentation has audio (including the caermaman)
            if (user.userData.userRepresentationType != UserRepresentationType.NoRepresentation)
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

        public virtual void SetRepresentation(UserRepresentationType type, bool onlyIfVisible = false, bool permanent = false)
        {
            if (isInitialized && type == userRepresentation) return;
            if (isInitialized && onlyIfVisible && !isVisible) return;
            isInitialized = true;
            userRepresentation = type;
            if (permanent)
            {
                user.userData.userRepresentationType = type;
            }
            // Delete old pipelines, if any   
            if (webcam.TryGetComponent(out BasePipeline webpipeline))
                Destroy(webpipeline);
            if (pointcloud.TryGetComponent(out BasePipeline pcpipeline))
                Destroy(pcpipeline);
            // Disable all representations
            webcam.SetActive(false);
            this.pointcloud.SetActive(false);
            avatar.SetActive(false);
            if (altRepOne != null) altRepOne.SetActive(false);
            if (altRepTwo != null) altRepTwo.SetActive(false);
            // Enable and initialize the correct representation
            VRTConfig._User userCfg = isLocalPlayer ? VRTConfig.Instance.LocalUser : null;
            if (charControl != null) charControl.enabled = true;
            switch (userRepresentation)
            {
                case UserRepresentationType.NoRepresentation:
                    // disable character controller.
                    if (charControl != null)
                    {
                        charControl.enabled = false;
                    }
                    break;
                case UserRepresentationType.VideoAvatar:
                    isVisible = true;
                    webcam.SetActive(true);
                    BasePipeline wcPipeline = BasePipeline.AddPipelineComponent(webcam, userRepresentation, isLocalPlayer);
                    wcPipeline?.Init(isLocalPlayer, user, userCfg, isPreviewPlayer);
                    break;
                case UserRepresentationType.SimpleAvatar:
                    isVisible = true;
                    avatar.SetActive(true);
                    break;
                case UserRepresentationType.PointCloud: // PC
                    isVisible = true;
                    this.pointcloud.SetActive(true);
           
                    BasePipeline pcPipeline = BasePipeline.AddPipelineComponent(this.pointcloud, userRepresentation, isLocalPlayer);
                    try
                    {
                        pcPipeline?.Init(isLocalPlayer, user, userCfg, isPreviewPlayer);
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Cannot set representation {userRepresentation}. Exception {e}");
                        Debug.LogError($"Cannot set representation {userRepresentation}. Revert to avatar.");
                        userRepresentation = UserRepresentationType.SimpleAvatar;
                        avatar.SetActive(true);
                        this.pointcloud.SetActive(false);
                        Destroy(pcPipeline);
                        throw;
                    }
                    break;
                case UserRepresentationType.AppDefinedRepresentationOne:
                    altRepOne.SetActive(true);
                    isVisible = true;
                    break;
                case UserRepresentationType.AppDefinedRepresentationTwo:
                    altRepTwo.SetActive(true);
                    isVisible = true;
                    break;
                default:
                    Debug.LogError($"{Name()}: Unknown UserRepresentationType {userRepresentation}");
                    isVisible = false;
                    break;
            }
        }

        public GameObject GetRepresentationGameObject()
        {
            switch (userRepresentation)
            {
                case UserRepresentationType.NoRepresentation:
                    return null;
                case UserRepresentationType.VideoAvatar:
                    return webcam;
                case UserRepresentationType.SimpleAvatar:
                    return avatar;
                case UserRepresentationType.PointCloud: // PC
                    return pointcloud;
                case UserRepresentationType.AppDefinedRepresentationOne:
                    return altRepOne;
                case UserRepresentationType.AppDefinedRepresentationTwo:
                    return altRepTwo;
                default:
                    Debug.LogError($"{Name()}: Unknown UserRepresentationType {userRepresentation}");
                    return null;
            }
        }

        public void LoadAudio(VRT.Orchestrator.Wrapping.User user)
        {
            if (isPreviewPlayer) return;
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
                    voice.AddComponent<VoiceSender>().Init(user, "audio", AudioBin2Dash.segmentSize, AudioBin2Dash.segmentLife); //Audio Pipeline
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
                voice.AddComponent<VoiceReceiver>().Init(user, "audio", audioStreamNumber); //Audio Pipeline
            }
        }

        public void EnableAudio()
        {
            this.voice.SetActive(true);
        }

        public void DisableAudio()
        {
            this.voice.SetActive(false);
        }

        public void EnablePointCloud()
        {
            this.pointcloud.SetActive(true);
        }

        public void DisablePointCloud()
        {
            this.pointcloud.SetActive(false);
        }

        public void EnableHands()
        {
            // xxxNacho enable hands
        }

        public void DisableHands()
        {
            // xxxNacho disable hands
        }





    }
}