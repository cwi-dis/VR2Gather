using UnityEngine;
using VRT.Core;
using VRT.Orchestrator.Wrapping;
using UnityEngine.XR.Interaction.Toolkit;
using FishNet.Object;
using FishNet.Connection;

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
	public class VRTFishnetGrabbable : NetworkBehaviour, IVRTGrabbable
	{


		[Tooltip("The grabbable object itself")]
		public Rigidbody Rigidbody;
			
		[Tooltip("Introspection/debug: does this instance want to grab this object?")]
		[SerializeField] private bool wantToGrab;
		[Tooltip("debug/introspection: have I grabbed the object?")]
		[SerializeField] private bool haveGrabbed;
		[Tooltip("debug/introspection: has someone grabbed this object?")]
		[SerializeField] private bool someoneHasGrabbed;
        [Tooltip("Print logging messages on important changes")]
        [SerializeField] bool debug = false;


        public void OnSelectEnter(SelectEnterEventArgs args)
        {
            if (debug) Debug.Log($"VRTFishnetGrabbable({name}): want to grab, by {args.interactorObject}");
			wantToGrab = true;
			OnGrabServer(NetworkObject);
		}

		public void OnSelectExit()
		{
			if (debug) Debug.Log($"VRTFishnetGrabbable({name}): released by me");
			wantToGrab = false;
			OnReleaseServer(NetworkObject);
		}

		[ServerRpc(RequireOwnership = false)]
		public void OnGrabServer(NetworkObject nob, NetworkConnection conn = null)
		{
			if (debug) Debug.Log($"FishnetGrabbable({name}): server: grabbed by {conn.ClientId}");
			nob.GiveOwnership(conn);
			OnGrabObserver(nob);
		}

		[ObserversRpc]
		public void OnGrabObserver(NetworkObject nob)
		{
			if (nob.IsOwner) {
				if (debug) Debug.Log($"FishnetGrabbable({name}): observer: grabbed by me");
				haveGrabbed = true;
#if xxxjackdeleted
                // This appears to be handled by the Fishnet NetworkTransform.
				Rigidbody.isKinematic = true;
				Rigidbody.useGravity = false;
#endif
			}
			else
			{
				if (debug) Debug.Log($"FishnetGrabbable({name}): observer: grabbed by someone else");
				Rigidbody.isKinematic = true;
				Rigidbody.useGravity = false;
			}
			someoneHasGrabbed = true;
		}

		[ServerRpc(RequireOwnership = false)]
		public void OnReleaseServer(NetworkObject nob)
		{
			if (debug) Debug.Log($"FishnetGrabbable({name}): server: released by someone");
			// Ownership stays with the last owner.
			OnReleaseObserver(nob);
		}

		[ObserversRpc]
		public void OnReleaseObserver(NetworkObject nob)
		{
			if (nob.IsOwner) {
				if (debug) Debug.Log($"FishnetGrabbable({name}): observer: released by me");
				// I no longer hold the object, but I'm still the Fishnet owner.
				// I have to take care of physics
				Rigidbody.isKinematic = true;
				Rigidbody.useGravity = true;
			}
			else
			{
				if (debug) Debug.Log($"FishnetGrabbable({name}): observer: released by someone else");
				Rigidbody.isKinematic = true;
				Rigidbody.useGravity = true;
			}
			haveGrabbed = false;
			someoneHasGrabbed = false;
		}
	}
}