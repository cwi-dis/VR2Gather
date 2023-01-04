using System;
using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	using HandState = Hand.HandState;

	public class HandNetworkControllerBase : MonoBehaviour
	{
		[Tooltip("The visual hand this controller is attached to")]
		public Hand hand;
		public class HandControllerData : BaseMessage
		{
			public Handedness handHandedness;
			public HandState handState;
		}

		public enum Handedness
		{
			Left,
			Right
		}

		[Tooltip("Current state of the hand")]
		public HandState handState;
		[Tooltip("Handedness of the hand")]
		public Handedness handHandedness;

		public enum HandInteractionEventType
		{
			Grab = 0,
			Release = 1
		}

		public class HandGrabEvent : BaseMessage
		{
			public string GrabbableObjectId;
			public string UserId;
			public Handedness Handedness;
			public HandInteractionEventType EventType;
		}

		[Tooltip("The object the hand is currently holding")]
		[SerializeField] protected Grabbable m_HeldGrabbable;
		public virtual Grabbable HeldGrabbable
		{
			get => m_HeldGrabbable;
			set => throw new System.NotImplementedException();
		}

		private bool _CanGrabAgain = true;

		protected PlayerNetworkController _Player;

		public void Awake()
		{
			_Player = GetComponentInParent<PlayerNetworkController>();
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_HandControllerData, typeof(HandNetworkControllerBase.HandControllerData));
		}

		protected virtual void Start()
		{
			_Player = GetComponentInParent<PlayerNetworkController>();

			OrchestratorController.Instance.Subscribe<HandControllerData>(OnHandControllerData);
		}

		private void OnDestroy()
		{
			OrchestratorController.Instance.Unsubscribe<HandControllerData>(OnHandControllerData);
		}


		private void OnTriggerStay(Collider other)
		{
			if (handState == HandState.Grabbing && _CanGrabAgain && HeldGrabbable == null)
			{
				var grabbable = other.GetComponent<Grabbable>();
				if (grabbable == null)
				{
					return;
				}

				HandGrabEvent handGrabEvent = new HandGrabEvent()
				{
					GrabbableObjectId = grabbable.NetworkId,
					UserId = _Player.UserId,
					Handedness = handHandedness,
					EventType = HandInteractionEventType.Grab,
				};

				ExecuteHandGrabEvent(handGrabEvent);

				_CanGrabAgain = false;
			}
		}

		protected void ExecuteHandGrabEvent(HandGrabEvent handGrabEvent)
		{
			// We are not enabled if we are running without the orchestrator
			if (!enabled) return;
			//If we're not master, inform the master
			//And then execute the event locally already instead of waiting for it to return
			if (!OrchestratorController.Instance.UserIsMaster)
			{
				OrchestratorController.Instance.SendTypeEventToMaster(handGrabEvent);
			}
			else
			{
				GrabbableObjectManager.Instance.HandleHandGrabEvent(handGrabEvent);
			}
		}

		internal void OnNetworkRelease(Grabbable grabbable)
		{
			if (m_HeldGrabbable != grabbable)
            {
				Debug.LogWarning($"{name}: OnNetworkRelease {grabbable} but  holding {m_HeldGrabbable}");
			}
			m_HeldGrabbable = null;
		}

		internal void OnNetworkGrab(Grabbable grabbable)
		{
			if (m_HeldGrabbable != null)
            {
				Debug.LogWarning($"{name}: OnNetworkGrab but already holding {m_HeldGrabbable}");
            }
			m_HeldGrabbable = grabbable;
		}

		void OnHandControllerData(HandControllerData data)
		{
			//
			// For incoming hand data, see if this is for a remote player hand and we
			// are that player and that hand. If so: Update our visual representation/animation.
			//
			if (!_Player.IsLocalPlayer && _Player.UserId == data.SenderId)
			{
				if (OrchestratorController.Instance.UserIsMaster)
				{
					OrchestratorController.Instance.SendTypeEventToAll(data, true);
				}

				if (data.handHandedness == handHandedness)
				{
					// This will update the local hand state and run the animation (if needed)
					hand.state = data.handState;
				}
			}
		}
	}
}