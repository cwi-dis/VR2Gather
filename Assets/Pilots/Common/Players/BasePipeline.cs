using UnityEngine;
using System.Collections.Generic;
using Cwipc;

namespace VRT.Core
{
    abstract public class BasePipeline : MonoBehaviour
    {
        protected bool isSource = false;

        public delegate BasePipeline AddPipelineComponentDelegate(GameObject dst, UserRepresentationType i);

        private static Dictionary<UserRepresentationType, AddPipelineComponentDelegate> PipelineTypeMapping = new Dictionary<UserRepresentationType, AddPipelineComponentDelegate>();

        protected static void RegisterPipelineClass(UserRepresentationType i, AddPipelineComponentDelegate ctor)
        {
            Debug.Log($"BasePipeline: register Pipeline constructor for {i}");
            PipelineTypeMapping[i] = ctor;
        }

        public static BasePipeline AddPipelineComponent(GameObject dst, UserRepresentationType i)
        {
            if (!PipelineTypeMapping.ContainsKey(i))
            {
                Debug.LogError($"BasePipeline: programmer error: no constructor for {i}");
                return null;
            }
            return PipelineTypeMapping[i](dst, i);
        }

        abstract public BasePipeline Init(object _user, Config._User cfg, bool preview = false);

        virtual public string Name()
        {
            return $"{GetType().Name}";
        }

        virtual public SyncConfig GetSyncConfig()
        {
            Debug.LogError("Programmer error: BasePipeline: GetSyncConfig should be overriden in subclass");
            return new SyncConfig();
        }

        virtual public void SetSyncConfig(SyncConfig config)
        {
            Debug.LogError("Programmer error: BasePipeline: SetSyncConfig should be overriden in subclass");

        }

        virtual public Vector3 GetPosition()
        {
            if (isSource)
            {
                Debug.LogError("Programmer error: BasePipeline: GetPosition called for pipeline that is a source");
                return new Vector3();
            }
            return transform.position;
        }

        virtual public Vector3 GetRotation()
        {
            if (isSource)
            {
                Debug.LogError("Programmer error: BasePipeline: GetRotation called for pipeline that is a source");
                return new Vector3();
            }
            return transform.rotation * Vector3.forward;
        }

        virtual public float GetBandwidthBudget()
        {
            return 999999.0f;
        }

        virtual public ViewerInformation GetViewerInformation()
        {
            if (!isSource)
            {
                Debug.LogError($"Programmer error: {Name()}: GetViewerInformation called for pipeline that is not a source");
                return new ViewerInformation();
            }
            // The camera object is nested in another object on our parent object, so getting at it is difficult:
            PlayerManager player = gameObject.GetComponentInParent<PlayerManager>();
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
    }
}