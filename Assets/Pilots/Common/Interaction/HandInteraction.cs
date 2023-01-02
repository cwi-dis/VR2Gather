using UnityEngine;
using UnityEngine.InputSystem;

namespace VRT.Pilots.Common
{
	using HandState = HandNetworkController.HandState;

	public class HandInteraction : MonoBehaviour
	{

		
		[Tooltip("If non-null, use this gameobject as hand (otherwise use self)")]
		public GameObject handGO;
		[Tooltip("If non-null use this hand visualizer (otherwise het from HandGO)")]
		public Hand hand;
		[Tooltip("Controller for the hand (default: gotten from HandGO)")]
		[SerializeField] private HandNetworkController handController;
		[Tooltip("Player network controller used to communicate changes to other players (default: get from parent)")]
		[SerializeField] private PlayerNetworkController playerNetworkController;
		
		[Tooltip("GameObject with collider to use for grabbing")]
		public GameObject GrabCollider;
		[Tooltip("GameObject with collider to use for touching")]
		public GameObject TouchCollider;
		[Tooltip("GameObject with teleporter ray")]
		public GameObject TeleporterRay;

		[Tooltip("Debugging: set this action to override the other actions: mode switching is done with this action.")]
		[SerializeField] InputActionProperty m_modeSwitchAction;
		[Tooltip("The Input System Action that determines whether we are grabbing (if > 0.5)")]
		[SerializeField] InputActionProperty m_grabbingAction;
		[Tooltip("The Input System Action that determines whether we are pointing (if > 0.5)")]
		[SerializeField] InputActionProperty m_pointingAction;
		[Tooltip("The Input System Action that determines whether we are teleporting (if > 0.5)")]
		[SerializeField] InputActionProperty m_teleportingAction;
	
		[Tooltip("Current hand state")]
		[DisableEditing] [SerializeField] private HandState currentState;

		public void Awake()
		{
		}

		void Start()
		{
			
			if (handGO == null) handGO = gameObject;
			if (hand == null) hand = handGO.GetComponent<Hand>();
			if (handController == null) handController = handGO.GetComponent<HandNetworkController>();
			if (playerNetworkController == null) playerNetworkController = GetComponentInParent<PlayerNetworkController>();
			if (handController == null)
            {
				Debug.LogError("HandInteraction: cannot find HandController");
            }
			if (!playerNetworkController.IsLocalPlayer)
            {
				Debug.LogError($"HandInteraction: only for local players");
            }
			currentState = HandState.Idle;
			FixObjectStates();
		}

		void FixObjectStates()
        {
			Debug.Log($"HandInteraction: state={currentState}");
			hand?.SetGrab(currentState == HandState.Grabbing);
			hand?.SetPoint(currentState == HandState.Pointing || currentState == HandState.Teleporting);
			switch (currentState)
			{
				case HandState.Idle:
					GrabCollider.SetActive(false);
					TouchCollider.SetActive(false);
					TeleporterRay.SetActive(false);
					break;
				case HandState.Pointing:
					GrabCollider.SetActive(false);
					TouchCollider.SetActive(true);
					TeleporterRay.SetActive(false);
					break;
				case HandState.Grabbing:
					GrabCollider.SetActive(true);
					TouchCollider.SetActive(false);
					TeleporterRay.SetActive(false);
					break;
				case HandState.Teleporting:
					GrabCollider.SetActive(false);
					TouchCollider.SetActive(false);
					TeleporterRay.SetActive(true);
					break;
			}
		}

	
		private HandState GetHandState()
        {
			if (m_modeSwitchAction.action != null)
            {
				// To debug actions it may be helpful that the normal grab/point
				// are only used for the interactors. The mode button will switch through
				// the various modes.
				if (m_modeSwitchAction.action.WasPerformedThisFrame())
                {
					switch(currentState)
                    {
						case HandState.Idle:
							return HandState.Pointing;
						case HandState.Pointing:
							return HandState.Grabbing;
						case HandState.Grabbing:
							return HandState.Teleporting;
						case HandState.Teleporting:
							return HandState.Idle;
                    }
                }
				return currentState;
            }
			if (m_teleportingAction.action != null)
			{
				if (m_teleportingAction.action.IsPressed()) return HandState.Teleporting;
			}
			if (m_pointingAction.action != null)
            {
				if (m_pointingAction.action.ReadValue<float>() > 0.5) return HandState.Pointing;
            }
			if (m_grabbingAction.action != null)
            {
				if (m_grabbingAction.action.ReadValue<float>() > 0.5) return HandState.Grabbing;
            }
			return HandState.Idle;
        }
		
		void Update()
		{
			var newHandState = GetHandState();
			if (newHandState == currentState) return;
			// xxxjack should we teleport if we've left teleport mode?
			currentState = newHandState;
			FixObjectStates();
#if xxxjack_old
			if (!playerNetworkController.IsLocalPlayer)

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
				inTeleportingMode = MyModeTeleportingAction.IsPressed();
				inTouchingMode = MyModeTouchingAction.IsPressed();
				if (negateTouching) inTouchingMode = !inTouchingMode;
				inGrabbingMode = MyGrabbingGrabAction.IsPressed();
				if (inTeleportingMode || inGrabbingMode)
                {
					inTouchingMode = false;
                }
				if (inTeleportingMode)
                {
					inGrabbingMode = false;
                }
				if (inTeleportingMode)
                {
					teleporter.UpdatePath();
					if (MyTeleportHomeAction.IsPressed())
                    {
						// Debug.Log("xxxjack teleport home");
						teleporter.TeleportHome();
                    }
				}
				else
                {
					// If we are _not_ in teleporting mode, but the teleporter
					// is active that means we have just gone out of teleporting mode.
					// We teleport (if possible).
					if (teleporter.teleporterActive)
                    {
						if (teleporter.canTeleport())
                        {
							teleporter.Teleport();
                        }
                    }
                }
				UpdateHandState();
			}
#endif
		}

#if xxxjack_old
		void UpdateHandState()
        {
			if (inTeleportingMode)
			{
				// Teleport mode overrides the other modes, specifically pointing mode.
				handController.SetHandState(HandController.HandState.Pointing);
				GrabCollider.SetActive(false);
				TouchCollider.SetActive(false);
				teleporter.SetActive(true);
				teleporter.UpdatePath();
				inGrabbingMode = false;
				inTouchingMode = false;
			}
			else if(inGrabbingMode)
			{
				handController.SetHandState(HandController.HandState.Grabbing);
				GrabCollider.SetActive(true);
				TouchCollider.SetActive(false);
				teleporter.SetActive(false);
				inTouchingMode = false;
				inTeleportingMode = false;
			}
			else if (inTouchingMode)
			{
				handController.SetHandState(HandController.HandState.Pointing);
				GrabCollider.SetActive(false);
				TouchCollider.SetActive(true);
				teleporter.SetActive(false);
				inTeleportingMode = false;
			}
			else 
			{
				handController.SetHandState(HandController.HandState.Idle);
				GrabCollider.SetActive(false);
				TouchCollider.SetActive(false);
				teleporter.SetActive(false);
			}
		}
#endif
	}
}