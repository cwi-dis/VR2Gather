using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEditor;
using System.Collections;
using System;

namespace VRT.Pilots.Common
{
	using HandState = HandDirectAppearance.HandState;

	public class HandDirectInteraction : MonoBehaviour
	{

		
		[Tooltip("If non-null, use this gameobject as hand (otherwise use self)")]
		public GameObject handGO;
		[Tooltip("If non-null use this hand visualizer (otherwise het from HandGO)")]
		public HandDirectAppearance hand;
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
        [Tooltip("Print logging messages on important changes")]
        [SerializeField] bool debug = false;

        [Tooltip("Current hand state")]
		[DisableEditing] [SerializeField] private HandState currentState;

		public void Awake()
		{
		}

      
		void Start()
		{
			
			if (handGO == null) handGO = gameObject;
			if (hand == null) hand = handGO.GetComponent<HandDirectAppearance>();
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
			StartCoroutine(FixObjectStatesCoro());
		}

		IEnumerator FixObjectStatesCoro()
		{
			for(int i=0; i<5; i++)
			{
				yield return null;
			}
			_FixObjectStates();
		}

		void _FixObjectStates() {
            if (debug) Debug.Log($"{name}: {Time.frameCount} HandState is now {currentState}");
            hand.state = currentState;
			FixGrab fixGrab = GrabCollider.GetComponent<FixGrab>();
			switch (currentState)
			{
				case HandState.Idle:
					fixGrab?.AboutToDisable();
					GrabCollider.SetActive(false);
					TouchCollider.SetActive(false);
					TeleporterRay.SetActive(false);
					MoveTurn.SetActive(true);
					ViewAdjust.SetActive(true);
					break;
				case HandState.Pointing:
					fixGrab?.AboutToDisable();
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
					fixGrab?.AboutToDisable();
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
			if (debug) Debug.Log($"{name}: {Time.frameCount} HandState will be {newHandState}");
			currentState = newHandState;
			FixObjectStates();
		}

        /// <summary>
        /// For VR2Gather direct interaction we want touching (with finger extended) to be activating.
        /// This callback should be added to XRDirectInteractor for touch as Hover Entered callback and it will
        /// call OnActivated() on the object that is touched.
        /// </summary>
        /// <param name="args"></param>
        public void OnDirectHoverEnter(HoverEnterEventArgs args)
        {
            var source = (IXRActivateInteractor)args.interactorObject;
            var target = (IXRActivateInteractable)args.interactableObject;
            if (debug) Debug.Log($"{name}: Direct Hover Enter from {source}, calling {target}.OnActivated() ");
            ActivateEventArgs activateArgs = new ActivateEventArgs
            {
                interactorObject = source,
                interactableObject = target
            };
            target.OnActivated(activateArgs);
        }

        /// <summary>
        /// See OnDirectHoverEnter for an explanation.
        /// </summary>
        /// <param name="args"></param>
        public void OnDirectHoverExit(HoverExitEventArgs args)
        {
            var source = (IXRActivateInteractor)args.interactorObject;
            var target = (IXRActivateInteractable)args.interactableObject;
            if (debug) Debug.Log($"{name}: Direct Hover Exit from {source}, calling {target}.OnDeactivated() ");
            DeactivateEventArgs activateArgs = new DeactivateEventArgs
            {
                interactorObject = source,
                interactableObject = target
            };
            target.OnDeactivated(activateArgs);
        }
    }
}