using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	[RequireComponent(typeof(HandController))]
	public class HandInteractionManager : MonoBehaviour
	{
		public enum HandInteractionEventType
		{
			Grab = 0,
			Release = 1
		}

		public class HandGrabEvent : BaseMessage
		{
			public string GrabbableObjectId;
			public string UserId;
			public HandController.Handedness Handedness;
			public HandInteractionEventType EventType;
		}

		public HandController HandController;
		public Grabbable HeldGrabbable;

		private bool _CanGrabAgain = true;

		public HandController.Handedness Handedness
		{
			get
			{
				return HandController.HandHandedness;
			}
		}

		private NetworkPlayer _Player;

		public void Awake()
		{
			HandController = GetComponent<HandController>();
			_Player = GetComponentInParent<NetworkPlayer>();
		}


		private void OnTriggerStay(Collider other)
		{
			if (HandController.HandState == HandController.State.Grabbing && _CanGrabAgain && HeldGrabbable == null)
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
					Handedness = HandController.HandHandedness,
					EventType = HandInteractionEventType.Grab,
				};

				ExecuteHandGrabEvent(handGrabEvent);

				_CanGrabAgain = false;
			}
		}

		private void Update()
		{
			if (HeldGrabbable != null && HandController.HandState != HandController.State.Grabbing)
			{
				HandGrabEvent handGrabEvent = new HandGrabEvent()
				{
					GrabbableObjectId = HeldGrabbable.NetworkId,
					UserId = _Player.UserId,
					Handedness = HandController.HandHandedness,
					EventType = HandInteractionEventType.Release
				};

				ExecuteHandGrabEvent(handGrabEvent);
			}

			if (!_CanGrabAgain && HandController.HandState != HandController.State.Grabbing)
			{
				_CanGrabAgain = true;
			}
		}

		private void ExecuteHandGrabEvent(HandGrabEvent handGrabEvent)
		{
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
	}
}