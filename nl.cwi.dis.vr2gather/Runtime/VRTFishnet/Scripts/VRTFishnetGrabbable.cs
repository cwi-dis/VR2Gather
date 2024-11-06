using UnityEngine;
using VRT.Orchestrator.Wrapping;
using FishNet.Object;

namespace VRT.Fishnet
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
	public class VRTFishnetGrabbable : NetworkBehaviour
	{


		[Tooltip("The grabbable object itself")]
		public Rigidbody Rigidbody;
		[Tooltip("The NetworkObject of the grabbable object itself")]
		public NetworkObject NetworkObject;
		
		[Tooltip("Introspection/debug: is the object grabbed and transmitting its position?")]
		[SerializeField] private bool isGrabbedByMe;
        [Tooltip("Print logging messages on important changes")]
        [SerializeField] bool debug = false;

        // xxxjack private HandController _CurrentGrabber;



		public void Update()
		{
#if xxxjackdeleted
			// If the local user is not grabbing this grabble we have nothing to do.
			if (!isGrabbed) return;
			// xxxjack bail out if sending too many updates
			if (Time.realtimeSinceStartup < _lastUpdateTime + (1 / UpdateFrequency)) return;
			_lastUpdateTime = Time.realtimeSinceStartup;
			SendRigidbodySyncMessage();
#endif
		}

		public void SendRigidbodySyncMessage()
		{
#if xxxjackdeleted
			if (debug) Debug.Log($"Grabbable: SendSyncMessage id={NetworkId} isGrabbed={isGrabbed}");

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
#endif
		}

		public void OnGrab()
		{
			if (debug) Debug.Log($"FishnetGrabbable({name}): grabbed by me");
			isGrabbedByMe = true;
			Rigidbody.isKinematic = true;
			Rigidbody.useGravity = false;
			OnGrabServer(NetworkObject);
		}

		public void OnRelease()
		{
			if (debug) Debug.Log($"FishnetGrabbable({name}): released by me");
			isGrabbedByMe = false;
			Rigidbody.isKinematic = false;
			Rigidbody.useGravity = true;
			OnReleaseServer(NetworkObject);
		}

		[ServerRpc(RequireOwnership = false)]
		public void OnGrabServer(NetworkObject nob)
		{
			if (debug) Debug.Log($"FishnetGrabbable({name}): server: grabbed by someone");
			nob.GiveOwnership(base.Owner);
			OnGrabObserver(nob);
		}

		[ObserversRpc]
		public void OnGrabObserver(NetworkObject nob)
		{
			if (nob.IsOwner) {
				if (debug) Debug.Log($"FishnetGrabbable({name}): observer: grabbed by me");
			}
			else
			{
				if (debug) Debug.Log($"FishnetGrabbable({name}): observer: grabbed by someone else");
			}		
		}

		[ServerRpc(RequireOwnership = false)]
		public void OnReleaseServer(NetworkObject nob)
		{
			if (debug) Debug.Log($"FishnetGrabbable({name}): server: released by someone");
			OnReleaseObserver(nob);
		}

		[ObserversRpc]
		public void OnReleaseObserver(NetworkObject nob)
		{
			if (nob.IsOwner) {
				if (debug) Debug.Log($"FishnetGrabbable({name}): observer: released by me");
			}
			else
			{
				if (debug) Debug.Log($"FishnetGrabbable({name}): observer: released by someone else");
			}
		}

#if xxxjackdeleted

		private void OnNetworkRigidbodySync(RigidbodySyncMessage rigidBodySyncMessage)
		{
			if (rigidBodySyncMessage.NetworkId != NetworkId)
			{
				return;
			}
			if (isGrabbed)
			{
				Debug.Log("Grabbable: ignore OnRigidBodySync for locally grabbed object");
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
#endif
	}
}