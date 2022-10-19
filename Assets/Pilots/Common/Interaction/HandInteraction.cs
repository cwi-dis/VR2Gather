using UnityEngine;
using UnityEngine.InputSystem;

namespace VRT.Pilots.Common
{
	public class HandInteraction : MonoBehaviour
	{

		
		[Tooltip("Teleporter to use")]
		public VRT.Teleporter.BaseTeleporter teleporter;

		[Tooltip("Arc length for curving teleporters")]
		public float teleportStrength = 10.0f;

		[Tooltip("Invert meaning of PointingModeAxis or Key")]
		public bool pointingModeAxisInvert = false;

		[Tooltip("Invert meaning of grabbingModeAxis")]
		public bool grabbingModeAxisInvert = false;

		[Tooltip("If non-null, use this gameobject as hand (otherwise use self)")]
		public GameObject Hand;
		[Tooltip("Collider to use for grabbing")]
		public GameObject GrabCollider;
		[Tooltip("Collider to use for touching")]
		public GameObject TouchCollider;

		private NetworkPlayer _Player;
		private HandController _Controller;

		[Header("Input Actions")]

		[Tooltip("Name of (button) action that enables touching")]
		public string ModeTouchingActionName;
		[Tooltip("Negate the touching action button")]
		public bool negateTouching;
		InputAction MyModeTouchingAction;
		[Tooltip("Name of (button) action that activates grab")]
		public string GrabbingGrabActionName;
		InputAction MyGrabbingGrabAction;
		[Tooltip("Name of (button) action that enables teleporting")]
		public string ModeTeleportingActionName;
		InputAction MyModeTeleportingAction;
		[Tooltip("Name of action (button) that activates home teleport")]
		public string TeleportHomeActionName;
		InputAction MyTeleportHomeAction;

		[Header("Introspection objects for debugging")]
		public PlayerInput MyPlayerInput;
		public bool inTeleportingMode = false;
		public bool inTouchingMode = false;
		public bool inGrabbingMode = false;

		public void OnControlsChanged(PlayerInput pi)
		{
			// Debug.Log($"xxxjack OnControlsChanged {pi}, enabled={pi.enabled}, inputIsActive={pi.inputIsActive}, controlScheme={pi.currentControlScheme}");
			EnsureDevice();
		}


		public void Awake()
		{
		}

		void Start()
		{
			_Player = GetComponentInParent<NetworkPlayer>();
			if (Hand == null) Hand = gameObject;
			_Controller = Hand.GetComponent<HandController>();
			if (_Controller == null)
            {
				Debug.LogError("HandInteraction: cannot find HandController");
            }

			GrabCollider.SetActive(false);
			TouchCollider.SetActive(false);
			EnsureDevice();
		}

		void EnsureDevice()
        {
			if (MyPlayerInput == null)
            {
				MyPlayerInput = GetComponent<PlayerInput>();
			}
			if (MyPlayerInput == null) Debug.LogError("HandInteraction: cannot find PlayerInput");

			MyModeTouchingAction = MyPlayerInput.actions[ModeTouchingActionName];
			MyGrabbingGrabAction = MyPlayerInput.actions[GrabbingGrabActionName];
			MyModeTeleportingAction = MyPlayerInput.actions[ModeTeleportingActionName];
			MyTeleportHomeAction = MyPlayerInput.actions[TeleportHomeActionName];
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
					var touchTransform = TouchCollider.transform;
					teleporter.CustomUpdatePath(touchTransform.position, touchTransform.forward, teleportStrength);
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
				_Controller.SetHandState(HandController.State.Pointing);
				GrabCollider.SetActive(false);
				TouchCollider.SetActive(false);
				teleporter.SetActive(true);
				teleporter.CustomUpdatePath(
					TouchCollider.transform.position,
					TouchCollider.transform.forward,
					teleportStrength
					);
				inGrabbingMode = false;
				inTouchingMode = false;
			}
			else if(inGrabbingMode)
			{
				_Controller.SetHandState(HandController.State.Grabbing);
				GrabCollider.SetActive(true);
				TouchCollider.SetActive(false);
				teleporter.SetActive(false);
				inTouchingMode = false;
				inTeleportingMode = false;
			}
			else if (inTouchingMode)
			{
				_Controller.SetHandState(HandController.State.Pointing);
				GrabCollider.SetActive(false);
				TouchCollider.SetActive(true);
				teleporter.SetActive(false);
				inTeleportingMode = false;
			}
			else 
			{
				_Controller.SetHandState(HandController.State.Idle);
				GrabCollider.SetActive(false);
				TouchCollider.SetActive(false);
				teleporter.SetActive(false);
			}
		}
	}
}