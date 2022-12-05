using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_Initialization : MonoBehaviour
    {   
        public UnityEvent postInitEvent = new UnityEvent();
        ViveSR_Experience_ActionSequence actionSequence;

        void Awake()
        {
            CheckBasicStatus();
        }
   
        void CheckBasicStatus()
        {
            actionSequence = ViveSR_Experience_ActionSequence.CreateActionSequence(gameObject);
            actionSequence.AddAction(() => CheckViveSRStatus(actionSequence.ActionFinished));
            actionSequence.AddAction(() => CheckCurrentHMDDevice(actionSequence.ActionFinished));
            actionSequence.AddAction(() => DetectHand(actionSequence.ActionFinished));
            actionSequence.AddAction(() => OnHandDetected(actionSequence.ActionFinished));
            actionSequence.AddAction(() => UpdateControllerRenderer(actionSequence.ActionFinished));
            actionSequence.AddAction(() => CheckCamera(actionSequence.ActionFinished));
            actionSequence.AddAction(() => Play());
            actionSequence.StartSequence();
        }

        void CheckViveSRStatus(Action done)
        {
            StartCoroutine(_CheckViveSRStatus(done));
        }

        IEnumerator _CheckViveSRStatus(Action done = null)
        {
            float initTime = Time.timeSinceLevelLoad;

            while (ViveSR.FrameworkStatus == FrameworkStatus.INITIAL || ViveSR.FrameworkStatus == FrameworkStatus.START)
            {
                if(Time.timeSinceLevelLoad - initTime > 10f)
                {
                    string errorMsg = "SRWorks initialization timeout.\nPlease quit the game, restart the SRWorks runtime, and try again.";
                    Debug.Log("[ViveSR Experience] " + errorMsg);
                    actionSequence.StopSequence();
                    ViveSR_DualCameraRig.Instance.SetMode(DualCameraDisplayMode.VIRTUAL);   // Set to virtual mode to show the error panel.
                    ViveSR_Experience.instance.ErrorHandlerScript.EnablePanel(errorMsg);
                }

                Debug.Log("[ViveSR Experience] Waiting for ViveSR");
                yield return new WaitForEndOfFrame();
            }

            if (ViveSR.FrameworkStatus == FrameworkStatus.ERROR || ViveSR.FrameworkStatus == FrameworkStatus.STOP)
            {                                     
                string errorMsg = string.Format("SRWorks initialization failed on {0}: {1}\nPlease quit the game, restart the SRWorks runtime, and try again.",
                    ViveSR.InitialError.FailedModule.ToString(), ViveSR.InitialError.ErrorCode);
                Debug.Log("[ViveSR Experience] " + errorMsg);
                actionSequence.StopSequence();
                ViveSR_DualCameraRig.Instance.SetMode(DualCameraDisplayMode.VIRTUAL);   // Set to virtual mode to show the error panel.
                ViveSR_Experience.instance.ErrorHandlerScript.EnablePanel(errorMsg);
            }
            else if(ViveSR.FrameworkStatus == FrameworkStatus.WORKING)
            {
                ModuleStatus moduleStatus = ModuleStatus.IDLE;

                int result = SRWorkModule_API.GetStatus(ModuleType.RIGIDRECONSTRUCTION, out moduleStatus);

                if ((Error)result == Error.WORK && moduleStatus == ModuleStatus.BLOCKED)
                    ViveSR_Experience.instance.IsAMD = true;
                 
                if (done != null) done();
            }
        }

        void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        void CheckCurrentHMDDevice(Action done = null)
        {
            string trackingSystem = SteamVR.instance.hmd_TrackingSystemName;

            if (trackingSystem == "lighthouse") ViveSR_Experience.instance.CurrentDevice = DeviceType.VIVE_PRO;
            else if (trackingSystem == "vive_eyes") ViveSR_Experience.instance.CurrentDevice = DeviceType.VIVE_COSMOS;
            else ViveSR_Experience.instance.CurrentDevice = DeviceType.NOT_SUPPORT;

            if (ViveSR_Experience.instance.CurrentDevice == DeviceType.NOT_SUPPORT)
            {
                Debug.LogWarning("[ViveSR Experience] Current tracking system (" + trackingSystem + ") is not supported");
                return;
            }

            if (done != null) done();
        }

        private void SetAttachPoint()
        {
            if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_PRO)
            {
                ViveSR_Experience.instance.AttachPointIndex = 0;
                ViveSR_Experience.instance.AttachPoint.transform.GetChild(0).transform.gameObject.SetActive(true);
            }
            else if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_COSMOS)
            {
                if (ViveSR_Experience.instance.targetHand.handType == SteamVR_Input_Sources.RightHand)
                {
                    ViveSR_Experience.instance.AttachPointIndex = 2;
                    ViveSR_Experience.instance.AttachPoint.transform.GetChild(2).transform.gameObject.SetActive(true);
                }
                else if (ViveSR_Experience.instance.targetHand.handType == SteamVR_Input_Sources.LeftHand)
                {    
                    ViveSR_Experience.instance.AttachPointIndex = 1;
                    ViveSR_Experience.instance.AttachPoint.transform.GetChild(1).transform.gameObject.SetActive(true);
                }
            }
                                                                                         
            ViveSR_Experience.instance.AttachPoint.transform.parent = ViveSR_Experience.instance.targetHand.transform;
            ViveSR_Experience.instance.AttachPoint.transform.localPosition = new Vector3(0f, 0f, 0.055f);
            ViveSR_Experience.instance.AttachPoint.transform.localEulerAngles = new Vector3(60f, 0f, 0f);
            ViveSR_Experience.instance.AttachPoint.SetActive(true);
        }

        void CheckCamera(Action done)
        {
            if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_PRO)
                StartCoroutine(_CheckCamera(done));
            else done();
        }

        IEnumerator _CheckCamera(Action done = null)
        {
            while (OpenVR.TrackedCamera == null)
            {
                Debug.Log("[ViveSR Experience] Looking for Tracked Camera");
                yield return new WaitForEndOfFrame();
            }

            uint deviceIndex = OpenVR.k_unTrackedDeviceIndex_Hmd;

            EVRTrackedCameraError error = EVRTrackedCameraError.None;

            ulong _handle = 0;
            error = OpenVR.TrackedCamera.AcquireVideoStreamingService(deviceIndex, ref _handle);

            while (error != EVRTrackedCameraError.None || _handle == 0)
            {
                Debug.Log("[ViveSR Experience] VIVE Pro Camera might have not been enabled in SteamVR SDK");
                error = OpenVR.TrackedCamera.AcquireVideoStreamingService(deviceIndex, ref _handle);
            }

            if (done != null) done();
        }

        void DetectHand(Action done)
        {
            StartCoroutine(_DetectHand(done));
        }
        IEnumerator _DetectHand(Action done = null)
        {
            while (ViveSR_Experience.instance.targetHand == null)
            {
                Debug.Log("[ViveSR Experience] Looking for Tracked Controller");
                for (int i = 0; i < Player.instance.handCount; i++)
                {
                    if (!Player.instance.GetHand(i).isPoseValid) continue;

                    ViveSR_Experience.instance.targetHand = Player.instance.GetHand(i);

                    break;
                }

                yield return new WaitForEndOfFrame();
            }
            Debug.Log("[ViveSR Experience] Controller Found");

            if (done != null) done();
        }

        void OnHandDetected(Action done = null)
        {
            SetAttachPoint();

            // Move playerHeadCollision to follow the left tracked camera.
            var playerHeadCollision = ViveSR_Experience.instance.PlayerHeadCollision;
            ViveSR_DualCameraRig.Instance.AttachToTrackedCameraLeft(playerHeadCollision.transform);
            playerHeadCollision.transform.localPosition = Vector3.zero;
            playerHeadCollision.transform.localRotation = Quaternion.identity;

            ViveSR_Experience_ControllerDelegate.instance.Init();

            Destroy(ViveSR_Experience.instance.targetHand.transform.Find("ControllerHoverHighlight").gameObject);    //don't allow highlight from steamVR
            Destroy(GameObject.Find("HeadCollider").gameObject);

            if (done != null) done(); 
        }                                
        
        void UpdateControllerRenderer(Action done)
        {
            // Set shader of controller renderer due to shader in SteamVR_RenderModel not functioning
            StartCoroutine(_UpdateControllerRenderer(done));
        }
        IEnumerator _UpdateControllerRenderer(Action done = null)
        {
            // Detect controller renderer
            while (GameObject.Find(ViveSR_Experience.instance.targetHand.renderModelPrefab.name + "(Clone)") == null)
                yield return new WaitForEndOfFrame();

            ViveSR_Experience.instance.ControllerObjGroup = GameObject.Find(ViveSR_Experience.instance.targetHand.renderModelPrefab.name + "(Clone)").gameObject;

            while (ViveSR_Experience.instance.ControllerRenderers.Count == 0)
            {
                Renderer[] renderers = ViveSR_Experience.instance.ControllerObjGroup.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        Renderer targetRenderer = renderers[i];
                        targetRenderer.material.shader = Shader.Find("ViveSR/Standard, Stencil"); // Update controller shader
                        ViveSR_Experience.instance.ControllerRenderers.Add(targetRenderer);
                    }
                }
                yield return new WaitForEndOfFrame();
            }

            ViveSR_ControllerLatency controllerLatency = FindObjectOfType<ViveSR_ControllerLatency>();

            // Set Controller Latency to hand and move both controller and controllerGUI
            if (controllerLatency)
            {
                controllerLatency.moveAttachedObject = false;

                if (ViveSR_Experience.instance.targetHand.handType == SteamVR_Input_Sources.LeftHand)
                    controllerLatency.trackController = ViveSR_ControllerLatency.ControllerHandType.LeftHand;
                if (ViveSR_Experience.instance.targetHand.handType == SteamVR_Input_Sources.RightHand)
                    controllerLatency.trackController = ViveSR_ControllerLatency.ControllerHandType.RightHand;

                RenderModel mainRenderModel = ViveSR_Experience.instance.ControllerObjGroup.GetComponent<RenderModel>();
                controllerLatency.AddObjectToMove(mainRenderModel.transform, new Vector3(0, 0, 0), new Vector3(0, 0, 0));
                // For attach point, we also need to apply a local position: (0, 0, 0.055F) and rotation: EulerAngles (60f, 0f, 0f), refer to ViveSR_Experience_ControllerGUI.SetAttachPoint()
                controllerLatency.AddObjectToMove(ViveSR_Experience.instance.AttachPoint.transform, new Vector3(0, 0, 0.055F), new Vector3(60F, 0, 0));
            }

            if (done != null) done();
        }

        void Play()
        { 
            switch (ViveSR_Experience.instance.scene)
            {
                case SceneType.Demo:
                    ViveSR_Experience_Demo.instance.Init(); break;
                case SceneType.Sample1:
                    FindObjectOfType<Sample1_Effects_SwitchMode>().Init(); break;
                case SceneType.Sample2:
                    FindObjectOfType<Sample2_DepthImage>().Init(); break;
                case SceneType.Sample3:
                    FindObjectOfType<Sample3_DynamicMesh>().Init(); break;
                case SceneType.Sample4:    
                    if(IsBlocked_AMD()) break;
                    else FindObjectOfType<Sample4_StaticMesh>().Init(); break;
                case SceneType.Sample5:
                    if (IsBlocked_AMD()) break;
                    FindObjectOfType<Sample5_ChairSegmentation>().Init(); break;
                case SceneType.Sample6:
                    if (IsBlocked_Cosmos()) break;
                    FindObjectOfType<Sample6_CameraControl>().Init(); break;
                case SceneType.Sample7:
                    FindObjectOfType<Sample7_Portal>().Init(); break;
                case SceneType.Sample8:
                    if (IsBlocked_AMD()) break;
                    FindObjectOfType<Sample8_TileDrawer>().Init(); break;
                case SceneType.Sample9:
                    if (IsBlocked_AMD()) break;
                    FindObjectOfType<Sample9_SemanticSegmentation>().Init(); break;
                case SceneType.Sample10:
                    FindObjectOfType<Sample10_HumanCut>().Init(); break;
            }
            postInitEvent.Invoke();
        }

        bool IsBlocked_AMD()
        {
            if (ViveSR_Experience.instance.IsAMD)
            {
                ViveSR_Experience.instance.ErrorHandlerScript.EnablePanel("Your GPU does not support this feature.\nPlease quit the game.");

                return true;
            }
            return false;
        }

        bool IsBlocked_Cosmos()
        {
            if (ViveSR_Experience.instance.CurrentDevice == DeviceType.VIVE_COSMOS)
            {
                ViveSR_Experience.instance.ErrorHandlerScript.EnablePanel("Your HMD does not support this feature.\nPlease quit the game.");

                return true;
            }
            return    true;
        }
    }
}