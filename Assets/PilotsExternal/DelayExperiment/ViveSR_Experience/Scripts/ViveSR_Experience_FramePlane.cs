using UnityEngine;

namespace Vive.Plugin.SR.Experience
{
    /// <summary>
    /// Attach to a default quad.
    /// </summary>
    public class ViveSR_Experience_FramePlane : MonoBehaviour
    {
        public float planeToCameraTranslationZ = 2f;

        private bool reverseTextureY = false;
        public bool ReverseTextureY { get {return reverseTextureY;} set {reverseTextureY = value;}}

        private Vector3 planeToCameraTranslation = Vector3.zero;
        private Quaternion planeToCameraRotation = Quaternion.identity;
        private Vector3 planeToCameraScaling = Vector3.one;
        private Vector3 cameraToParentTranslation = Vector3.zero;
        private Quaternion cameraToParentRotation = Quaternion.identity;

        private void Start()
        {
            GetComponent<Renderer>().material.shader = Shader.Find("Unlit/Texture");
        }

        public void SetCameraIntrinsics(
            float width, float height, float cx, float cy, float focalLength)
        {
            // Calculate the transformation from the plane space to the camera space.
            // Assume the default quad has size 1 meter x 1 meter.
            var quadWidthScaling = width / focalLength * planeToCameraTranslationZ;
            var quadHeightScaling = height / focalLength * planeToCameraTranslationZ;
            var cxFromCenterInMeter = (cx - width / 2f) / focalLength * planeToCameraTranslationZ;
            var cyFromCenterInMeter = (cy - height / 2f) / focalLength * planeToCameraTranslationZ;
            planeToCameraTranslation = new Vector3(
                -cxFromCenterInMeter, -cyFromCenterInMeter, planeToCameraTranslationZ);
            planeToCameraScaling = new Vector3(quadWidthScaling, quadHeightScaling, 1f);
            if (reverseTextureY)
            {
                planeToCameraScaling.y *= -1;
            }

            // Update the transformation from the plane space to the parent space.
            UpdateLocalTransform();
        }

        /// <summary>
        /// Set the frame plane's local transformation by providing
        /// the frame camera's local transformation, which is assumed
        /// only containing translation and rotation.
        /// </summary>
        public void SetCameraLocalExtrinsics(Matrix4x4 matrix)
        {
            cameraToParentTranslation = new Vector3(matrix[0, 3], matrix[1, 3], matrix[2, 3]);
            cameraToParentRotation = matrix.rotation;
            // Update the frame plane's local transformation.
            UpdateLocalTransform();
        }

        public void SetCameraLocalPosition(Vector3 position)
        {
            cameraToParentTranslation = position;
            UpdateLocalTransform();
        }

        public void SetMainTexture(Texture texture)
        {
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;
        }

        private void UpdateLocalTransform()
        {
            // Calculate the transformation from the plane space to the camera space,
            // by concatenating the plane-to-camera transformation and the
            // camera-to-parent transformation.
            // The camera-to-parent transformation has an identity scaling.
            transform.localPosition = cameraToParentRotation * planeToCameraTranslation + cameraToParentTranslation;
            transform.localRotation = cameraToParentRotation * planeToCameraRotation;
            transform.localScale = planeToCameraScaling;
        }
    }
}