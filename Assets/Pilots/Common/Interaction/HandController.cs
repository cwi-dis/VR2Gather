using UnityEngine;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	public class HandController : MonoBehaviour
	{
		public class HandControllerData : BaseMessage
		{
			public Handedness HandHandedness;
			public State HandState;
		}

		public enum State
		{
			Idle,
			Pointing,
			Grabbing
		}

		public enum Handedness
		{
			Left,
			Right
		}

		public State HandState;
		public Handedness HandHandedness;

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

		public Grabbable HeldGrabbable;

		private bool _CanGrabAgain = true;

		private Animator _Animator;

		private NetworkPlayer _Player;

		public void Awake()
		{
			_Player = GetComponentInParent<NetworkPlayer>();
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_HandControllerData, typeof(HandController.HandControllerData));
		}

		void Start()
		{
			_Animator = GetComponentInChildren<Animator>();
			_Player = GetComponentInParent<NetworkPlayer>();

			OrchestratorController.Instance.Subscribe<HandControllerData>(OnHandControllerData);
		}

		private void OnDestroy()
		{
			OrchestratorController.Instance.Unsubscribe<HandControllerData>(OnHandControllerData);
		}


		private void OnTriggerStay(Collider other)
		{
			if (HandState == State.Grabbing && _CanGrabAgain && HeldGrabbable == null)
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
					Handedness = HandHandedness,
					EventType = HandInteractionEventType.Grab,
				};

				ExecuteHandGrabEvent(handGrabEvent);

				_CanGrabAgain = false;
			}
		}

		private void Update()
		{
			if (HeldGrabbable != null && HandState != State.Grabbing)
			{
				HandGrabEvent handGrabEvent = new HandGrabEvent()
				{
					GrabbableObjectId = HeldGrabbable.NetworkId,
					UserId = _Player.UserId,
					Handedness = HandHandedness,
					EventType = HandInteractionEventType.Release
				};

				ExecuteHandGrabEvent(handGrabEvent);
			}

			if (!_CanGrabAgain && HandState != State.Grabbing)
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

				if (data.HandHandedness == HandHandedness)
				{
					SetHandState(data.HandState);
				}
			}
		}

		public void SetHandState(State handState)
		{
			if (HandState != handState)
			{
				HandState = handState;
				UpdateAnimation();
				//
				// If we are a hand of the local player we forward the state change,
				// so other players can see it too.
				//
				if (_Player.IsLocalPlayer)
				{
					var data = new HandControllerData
					{
						HandHandedness = HandHandedness,
						HandState = HandState
					};

					if (OrchestratorController.Instance.UserIsMaster)
					{
						OrchestratorController.Instance.SendTypeEventToAll(data);
					}
					else
					{
						OrchestratorController.Instance.SendTypeEventToMaster(data);
					}
				}
			}
		}

		private void UpdateAnimation()
		{
			if (HandState == State.Grabbing)
			{
				if (!_Animator.GetBool("IsGrabbing"))
				{
					_Animator.SetBool("IsGrabbing", true);
				}
				_Animator.SetBool("IsPointing", false);
			}
			else if (HandState == State.Pointing)
			{
				if (!_Animator.GetBool("IsPointing"))
				{
					_Animator.SetBool("IsPointing", true);
				}
				_Animator.SetBool("IsGrabbing", false);
			}
			else
			{
				_Animator.SetBool("IsGrabbing", false);
				_Animator.SetBool("IsPointing", false);
			}
		}
	}
}