using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	public class Grabbable : NetworkIdBehaviour
	{
		public class RigidbodySyncMessage : BaseMessage
		{
			public string NetworkId;
			public Vector3 Position;
			public Quaternion Rotation;
		}


		public bool UseOffsets;

		public Vector3 GrabPositionOffsetLeft;
		public Vector3 GrabRotationOffsetLeft;

		public Vector3 GrabPositionOffsetRight;
		public Vector3 GrabRotationOffsetRight;

		public Rigidbody Rigidbody;
		public bool ForceSleepSync;
		private bool _isKinematic = false;
		private bool _useGravity = true;
		private bool _IsSimulating = false;
		private int newGrabbableID = 0;

		private HandController _CurrentGrabber;

		public void Awake()
		{
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_RigidbodySyncMessage, typeof(RigidbodySyncMessage));
		}
		public void OnEnable()
		{	
			// Any grabbable object needs a unique networkID, otherwise it will cause problems. If this are generated on the fly, we need a counter=newGrabbableID
			if (NetworkId == null || NetworkId == "")
			{
				NetworkId = "nid_0000_" + newGrabbableID;
				newGrabbableID += 1;
			}

			// we now register the object to the grabbableObjectManager
			GrabbableObjectManager.RegisterGrabbable(this);
			OrchestratorController.Instance.Subscribe<RigidbodySyncMessage>(OnRigidbodySync);
		}

		public void OnDisable()
		{
			if (_CurrentGrabber != null)
			{
				OnRelease(_CurrentGrabber);
			}
			GrabbableObjectManager.UnregisterGrabbable(this);

			OrchestratorController.Instance.Unsubscribe<RigidbodySyncMessage>(OnRigidbodySync);
		}

        public void Start()
        {
			//on start we save the selected properties
			_isKinematic = Rigidbody.isKinematic;
			_useGravity = Rigidbody.useGravity;
        }

        public void Update()
		{
			if (OrchestratorController.Instance.UserIsMaster && ForceSleepSync && _IsSimulating)
			{
				if (Rigidbody != null && Rigidbody.IsSleeping())
				{
					OrchestratorController.Instance.SendTypeEventToAll(
						new RigidbodySyncMessage
						{
							NetworkId = NetworkId,
							Position = transform.position,
							Rotation = transform.rotation
						});

					_IsSimulating = false;
				}
			}
		}

		public void OnGrab(HandController handController)
		{
			if (_CurrentGrabber != handController)
			{
				if (_CurrentGrabber != null)
				{
					_CurrentGrabber.HeldGrabbable = null;
				}

				if (Rigidbody != null)
				{
					Rigidbody.isKinematic = true;
					Rigidbody.useGravity = false;
				}

				transform.parent = handController.transform;

				if (UseOffsets)
				{
					if (handController.HandHandedness == HandController.Handedness.Left)
					{
						ParentToHand(handController.transform, GrabPositionOffsetLeft, GrabRotationOffsetLeft);
					}
					else
					{
						ParentToHand(handController.transform, GrabPositionOffsetRight, GrabRotationOffsetRight);
					}
				}

				_CurrentGrabber = handController;
				handController.HeldGrabbable = this;
				_IsSimulating = false;
			}
		}

		public void OnRelease(HandController handController)
		{
			if (_CurrentGrabber == handController)
			{
				transform.parent = null;
				_CurrentGrabber.HeldGrabbable = null;
				_CurrentGrabber = null;

				if (Rigidbody != null)
				{
					//we return the original state
					Rigidbody.isKinematic = _isKinematic;
					Rigidbody.useGravity = _useGravity;
					_IsSimulating = true;
				}
			}
		}

		private void ParentToHand(Transform newParent, Vector3 localPosition, Vector3 localRotation)
		{
			transform.parent = newParent;
			transform.localPosition = localPosition;
			transform.localRotation = Quaternion.Euler(localRotation);
		}

		private void OnRigidbodySync(RigidbodySyncMessage rigidBodySyncMessage)
		{
			if (rigidBodySyncMessage.NetworkId == NetworkId && !OrchestratorController.Instance.UserIsMaster)
			{
				Rigidbody.Sleep();
				transform.position = rigidBodySyncMessage.Position;
				transform.rotation = rigidBodySyncMessage.Rotation;
			}
		}
	}
}