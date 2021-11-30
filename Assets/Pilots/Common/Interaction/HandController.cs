using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
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

		[Tooltip("When this key is pressed we are in teleporting mode")]
		public KeyCode teleportModeKey = KeyCode.None;

		[Tooltip("Teleporter to use")]
		public VRT.Teleporter.BaseTeleporter teleporter;

		[Tooltip("Arc length for curving teleporters")]
		public float teleportStrength = 10.0f;

		[Tooltip("When this axis is active (or inactive depending on invert) we are in pointing mode")]
		public string pointingModeAxis = "";
		[Tooltip("When this Key is active (or inactive depending on invert) we are in pointing mode")]
		public KeyCode pointingModeKey = KeyCode.None;
		[Tooltip("Invert meaning of PointingModeAxis or Key")]
		public bool pointingModeAxisInvert = false;

		[Tooltip("When this axis is active (or inactive depending on invert) we are in grabbing mode")]
		public string grabbingModeAxis = "";
		[Tooltip("When this Key is active (or inactive depending on invert) we are in grabbing mode")]
		public KeyCode grabbingModeKey = KeyCode.None;
		[Tooltip("Invert meaning of grabbingModeAxis")]
		public bool grabbingModeAxisInvert = false;
		
		public XRNode XRNode;
		public State HandState;
		public Handedness HandHandedness;

		public GameObject GrabCollider;
		public GameObject TouchCollider;

		private Animator _Animator;
		private NetworkPlayer _Player;
		
		public void Awake()
		{
			OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_HandControllerData, typeof(HandControllerData));
		}

		void Start()
		{
			_Animator = GetComponentInChildren<Animator>();
			_Player = GetComponentInParent<NetworkPlayer>();

			GrabCollider.SetActive(false);
			TouchCollider.SetActive(false);

			OrchestratorController.Instance.Subscribe<HandControllerData>(OnHandControllerData);
		}

		private void OnDestroy()
		{
			OrchestratorController.Instance.Unsubscribe<HandControllerData>(OnHandControllerData);
		}

		void Update()
		{
			if (_Player.IsLocalPlayer)
			{
				//Prevent floor clipping when input tracking provides glitched results
				//This could on occasion cause released grabbables to go throught he floor
				if (transform.position.y <= 0.05f)
				{
					transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
				}

				//
				// See whether we are pointing, grabbing, teleporting or idle
				//

				bool pointingModeAxisIsPressed = false;
				if (pointingModeKey != KeyCode.None)
				{
					pointingModeAxisIsPressed = Input.GetKey(pointingModeKey);
				}
				if (pointingModeAxis != "")
				{
					pointingModeAxisIsPressed = Input.GetAxis(pointingModeAxis) >= 0.5f;
				}
				if (pointingModeAxisInvert) pointingModeAxisIsPressed = !pointingModeAxisIsPressed;

				bool grabbingModeAxisIsPressed = false;
				if (pointingModeKey != KeyCode.None)
				{
					grabbingModeAxisIsPressed = Input.GetKey(grabbingModeKey);
				}
				if (grabbingModeAxis != "")
				{
					grabbingModeAxisIsPressed = Input.GetAxis(grabbingModeAxis) >= 0.5f;
				}
				if (grabbingModeAxisInvert) grabbingModeAxisIsPressed = !grabbingModeAxisIsPressed;

				if (grabbingModeAxisInvert) grabbingModeAxisIsPressed = !grabbingModeAxisIsPressed;
				if (teleportModeKey != KeyCode.None && teleporter != null)
				{
					bool teleportModeKeyIsPressed = teleportModeKey != KeyCode.None && Input.GetKey(teleportModeKey);

					if (teleportModeKeyIsPressed)
					{
						teleporter.SetActive(true);
						var touchTransform = TouchCollider.transform;
						teleporter.CustomUpdatePath(touchTransform.position, touchTransform.forward, teleportStrength);
					}
					else if (teleporter.teleporterActive)
					{
						//
						// Teleport key was released. See if we should teleport.
						//
						if (teleporter.canTeleport())
						{
							teleporter.Teleport();
						}
						teleporter.SetActive(false);
					}
				}

				if (grabbingModeAxisIsPressed)
				{
					SetHandState(State.Grabbing);
					GrabCollider.SetActive(true);
					TouchCollider.SetActive(false);
				}
				else if (pointingModeAxisIsPressed)
				{
					SetHandState(State.Pointing);
					GrabCollider.SetActive(false);
					TouchCollider.SetActive(true);
				}
				else
				{
					SetHandState(State.Idle);
					GrabCollider.SetActive(false);
					TouchCollider.SetActive(false);
				}
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

		void SetHandState(State handState)
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