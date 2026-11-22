using UnityEngine;
using UnityEngine.Rendering;
#if VRT_HAS_URP
using UnityEngine.Rendering.Universal;
#endif

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Makes the overlay cameras (the HUD camera) render on top of the base camera, in both render pipelines.
    ///
    /// URP needs the overlay cameras marked as Overlay and in the base camera's camera stack; without that
    /// the second camera renders as a Base camera and the screen goes black. That setting lives in URP's
    /// UniversalAdditionalCameraData component, which can't be on the prefab: in a project without the URP
    /// package it is a missing script, and Unity refuses to save prefabs with missing scripts (#384).
    /// So this component sets it up at runtime. In BIRP it does nothing: the overlay cameras' own
    /// Camera settings (clear depth only, higher depth) already do the job.
    ///
    /// Remove when BIRP support goes: then UniversalAdditionalCameraData can be on the prefab again (#432).
    /// </summary>
    public class CameraStackSetup : MonoBehaviour
    {
        [Tooltip("The camera the overlay cameras render on top of")]
        public Camera baseCamera;
        [Tooltip("Cameras that render on top of the base camera, in this order")]
        public Camera[] overlayCameras;

        void Awake()
        {
            // The URP package can be installed in a project that uses BIRP (VRTApp-Prison did), so check
            // the active pipeline, not only whether URP is there.
            if (GraphicsSettings.currentRenderPipeline == null) return;
#if VRT_HAS_URP
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset) return;
            if (baseCamera == null)
            {
                Debug.LogError($"CameraStackSetup: {name}: baseCamera not set");
                return;
            }
            var stack = baseCamera.GetUniversalAdditionalCameraData().cameraStack;
            foreach (var cam in overlayCameras)
            {
                if (cam == null) continue;
                cam.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
                if (!stack.Contains(cam)) stack.Add(cam);
            }
#else
            Debug.LogWarning($"CameraStackSetup: {name}: a scriptable render pipeline is active, but VR2Gather only supports URP and BIRP");
#endif
        }
    }
}
