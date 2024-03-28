using UnityEngine;
using System.Collections.Generic;
using Cwipc;
using VRT.Core;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Abstract base class for MonoBehaviour that is responsible for capture/transmission/reception/rendering of a media stream
    /// for a participant. Concrete subclasses exist for each UserRepresentationType.
    ///
    /// The base class also has static members that maintain the mapping of UserRepresentationType to the method that adds
    /// an component of the correct type to a gameobject (the factory function for that implementation subclass of BasePipeline).
    /// </summary>
    abstract public class BasePipeline : MonoBehaviour
    {
        protected bool isSource = false;

        /// <summary>
        /// Function pointer to a creation method for BasePipeline subclass objects.
        /// </summary>
        /// <param name="dst">The GameObject on which the component will be created.</param>
        /// <param name="i">The UserRepresentationType for which we want a pipeline.</param>
        /// <returns></returns>
        public delegate BasePipeline AddPipelineComponentDelegate(GameObject dst, UserRepresentationType i);

        private static Dictionary<UserRepresentationType, AddPipelineComponentDelegate> SelfPipelineTypeMapping = new Dictionary<UserRepresentationType, AddPipelineComponentDelegate>();
        private static Dictionary<UserRepresentationType, AddPipelineComponentDelegate> OtherPipelineTypeMapping = new Dictionary<UserRepresentationType, AddPipelineComponentDelegate>();

        /// <summary>
        /// Register a constructor for a BasePipeline subclass that handles a specific UserRepresentationType.
        /// </summary>
        /// <param name="i">The representation type</param>
        /// <param name="ctor">The constructor to call to create a new instance</param>
        protected static void RegisterPipelineClass(bool isLocalPlayer, UserRepresentationType i, AddPipelineComponentDelegate ctor)
        {
            Debug.Log($"BasePipeline: register Pipeline constructor for {i}, self={isLocalPlayer}");
            if (isLocalPlayer)
            {
                SelfPipelineTypeMapping[i] = ctor;
            }
            else
            {
                OtherPipelineTypeMapping[i] = ctor;
            }
        }

        /// <summary>
        /// Add a component (BasePipeline subclass) for a specific user representation.
        /// </summary>
        /// <param name="dst">The GameObject to which the component is added.</param>
        /// <param name="i">The UserRepresentationType wanted</param>
        /// <returns>The object created</returns>
        public static BasePipeline AddPipelineComponent(GameObject dst, UserRepresentationType i, bool isLocalPlayer)
        {
            var map = isLocalPlayer ? SelfPipelineTypeMapping : OtherPipelineTypeMapping;
            if (!map.ContainsKey(i))
            {
                Debug.LogError($"BasePipeline: programmer error: no constructor for {i}, self={isLocalPlayer}");
                return null;
            }
            return map[i](dst, i);
        }

        /// <summary>
        /// Initialize a pipeline instance.
        /// </summary>
        /// <param name="_user">The structure with the parameters describing the user for which this pieline is created</param>
        /// <param name="cfg">The configration data for this pipeline</param>
        /// <param name="preview">Set to true if the pipeline should not transmit, only render locally.</param>
        /// <returns></returns>
        abstract public BasePipeline Init(bool isLocalPlayer, object _user, VRTConfig._User cfg, bool preview = false);

        virtual public string Name()
        {
            return $"{GetType().Name}";
        }

        /// <summary>
        /// Return synchronization information. Should only be called on sending pipelines.
        /// </summary>
        /// <returns></returns>
        virtual public SyncConfig GetSyncConfig()
        {
            Debug.LogError("Programmer error: BasePipeline: GetSyncConfig should be overriden in subclass");
            return new SyncConfig();
        }

        /// <summary>
        /// Set the synchronization information for this pipeline. Should only be called on receiving pipelines.
        /// </summary>
        /// <param name="config"></param>
        virtual public void SetSyncConfig(SyncConfig config)
        {
            Debug.LogError("Programmer error: BasePipeline: SetSyncConfig should be overriden in subclass");

        }

     

        /// <summary>
        /// Returns bandwidth budget. Unused?
        /// </summary>
        /// <returns></returns>
        virtual public float GetBandwidthBudget()
        {
            return 999999.0f;
        }

        /// <summary>
        /// Return position and rotation of this user.  Should only be called on sending pipelines.
        /// </summary>
        /// <returns></returns>
        virtual public ViewerInformation GetViewerInformation()
        {
            if (!isSource)
            {
                Debug.LogError($"Programmer error: {Name()}: GetViewerInformation called for pipeline that is not a source");
                return new ViewerInformation();
            }
            // The camera object is nested in another object on our parent object, so getting at it is difficult:
            PlayerControllerSelf player = gameObject.GetComponentInParent<PlayerControllerSelf>();
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