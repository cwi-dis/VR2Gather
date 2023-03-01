using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEditor;

// This InputProcessor should negate values, i.e. a range 0..1 will be mapped to 1..0.
// This is different from inverting (which maps -1..1 to 1..-1).
// But it doesn't work...
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class NegateProcessor : InputProcessor<float>
{

#if UNITY_EDITOR
	static NegateProcessor()
	{
		Initialize();
	}
#endif

	[RuntimeInitializeOnLoadMethod]
	static void Initialize()
	{
		InputSystem.RegisterProcessor<NegateProcessor>();
	}

	public override float Process(float value, InputControl control)
	{
		string name = "no control";
		if (control != null) name = control.name;
		return 1 - value;
	}
}

namespace VRT.Pilots.Common
{
	using HandState = Hand.HandState;

	public class HandInteraction : MonoBehaviour
	{

		
		[Tooltip("If non-null, use this gameobject as hand (otherwise use self)")]
		public GameObject handGO;
		[Tooltip("If non-null use this hand visualizer (otherwise het from HandGO)")]
		public Hand hand;
		[Tooltip("Controller for the hand (default: gotten from HandGO)")]
		[SerializeField] private HandNetworkControllerSelf handController;
		[Tooltip("Player network controller used to communicate changes to other players (default: get from parent)")]
		[SerializeField] private PlayerNetworkControllerBase playerNetworkController;
		
		[Tooltip("GameObject with collider to use for grabbing")]
		public GameObject GrabCollider;
		[Tooltip("GameObject with collider to use for touching")]
		public GameObject TouchCollider;
		[Tooltip("GameObject with teleporter ray")]
		public GameObject TeleporterRay;
		[Tooltip("GameObject with move/turn (disabled when teleporter active)")]
		public GameObject MoveTurn;
		[Tooltip("GameObject with view adjust (disabled when teleporter active)")]
		public GameObject ViewAdjust;

		[Tooltip("The Input System Action that determines whether we are grabbing (if > 0.5)")]
		[SerializeField] InputActionProperty m_grabbingAction;
		[Tooltip("The Input System Action that determines whether we are pointing (if > 0.5)")]
		[SerializeField] InputActionProperty m_pointingAction;
		[Tooltip("The Input System Action that determines whether we are teleporting (if > 0.5)")]
		[SerializeField] InputActionProperty m_teleportingAction;
		[Tooltip("The Input System Action that determines whether we aborted teleport")]
		[SerializeField] InputActionProperty m_teleportCancelAction;

		[Tooltip("Current hand state")]
		[DisableEditing] [SerializeField] private HandState currentState;

		public void Awake()
		{
		}

      
		void Start()
		{
			
			if (handGO == null) handGO = gameObject;
			if (hand == null) hand = handGO.GetComponent<Hand>();
			if (handController == null) handController = handGO.GetComponent<HandNetworkControllerSelf>();
			if (playerNetworkController == null) playerNetworkController = GetComponentInParent<PlayerNetworkControllerBase>();
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
			hand.state = currentState;
			switch (currentState)
			{
				case HandState.Idle:
					GrabCollider.SetActive(false);
					TouchCollider.SetActive(false);
					TeleporterRay.SetActive(false);
					MoveTurn.SetActive(true);
					ViewAdjust.SetActive(true);
					break;
				case HandState.Pointing:
					GrabCollider.SetActive(false);
					TouchCollider.SetActive(true);
					TeleporterRay.SetActive(false);
					MoveTurn.SetActive(true);
					ViewAdjust.SetActive(true);
					break;
				case HandState.Grabbing:
					GrabCollider.SetActive(true);
					TouchCollider.SetActive(false);
					TeleporterRay.SetActive(false);
					MoveTurn.SetActive(true);
					ViewAdjust.SetActive(true);
					break;
				case HandState.Teleporting:
					GrabCollider.SetActive(false);
					TouchCollider.SetActive(false);
					TeleporterRay.SetActive(true);
					MoveTurn.SetActive(false);
					ViewAdjust.SetActive(false);
					break;
			}
		}

		private HandState GetHandState()
        {
			if (m_teleportingAction.action != null)
			{
				if (m_teleportingAction.action.triggered) return HandState.Teleporting;
			}
			// If we are in teleporting we stay there as long the the button keeps being depressed and we're not cancelling
			if (currentState == HandState.Teleporting)
			{
				if (m_teleportCancelAction.action.triggered) return HandState.Idle;
				if (m_teleportingAction.action.IsPressed()) return HandState.Teleporting;
			}
			// Grabbing has priority over pointing (because you can grab without your index
			// finger on the oculus)
			if (m_grabbingAction.action != null)
			{
				if (m_grabbingAction.action.ReadValue<float>() > 0.5) return HandState.Grabbing;
			}
			if (m_pointingAction.action != null)
            {
				if (m_pointingAction.action.ReadValue<float>() > 0.5) return HandState.Pointing;
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

		public void OnSelectEnter(SelectEnterEventArgs args)
		{
			var interactable = args.interactable;
			GameObject grabbedObject = interactable.gameObject;
			VRTGrabbableController grabbable = grabbedObject.GetComponent<VRTGrabbableController>();
			if (grabbable == null)
            {
				Debug.LogError($"{name}: grabbed {grabbedObject} which has no Grabbable");
            }
			Debug.Log($"{name}: grabbed {grabbable}");
			handController.HeldGrabbable = grabbable;
		}

		public void OnSelectExit(SelectExitEventArgs args)
		{
			// xxxjack we could check that the object released is actually held...
			// xxxjack may also be needed if we can hold multiple objects....
			Debug.Log($"{name}: released {handController.HeldGrabbable}");
			handController.HeldGrabbable = null;

		}

		public void OnDirectHoverEnter(HoverEnterEventArgs args)
		{
			Debug.Log("Direct Hover Enter");
		}

		public void OnDirectHoverExit(HoverExitEventArgs args)
		{
			Debug.Log("Direct Hover Exit");
		}
	}
}