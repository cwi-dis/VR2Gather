﻿using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	public class HandNetworkController : MonoBehaviour
	{
		ActionBasedController controller;
		public Hand hand;
		public class HandControllerData : BaseMessage
		{
			public Handedness handHandedness;
			public HandState handState;
		}

		public enum HandState
		{
			Idle,
			Pointing,
			Grabbing,
			Teleporting,
			ViewAdjusting
		}

		public enum Handedness
		{
			Left,
			Right
		}

		public HandState handState;
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

		public Grabbable HeldGrabbable;

		private bool _CanGrabAgain = true;

		private Animator _Animator;

		private PlayerNetworkController _Player;

		public void Awake()
		{
			_Player = GetComponentInParent<PlayerNetworkController>();
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_HandControllerData, typeof(HandNetworkController.HandControllerData));
		}

		void Start()
		{
			controller = GetComponent<ActionBasedController>();
#if xxxjack_old
			_Animator = GetComponentInChildren<Animator>();
#endif
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

		private void Update()
		{
			float isGrabbing = controller.selectAction.action.ReadValue<float>();
			float isPointing = controller.activateAction.action.ReadValue<float>();
//			Debug.Log($"xxxjack hand {HandHandedness} isGrabbing={isGrabbing} isPointing={isPointing}");
			hand.SetGrab(isGrabbing > 0.5);
			hand.SetPoint(isPointing > 0.5);
#if xxxjack_old
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
#endif
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

				if (data.handHandedness == handHandedness)
				{
					SetHandState(data.handState);
				}
			}
		}

		public void SetHandState(HandState handState)
		{
			if (this.handState != handState)
			{
				this.handState = handState;
                UpdateAnimation();
				//
				// If we are a hand of the local player we forward the state change,
				// so other players can see it too.
				//
				if (_Player.IsLocalPlayer)
				{
					var data = new HandControllerData
                    {
						handHandedness = handHandedness,
						handState = this.handState
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
#if xxxjack_old
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
#endif
		}
	}
}