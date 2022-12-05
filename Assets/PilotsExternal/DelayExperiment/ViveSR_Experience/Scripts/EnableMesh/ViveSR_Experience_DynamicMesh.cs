using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    public class ViveSR_Experience_DynamicMesh : MonoBehaviour
    {
        public bool ShowDynamicCollision { get; private set; }
        public bool ShowWireframe { get; private set; }
        [SerializeField] Material wireFrameMaterial, transparentMaterial;
        bool initialized = false;

        public void SetDynamicMesh(bool isOn)
        {
            if (!initialized)
            {
                ViveSR_DualCameraDepthCollider.SetDepthColliderMaterial(wireFrameMaterial);
                ShowWireframe = true;
                initialized = true;
            }

            ViveSR_DualCameraImageCapture.EnableDepthProcess(isOn);  
            ViveSR_DualCameraDepthCollider.UpdateDepthCollider = isOn;
            ViveSR_DualCameraDepthCollider.EnableDepthCollider(isOn);

            ViveSR_DualCameraDepthCollider.DepthColliderVisibility = isOn ? ShowDynamicCollision : false;
            if (ShowDynamicCollision) SetWireframeDisplay(ShowWireframe);
        }

        public void SetMeshDisplay(bool isOn)
        {
            ShowDynamicCollision = isOn;

            ViveSR_DualCameraDepthCollider.DepthColliderVisibility = isOn;
        }
        public void SetWireframeDisplay(bool isOn)
        {
            ShowWireframe = isOn;

            ViveSR_DualCameraDepthCollider.SetDepthColliderMaterial(isOn ? wireFrameMaterial : transparentMaterial);
        }
    }
}