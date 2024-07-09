using UnityEngine;
using VRT.Core;

namespace VRT.Pilots.Common
{

    public class PlayerControllerSelf : PlayerControllerBase
    {
        [Tooltip("Disable transmitters for this self-player")]
        public bool previewPlayer = false;

        public bool debugTransform = false;

        void Awake()
        {
            isPreviewPlayer = previewPlayer;
        }

        public override void SetUpPlayerController(bool _isLocalPlayer, VRT.Orchestrator.Elements.User user)
        {
            if (!_isLocalPlayer)
            {
                Debug.LogError($"{Name()}: isLocalPlayer==false");
            }
            isLocalPlayer = true;
            _SetupCommon(user);
            setupCamera();
            LoadCameraTransform();
        }

        public void LoadCameraTransform()
        {
            if (cameraOffset == null)
            {
                Debug.LogError($"{Name()}: No cameraOffset");
            }
            if (!VRTConfig.ISXRActive()) {
                Debug.Log($"{Name()}: XR not active, set default camera transform");
                cameraOffset.localPosition = new Vector3(0, 1.8f, 0);
                cameraOffset.localRotation = Quaternion.identity;
                return;
            }
            Vector3 pos = new Vector3(PlayerPrefs.GetFloat("cam_pos_x", 0), PlayerPrefs.GetFloat("cam_pos_y", 0), PlayerPrefs.GetFloat("cam_pos_z", 0));
            Vector3 rot = new Vector3(PlayerPrefs.GetFloat("cam_rot_x", 0), PlayerPrefs.GetFloat("cam_rot_y", 0), PlayerPrefs.GetFloat("cam_rot_z", 0));
            if (debugTransform) Debug.Log($"{Name()}: loaded self-camera pos={pos}, rot={rot}");
            cameraOffset.localPosition = pos;
            cameraOffset.localRotation = Quaternion.Euler(rot);
        }

        public void SaveCameraTransform()
        {
           if (cameraOffset == null)
            {
                Debug.LogError($"{Name()}: No cameraOffset");
            }
            if (!VRTConfig.ISXRActive()) {
                Debug.Log($"{Name()}: XR not active, not saving camera transform");
                return;
            }
            Vector3 pos = cameraOffset.localPosition;
            Vector3 rot = cameraOffset.localRotation.eulerAngles;
            if (debugTransform) Debug.Log($"{Name()}: Saving self-camera pos={pos}, rot={rot}");
            PlayerPrefs.SetFloat("cam_pos_x", pos.x);
            PlayerPrefs.SetFloat("cam_pos_y", pos.y);
            PlayerPrefs.SetFloat("cam_pos_z", pos.z);
            PlayerPrefs.SetFloat("cam_rot_x", rot.x);
            PlayerPrefs.SetFloat("cam_rot_y", rot.y);
            PlayerPrefs.SetFloat("cam_rot_z", rot.z);
        }

        /// <summary>
        /// Enable camera (or camera-like object) and input handling.
        /// If not the local player most things will be disabled.
        /// If disableInput is true the input handling will be disabled (probably because we are in the calibration
        /// scene or some other place where input is handled differently than through the PFB_Player).
        /// </summary>
        /// <param name="disableInput"></param>
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

        /// <summary>
        /// Returns the transform of whatever camera is currently used (normal or holo).
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Return position and rotation of this user.  Should only be called on sending pipelines.
        /// </summary>
        /// <returns></returns>
        virtual public ViewerInformation GetViewerInformation()
        {
           // The camera object is nested in another object on our parent object, so getting at it is difficult:
            Transform cameraTransform = getCameraTransform();
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
        
        protected override void Update()
        {
            base.Update();
             if (debugTiling)
            {
                // Debugging: print position/orientation of camera and others every 10 seconds.
                if (lastUpdateTime == null || System.DateTime.Now > lastUpdateTime + System.TimeSpan.FromSeconds(10))
                {
                    lastUpdateTime = System.DateTime.Now;
                    
                    ViewerInformation vi = GetViewerInformation();
                    Debug.Log($"{Name()}: Tiling: self: pos=({vi.position.x}, {vi.position.y}, {vi.position.z}), lookat=({vi.gazeForwardDirection.x}, {vi.gazeForwardDirection.y}, {vi.gazeForwardDirection.z})");
                }
            }
        }

        /// <summary>
        /// This method is called by the HUD (or other means) if the user has issued a command.
        /// If the player controller doesn't implement this command the PilotController is tried.
        /// </summary>
        /// <param name="command"></param>
        /// <returns>True if command implemented</returns>
        public virtual bool OnUserCommand(string command)
        {
            if (command == "resetview")
            {
                ViewAdjust va = GetComponentInChildren<ViewAdjust>();
                if (va != null) va.ResetOrigin();
                return true;
            }
            if (command == "lowerview")
            {
                ViewAdjust va = GetComponentInChildren<ViewAdjust>();
                if (va != null) va.LowerView();
                return true;
            }
            if (command == "higherview")
            {
                ViewAdjust va = GetComponentInChildren<ViewAdjust>();
                if (va != null) va.HigherView();
                return true;
            }
            if (command == "resetposition")
            {
                transform.localPosition = Vector3.zero;
                transform.localEulerAngles = Vector3.zero;
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        [ContextMenu("Toggle Invisibility (Editor-only hack)")]
        private void ForceTrigger()
        {
            if (userRepresentation == user.userData.userRepresentationType)
            {
                SetRepresentation(UserRepresentationType.NoRepresentation);
            }
            else
            {
                SetRepresentation(user.userData.userRepresentationType);
            }
        }
#endif
    }
}