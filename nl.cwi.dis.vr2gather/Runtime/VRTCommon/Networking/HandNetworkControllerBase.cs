using System;
using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	using HandState = HandDirectAppearance.HandState;

	public class HandNetworkControllerBase : MonoBehaviour
	{
		[Tooltip("The visual hand this controller is attached to")]
		public HandDirectAppearance handAppearance;
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
		[SerializeField] protected VRTGrabbableController m_HeldGrabbable;
		public virtual VRTGrabbableController HeldGrabbable
		{
			get => m_HeldGrabbable;
			set => throw new System.NotImplementedException();
		}

		protected PlayerNetworkControllerBase _Player;

		bool subscribed = false;

		protected virtual void Start()
		{
			_Player = GetComponentInParent<PlayerNetworkControllerBase>();

            OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_HandControllerData, typeof(HandNetworkControllerBase.HandControllerData));
            OrchestratorController.Instance.Subscribe<HandControllerData>(OnHandControllerData);
			subscribed = true;
		}

		private void OnDestroy()
		{
			if (!subscribed) return;
			subscribed = false;
			OrchestratorController.Instance.Unsubscribe<HandControllerData>(OnHandControllerData);
		}

		protected void ExecuteHandGrabEvent(HandGrabEvent handGrabEvent)
		{
			// We are not enabled if we are running without the orchestrator
			if (!enabled)
			{
				Debug.Log($"HandNetworkControllerBase({name}): not forwarding HandGrabEvent");
				return;
			}
			//If we're not master, inform the master
			//And then execute the event locally already instead of waiting for it to return
			if (!OrchestratorController.Instance.UserIsMaster)
			{
				OrchestratorController.Instance.SendTypeEventToMaster(handGrabEvent);
			}
			else
			{
				VRTGrabbableManager.Instance.HandleHandGrabEvent(handGrabEvent);
			}
		}

		internal void OnNetworkRelease(VRTGrabbableController grabbable)
		{
			if (m_HeldGrabbable != grabbable && m_HeldGrabbable != null)
            {
				Debug.LogWarning($"HandNetworkControllerBase({name}): OnNetworkRelease {grabbable} but  holding {m_HeldGrabbable}");
			}
            Debug.Log($"HandNetworkControllerBase({name}): OnNetworkRelease({grabbable})");
            m_HeldGrabbable = null;
		}

		internal void OnNetworkGrab(VRTGrabbableController grabbable)
		{
			if (m_HeldGrabbable == grabbable)
			{
				Debug.Log($"HandNetworkControllerBase({name}): ignoring OnNetworkGrab for {grabbable} which is already held");
				return;
			}
			if (m_HeldGrabbable != null)
            {
				Debug.LogWarning($"HandNetworkControllerBase({name}): OnNetworkGrab {grabbable} but already holding {m_HeldGrabbable}");
            }
			Debug.Log($"HandNetworkControllerBase({name}): OnNetworkGrab({grabbable})");
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
					handAppearance.state = data.handState;
				}
			}
		}
	}
}