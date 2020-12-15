using Orchestrator;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRTPilots
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

		public XRNode XRNode;
		public State HandState;
		public Handedness HandHandedness;

		public GameObject GrabCollider;
		public GameObject TouchCollider;

		public LineRenderer TeleportLineRenderer;
		public Material TeleportPossibleMaterial;
		public Material TeleportImpossibleMaterial;


		private Animator _Animator;
		private NetworkPlayer _Player;

		private List<XRNodeState> _NodeStates = new List<XRNodeState>();

		private bool _WasAButtonPressed = false;
		private PlayerLocation _SelectedLocation;

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
				InputTracking.GetNodeStates(_NodeStates);

				//Prevent floor clipping when input tracking provides glitched results
				//This could on occasion cause released grabbables to go throught he floor
				if (transform.position.y <= 0.05f)
				{
					transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
				}


				bool index_trigger_pressed = ControllerInput.Instance.PrimaryTrigger(XRNode);
				bool hand_trigger_pressed = ControllerInput.Instance.SecondaryTrigger(XRNode);
				bool a_button_held_down = ControllerInput.Instance.ButtonA();

				if (HandHandedness == Handedness.Right && a_button_held_down)
				{
					_WasAButtonPressed = true;
					var touchTransform = TouchCollider.transform;
					Debug.DrawLine(touchTransform.position, touchTransform.position + 10.0f * touchTransform.forward, Color.red);
					Ray teleportRay = new Ray(touchTransform.position, touchTransform.forward);
					RaycastHit hit = new RaycastHit();

					TeleportLineRenderer.enabled = true;

					Vector3[] points = new Vector3[2];
					points[0] = touchTransform.position;
					points[1] = touchTransform.position + 25.0f * touchTransform.forward;

					if (Physics.Raycast(teleportRay, out hit))
					{
						points[1] = hit.point;
						if (hit.collider.tag == "PlayerLocation")
						{
							var location = hit.collider.GetComponent<PlayerLocation>();
							if (location.IsEmpty)
							{
								TeleportLineRenderer.material = TeleportPossibleMaterial;
								_SelectedLocation = location;
							}
							else
							{
								TeleportLineRenderer.material = TeleportImpossibleMaterial;
								_SelectedLocation = null;
							}
						}
						else
						{
							TeleportLineRenderer.material = TeleportImpossibleMaterial;
							_SelectedLocation = null;
						}
					}
					else
					{
						TeleportLineRenderer.material = TeleportImpossibleMaterial;
					}
					TeleportLineRenderer.SetPositions(points);
				}
				else if (_WasAButtonPressed)
				{
					if (_SelectedLocation != null)
					{
						SessionPlayersManager.Instance.RequestLocationChange(_SelectedLocation.NetworkId);
					}
					_SelectedLocation = null;
					TeleportLineRenderer.material = TeleportImpossibleMaterial;
					TeleportLineRenderer.enabled = false;
				}



				if (index_trigger_pressed && hand_trigger_pressed)
				{
					SetHandState(State.Grabbing);
					GrabCollider.SetActive(true);
					TouchCollider.SetActive(false);
				}
				else if (hand_trigger_pressed)
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