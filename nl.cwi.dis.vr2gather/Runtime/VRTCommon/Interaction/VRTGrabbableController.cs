using UnityEngine;
using VRT.Core;
using VRT.Orchestrator.Wrapping;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRT.Pilots.Common
{
	/// <summary>
	/// Component to implement VR2Gather grabbable object.
	/// Works with ray interaction and direct interaction.
	/// The interactors should be set up to call the OnSelectEnter() and OnSelectExit() methods of this object,
	/// when the object has entered/exited select mode.
	/// 
	/// The object requires a Rigidbody, and that seems to be enough both for direct and ray-based interaction.
	/// 
	/// While the object is held it will send out messages to synchronize its position and orientation
	/// to other instances of the VR2Gather experience.
	/// While the object is held it will be kinematic, reverting to gravity when released.
	/// 
	/// </summary>
	public class VRTGrabbableController : NetworkIdBehaviour, IVRTGrabbable
	{
		public class RigidbodySyncMessage : BaseMessage
		{
			public string NetworkId;
			public bool isGrabbed;
			public Vector3 Position;
			public Quaternion Rotation;
		}

		[Tooltip("The grabbable object itself")]
		public Rigidbody Rigidbody;
		[Tooltip("Number of times to sync per second")]
		public float UpdateFrequency = 10;
		float _lastUpdateTime;

		[Tooltip("Introspection/debug: is the object grabbed and transmitting its position?")]
		[DisableEditing] [SerializeField] private bool isGrabbed;
        [Tooltip("Print logging messages on important changes")]
        [SerializeField] bool debug = false;
		
        // xxxjack private HandController _CurrentGrabber;

        protected override void Awake()
		{
			Debug.LogError($"{gameObject.name}: VR2Gather VRTGrabbableController objects should not be used in VR2Gather-Fishnet");
			base.Awake();
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_RigidbodySyncMessage, typeof(RigidbodySyncMessage));

		}
		public void OnEnable()
		{
			VRTGrabbableManager.RegisterGrabbable(this);

			OrchestratorController.Instance.Subscribe<RigidbodySyncMessage>(OnNetworkRigidbodySync);
		}

		public void OnDisable()
		{
			
			VRTGrabbableManager.UnregisterGrabbable(this);

			OrchestratorController.Instance.Unsubscribe<RigidbodySyncMessage>(OnNetworkRigidbodySync);
		}

		public void Update()
		{
			// If the local user is not grabbing this grabble we have nothing to do.
			if (!isGrabbed) return;
			// xxxjack bail out if sending too many updates
			if (Time.realtimeSinceStartup < _lastUpdateTime + (1 / UpdateFrequency)) return;
			_lastUpdateTime = Time.realtimeSinceStartup;
			SendRigidbodySyncMessage();
		}

		public void SendRigidbodySyncMessage()
		{
			if (debug) Debug.Log($"VRTGrabbableController: SendSyncMessage id={NetworkId} isGrabbed={isGrabbed}");

			RigidbodySyncMessage message = new RigidbodySyncMessage
			{
				NetworkId = NetworkId,
				isGrabbed = isGrabbed,
				Position = Rigidbody.transform.position,
				Rotation = Rigidbody.transform.rotation
			};
			if (!OrchestratorController.Instance.UserIsMaster)
			{
				OrchestratorController.Instance.SendTypeEventToMaster(message);
			}
			else
			{
				OrchestratorController.Instance.SendTypeEventToAll(message);
			}
		}

		public void OnSelectEnter(SelectEnterEventArgs args)
		{
			if (isGrabbed)
			{
				Debug.LogWarning($"VRTGrabbableController({name}): grabbed, but it was already grabbed");
				return;
			}
            if (debug) Debug.Log($"VRTGrabbableController({name}): grabbed by {args.interactorObject}");
            isGrabbed = true;
			Rigidbody.isKinematic = true;
			Rigidbody.useGravity = false;
			SendRigidbodySyncMessage();
		}

		public void OnSelectExit()
		{
			if ( !isGrabbed) 
            {
				Debug.LogWarning($"VRTGrabbableController({name}): released, but it was not grabbed");
				return;
            }
            if (debug) Debug.Log($"VRTGrabbableController({name}): released");
            isGrabbed = false;
			Rigidbody.isKinematic = false;
            Rigidbody.useGravity = true;
			SendRigidbodySyncMessage();
		}


		private void OnNetworkRigidbodySync(RigidbodySyncMessage rigidBodySyncMessage)
		{
			if (rigidBodySyncMessage.NetworkId != NetworkId)
			{
				return;
			}
			if (isGrabbed)
			{
				Debug.Log("VRTGrabbableController: ignore OnRigidBodySync for locally grabbed object");
				return;
			}
			// If we are master we also forward the message
			if (OrchestratorController.Instance.UserIsMaster)
			{
				OrchestratorController.Instance.SendTypeEventToAll(rigidBodySyncMessage, true);
			}
			Rigidbody.Sleep();
			Rigidbody.transform.position = rigidBodySyncMessage.Position;
			Rigidbody.transform.rotation = rigidBodySyncMessage.Rotation;
			Rigidbody.isKinematic = rigidBodySyncMessage.isGrabbed;
			Rigidbody.useGravity = !rigidBodySyncMessage.isGrabbed;
		}
	}
}