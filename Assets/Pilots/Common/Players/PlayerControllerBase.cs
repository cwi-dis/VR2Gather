using UnityEngine;
using System.Reflection;
using VRT.Core;
using System;
using VRT.UserRepresentation.Voice;

namespace VRT.Pilots.Common
{

    public class PlayerControllerBase : MonoBehaviour
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
        [Tooltip("True if this is the local player (debug/introspection only)")]
        [SerializeField] protected bool isLocalPlayer;
        [Tooltip("True if this user has a visual representation")]
        private bool _isVisible;
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

        public void SetUpPlayerController(bool _isLocalPlayer, VRT.Orchestrator.Wrapping.User user, BaseConfigDistributor[] configDistributors)
        {
            isLocalPlayer = _isLocalPlayer;

            userName = user.userName;

            setupCamera();
          
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
            if (isLocalPlayer)
            {
                LoadCameraTransform();
            }
        }

        public void SetRepresentation(UserRepresentationType type, Orchestrator.Wrapping.User user, Config._User userCfg, BaseConfigDistributor[] configDistributors=null)
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
                        userCfg = isLocalPlayer ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
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
           
                    userCfg = isLocalPlayer ? Config.Instance.LocalUser : Config.Instance.RemoteUser;
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

        public void LoadCameraTransform()
        {
            if (!isLocalPlayer)
            {
                Debug.LogError($"{Name()}: LoadCameraTransform called for non-local player");
            }
            if (cameraOffset == null)
            {
                Debug.LogError($"{Name()}: No cameraOffset");
            }
            Vector3 pos = new Vector3(PlayerPrefs.GetFloat("cam_pos_x", 0), PlayerPrefs.GetFloat("cam_pos_y", 0), PlayerPrefs.GetFloat("cam_pos_z", 0));
            Vector3 rot = new Vector3(PlayerPrefs.GetFloat("cam_rot_x", 0), PlayerPrefs.GetFloat("cam_rot_y", 0), PlayerPrefs.GetFloat("cam_rot_z", 0));
            Debug.Log($"{Name()}: loaded self-camera pos={pos}, rot={rot}");
            cameraOffset.localPosition = pos;
            cameraOffset.localRotation = Quaternion.Euler(rot);
        }

        public void SaveCameraTransform()
        {
            if (!isLocalPlayer)
            {
                Debug.LogError($"{Name()}: LoadCameraTransform called for non-local player");
            }
            if (cameraOffset == null)
            {
                Debug.LogError($"{Name()}: No cameraOffset");
            }
            Vector3 pos = cameraOffset.localPosition;
            Vector3 rot = cameraOffset.localRotation.eulerAngles;
            Debug.Log($"{Name()}: Saving self-camera pos={pos}, rot={rot}");
            PlayerPrefs.SetFloat("cam_pos_x", pos.x);
            PlayerPrefs.SetFloat("cam_pos_y", pos.y);
            PlayerPrefs.SetFloat("cam_pos_z", pos.z);
            PlayerPrefs.SetFloat("cam_rot_x", rot.x);
            PlayerPrefs.SetFloat("cam_rot_y", rot.y);
            PlayerPrefs.SetFloat("cam_rot_z", rot.z);
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
        public void setupCamera(bool disableInput = false)
        {
            if (!isLocalPlayer) return;
            // Unity has two types of null. We need the C# null.
            if (holoCamera == null) holoCamera = null;
            // Enable either the normal camera or the holodisplay camera for the local user.
            bool useLocalHoloDisplay = false;
            bool useLocalNormalCam = !disableInput;
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
                cam?.gameObject.SetActive(false);
                holoCamera?.SetActive(false);
            }
        }

        public Transform getCameraTransform()
        {
            if (!isLocalPlayer)
            {
                Debug.LogError($"Programmer error: {Name()}: GetCameraTransform called but isLocalPlayer is false");
                return null;
            }
            if (holoCamera != null && holoCamera.activeSelf)
            {
                return holoCamera.transform;
            }
            else
            {
                return cam.transform;
            }
        }
        /// <summary>
        /// Get position in world coordinates. Should only be called on receiving pipelines.
        /// </summary>
        /// <returns></returns>
        virtual public Vector3 GetPosition()
        {
            if (isLocalPlayer)
            {
                Debug.LogError("Programmer error: BasePipeline: GetPosition called for pipeline that is a source");
                return new Vector3();
            }
            return transform.position;
        }

        /// <summary>
        /// Get rotation in world coordinates. Should only be called on receiving pipelines.
        /// </summary>
        /// <returns></returns>
        virtual public Vector3 GetRotation()
        {
            if (isLocalPlayer)
            {
                Debug.LogError("Programmer error: BasePipeline: GetRotation called for pipeline that is a source");
                return new Vector3();
            }
            return transform.rotation * Vector3.forward;
        }

        /// <summary>
        /// Return position and rotation of this user.  Should only be called on sending pipelines.
        /// </summary>
        /// <returns></returns>
        virtual public ViewerInformation GetViewerInformation()
        {
            if (!isLocalPlayer)
            {
                Debug.LogError($"Programmer error: {Name()}: GetViewerInformation called for pipeline that is not a source");
                return new ViewerInformation();
            }
            // The camera object is nested in another object on our parent object, so getting at it is difficult:
            PlayerControllerBase player = gameObject.GetComponentInParent<PlayerControllerBase>();
            Transform cameraTransform = player?.getCameraTransform();
            if (cameraTransform == null)
            {
                Debug.LogError($"Programmer error: {Name()}: no Camera object for self user");
                return new ViewerInformation();
            }
            Vector3 position = cameraTransform.position;
            Vector3 forward = cameraTransform.rotation * Vector3.forward;
            return new ViewerInformation()
            {
                position = position,
                gazeForwardDirection = forward
            };
        }

        // Update is called once per frame
        System.DateTime lastUpdateTime;
        private void Update()
        {
            if (debugTiling)
            {
                // Debugging: print position/orientation of camera and others every 10 seconds.
                if (lastUpdateTime == null || System.DateTime.Now > lastUpdateTime + System.TimeSpan.FromSeconds(10))
                {
                    lastUpdateTime = System.DateTime.Now;
                    if (isLocalPlayer)
                    {
                        ViewerInformation vi = GetViewerInformation();
                        Debug.Log($"{Name()}: Tiling: self: pos=({vi.position.x}, {vi.position.y}, {vi.position.z}), lookat=({vi.gazeForwardDirection.x}, {vi.gazeForwardDirection.y}, {vi.gazeForwardDirection.z})");
                    }
                    else
                    {
                        Vector3 position = GetPosition();
                        Vector3 rotation = GetRotation();
                        Debug.Log($"{Name()}: Tiling: other: pos=({position.x}, {position.y}, {position.z}), rotation=({rotation.x}, {rotation.y}, {rotation.z})");
                    }
                }
            }
        }
    }
}