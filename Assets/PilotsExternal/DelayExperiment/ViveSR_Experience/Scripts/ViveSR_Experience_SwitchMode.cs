using UnityEngine;
using Valve.VR.InteractionSystem;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_SwitchMode : MonoBehaviour
    {
        [SerializeField] GameObject VRMode_bg;
        public DualCameraDisplayMode currentMode = DualCameraDisplayMode.MIX;
        [SerializeField] Material SkyboxMaterial;
        [SerializeField] bool setSkybox;

        private void Start()
        {
            Player.instance.hmdTransforms[0].gameObject.GetComponent<Camera>().enabled = true;
        }

        public void SwitchMode(DualCameraDisplayMode mode)
        {
            if (mode == currentMode) return;

            currentMode = mode;
            ViveSR_DualCameraRig.Instance.SetMode(mode);

            switch (mode)
            {
                case DualCameraDisplayMode.VIRTUAL:
                    // Disable pass through texture update.
                    ViveSR_DualCameraRig.Instance.DualCameraImageRenderer.enabled = false;
                    // Enable the background.
                    if(VRMode_bg != null) VRMode_bg.SetActive(true);
                    SetSkybox(true);
                    break;
                default:
                    // Enable pass through texture update.
                    ViveSR_DualCameraRig.Instance.DualCameraImageRenderer.enabled = true;
                    // Disable the background.
                    if(VRMode_bg != null) VRMode_bg.SetActive(false);
                    SetSkybox(false);
                    break;
            }
        }

        private void OnDestroy()
        {
            if (setSkybox) SetSkybox(false);
        }

        public void SetSkybox(bool on)
        {
            SkyboxMaterial.SetFloat("_Exposure", on ? 1 : 0);
        }
    }
}