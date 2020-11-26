using UnityEngine;

namespace Pilots
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
		private bool _IsSimulating = false;

		private HandInteractionManager _CurrentGrabber;

		public void OnEnable()
		{
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

		public void OnGrab(HandInteractionManager handInteractionManager)
		{
			if (_CurrentGrabber != handInteractionManager)
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

				transform.parent = handInteractionManager.transform;

				if (UseOffsets)
				{
					if (handInteractionManager.Handedness == HandController.Handedness.Left)
					{
						ParentToHand(handInteractionManager.transform, GrabPositionOffsetLeft, GrabRotationOffsetLeft);
					}
					else
					{
						ParentToHand(handInteractionManager.transform, GrabPositionOffsetRight, GrabRotationOffsetRight);
					}
				}

				_CurrentGrabber = handInteractionManager;
				handInteractionManager.HeldGrabbable = this;
				_IsSimulating = false;
			}
		}

		public void OnRelease(HandInteractionManager handInteractionManager)
		{
			if (_CurrentGrabber == handInteractionManager)
			{
				transform.parent = null;
				_CurrentGrabber.HeldGrabbable = null;
				_CurrentGrabber = null;

				if (Rigidbody != null)
				{
					Rigidbody.isKinematic = false;
					Rigidbody.useGravity = true;
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