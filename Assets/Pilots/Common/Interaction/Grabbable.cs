using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	/// <summary>
	/// Component to implement VR2Gather grabbable object.
	/// Works with ray interaction and direct interaction.
	/// The interactors should be set up to call the OnGrab() and OnRelease() methods of this object,
	/// when the object has entered/exited select mode.
	/// 
	/// The object requires a Rigidbody, and that seems to be enough both for direct and ray-based interaction.
	/// 
	/// While the object is held it will send out messages to synchronize its position and orientation
	/// to other instances of the VR2Gather experience.
	/// While the object is held it will be kinematic, reverting to gravity when released.
	/// 
	/// </summary>
	public class Grabbable : NetworkIdBehaviour
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

		// xxxjack private HandController _CurrentGrabber;

		protected override void Awake()
		{
			base.Awake();
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_RigidbodySyncMessage, typeof(RigidbodySyncMessage));

		}
		public void OnEnable()
		{
			GrabbableObjectManager.RegisterGrabbable(this);

			OrchestratorController.Instance.Subscribe<RigidbodySyncMessage>(OnRigidbodySync);
		}

		public void OnDisable()
		{
			
			GrabbableObjectManager.UnregisterGrabbable(this);

			OrchestratorController.Instance.Unsubscribe<RigidbodySyncMessage>(OnRigidbodySync);
		}

		public void Update()
		{
			if (!isGrabbed) return;
			// xxxjack bail out if sending too many updates
			if (Time.realtimeSinceStartup < _lastUpdateTime + (1 / UpdateFrequency)) return;
			_lastUpdateTime = Time.realtimeSinceStartup;
			SendSyncMessage();
		}

		public void SendSyncMessage()
        {
			Debug.Log($"Grabbable: xxxjack SendSyncMessage id={NetworkId} isGrabbed={isGrabbed}");
			OrchestratorController.Instance.SendTypeEventToAll(
					new RigidbodySyncMessage
					{
						NetworkId = NetworkId,
						isGrabbed = isGrabbed,
						Position = Rigidbody.transform.position,
						Rotation = Rigidbody.transform.rotation
					});
		}

		public void OnGrab()
		{
			Debug.Log($"Grabbable({name}): grabbed");
			isGrabbed = true;
			Rigidbody.isKinematic = true;
			Rigidbody.useGravity = false;
		}

		public void OnRelease()
		{
			Debug.Log($"Grabbable({name}): released");
			isGrabbed = false;
			SendSyncMessage();
			Rigidbody.isKinematic = false;
			Rigidbody.useGravity = true;
		}


		private void OnRigidbodySync(RigidbodySyncMessage rigidBodySyncMessage)
		{
			if (rigidBodySyncMessage.NetworkId == NetworkId && !isGrabbed)
			{
				Rigidbody.Sleep();
				Rigidbody.transform.position = rigidBodySyncMessage.Position;
				Rigidbody.transform.rotation = rigidBodySyncMessage.Rotation;
				Rigidbody.isKinematic = rigidBodySyncMessage.isGrabbed;
				Rigidbody.useGravity = !rigidBodySyncMessage.isGrabbed;
			}
		}
	}
}