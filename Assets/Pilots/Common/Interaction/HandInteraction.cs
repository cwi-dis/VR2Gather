using UnityEngine;
using UnityEngine.InputSystem;

namespace VRT.Pilots.Common
{
	using HandState = HandController.HandState;

	public class HandInteraction : MonoBehaviour
	{

		
		[Tooltip("Teleporter to use")]
		public VRT.Teleporter.BaseTeleporter teleporter;

		[Tooltip("If non-null, use this gameobject as hand (otherwise use self)")]
		public GameObject Hand;
		[Tooltip("Controller for the hand (default: gotten from Hand)")]
		[SerializeField] private HandController handController;
		[Tooltip("Player network controller used to communicate changes to other players (default: get from parent)")]
		[SerializeField] private PlayerNetworkController playerNetworkController;
		
		[Tooltip("Collider to use for grabbing")]
		public GameObject GrabCollider;
		[Tooltip("Collider to use for touching")]
		public GameObject TouchCollider;

		[Tooltip("The Input System Action that determines whether we are grabbing (if > 0.5)")]
		[SerializeField] InputActionProperty m_grabbingAction;
		[Tooltip("The Input System Action that determines whether we are pointing (if > 0.5)")]
		[SerializeField] InputActionProperty m_pointingAction;


		[Tooltip("The Input System Action that determines whether we are teleporting (if > 0.5)")]
		[SerializeField] InputActionProperty m_teleportingAction;
	
		[Header("Input Actions")]


		[Header("Introspection objects for debugging")]
		[DisableEditing] public PlayerInput MyPlayerInput;
		[DisableEditing] public bool inTeleportingMode = false;
		[DisableEditing] public bool inTouchingMode = false;
		[DisableEditing] public bool inGrabbingMode = false;


		public void Awake()
		{
		}

		void Start()
		{
			
			if (Hand == null) Hand = gameObject;
			if (handController == null) handController = Hand.GetComponent<HandController>();
			if (playerNetworkController == null) playerNetworkController = GetComponentInParent<PlayerNetworkController>();
			if (handController == null)
            {
				Debug.LogError("HandInteraction: cannot find HandController");
            }
			if (!playerNetworkController.IsLocalPlayer)
            {
				Debug.LogError($"HandInteraction: only for local players");
            }
			GrabCollider.SetActive(false);
			TouchCollider.SetActive(false);
		}

	
		private HandState GetHandState()
        {
			return HandState.Idle;
        }
		
		void Update()
		{
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
		}

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
	}
}