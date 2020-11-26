using UnityEngine;

namespace VRTCore
{
    public class BasePipeline : MonoBehaviour
    {
        protected bool isSource = false;
        public BasePipeline Init(bool preview = false)
        {

            return this;
        }

        public SyncConfig GetSyncConfig()
        {
            Debug.LogError("Programmer error: BasePipeline: GetSyncConfig should be overriden in subclass");
            return new SyncConfig();
        }

        public void SetSyncConfig(SyncConfig config)
        {
            Debug.LogError("Programmer error: BasePipeline: SetSyncConfig should be overriden in subclass");

        }

        public Vector3 GetPosition()
        {
            if (isSource)
            {
                Debug.LogError("Programmer error: WebCamPipeline: GetPosition called for pipeline that is a source");
                return new Vector3();
            }
            return transform.position;
        }

        public Vector3 GetRotation()
        {
            if (isSource)
            {
                Debug.LogError("Programmer error: WebCamPipeline: GetRotation called for pipeline that is a source");
                return new Vector3();
            }
            return transform.rotation * Vector3.forward;
        }

        public float GetBandwidthBudget()
        {
            return 999999.0f;
        }

        public ViewerInformation GetViewerInformation()
        {
            if (!isSource)
            {
                Debug.LogError("Programmer error: WebCamPipeline: GetViewerInformation called for pipeline that is not a source");
                return new ViewerInformation();
            }
            // The camera object is nested in another object on our parent object, so getting at it is difficult:
            Camera _camera = gameObject.transform.parent.GetComponentInChildren<Camera>();
            if (_camera == null)
            {
                Debug.LogError("Programmer error: WebCamPipeline: no Camera object for self user");
                return new ViewerInformation();
            }
            Vector3 position = _camera.transform.position;
            Vector3 forward = _camera.transform.rotation * Vector3.forward;
            return new ViewerInformation()
            {
                position = position,
                gazeForwardDirection = forward
            };
        }
    }
}