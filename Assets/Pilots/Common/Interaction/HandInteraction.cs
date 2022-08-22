using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;
using VRT.Orchestrator.Wrapping;

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

		public bool inTeleportMode = false;
		public bool inPointingMode = false;
		public bool inGrabbingMode = false;

		void OnModeTeleporting(InputValue value)
		{
			bool onOff = value.Get<float>() > 0.5;
			Debug.Log($"xxxjack HandInteraction: OnModeTeleporting {onOff}");
			// Releaseing the teleport key will execute the teleport
			if (inTeleportMode && !onOff)
            {
				if (teleporter.canTeleport())
				{
					teleporter.Teleport();
				}
			}
			inTeleportMode = onOff;
			UpdateHandState();
		}

		void OnModePointing(InputValue value)
		{
			bool onOff = value.Get<float>() > 0.5;
			if (pointingModeAxisInvert) onOff = !onOff;
			Debug.Log($"xxxjack HandInteraction: OnModePointing {onOff}");
			inPointingMode = onOff;
			UpdateHandState();
		}

		void OnModeGrabbing(InputValue value)
		{
			bool onOff = value.Get<float>() > 0.5;
			if (grabbingModeAxisInvert) onOff = !onOff;
			Debug.Log($"xxxjack HandInteraction: OnModeGrabbing {onOff}");
			inGrabbingMode = onOff;
			UpdateHandState();

		}

		void OnTeleportHome(InputValue value)
		{
			Debug.Log("xxxjack HandInteraction: OnTeleportHome");
			if (teleporter.teleporterActive) teleporter.TeleportHome();
			inTeleportMode = false;
			UpdateHandState();
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


#if !ENABLE_INPUT_SYSTEM

				if (pointingModeKey != KeyCode.None)
				{
					inPointingMode = Input.GetKey(pointingModeKey);
				}
				if (pointingModeAxis != "")
				{
					inPointingMode = Input.GetAxis(pointingModeAxis) >= 0.5f;
				}
				if (pointingModeAxisInvert) inPointingMode = !inPointingMode;

				if (pointingModeKey != KeyCode.None)
				{
					inGrabbingMode = Input.GetKey(grabbingModeKey);
				}
				if (grabbingModeAxis != "")
				{
					inGrabbingMode = Input.GetAxis(grabbingModeAxis) >= 0.5f;
				}
				if (grabbingModeAxisInvert) inGrabbingMode = !inGrabbingMode;
#endif

#if ENABLE_INPUT_SYSTEM
				if (inTeleportMode)
                {
					var touchTransform = TouchCollider.transform;
					teleporter.CustomUpdatePath(touchTransform.position, touchTransform.forward, teleportStrength);
				}
#else
				if (teleportModeKey != KeyCode.None && teleporter != null)
				{
					inTeleportMode = teleportModeKey != KeyCode.None && Input.GetKey(teleportModeKey);
					if (inTeleportMode)
					{
						teleporter.SetActive(true);
						var touchTransform = TouchCollider.transform;
						teleporter.CustomUpdatePath(touchTransform.position, touchTransform.forward, teleportStrength);
						// See if user wants to go to the home position
						if (teleportHomeKey != KeyCode.None && Input.GetKeyDown(teleportHomeKey))
						{
							teleporter.TeleportHome();
                        }
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
#endif
#if !ENABLE_INPUT_SYSTEM
				UpdateHandState();

#endif
			}
		}

		void UpdateHandState()
        {
			if (inGrabbingMode)
			{
				_Controller.SetHandState(HandController.State.Grabbing);
				GrabCollider.SetActive(true);
				TouchCollider.SetActive(false);
				teleporter.SetActive(false);
				inPointingMode = false;
				inTeleportMode = false;
			}
			else if (inPointingMode)
			{
				_Controller.SetHandState(HandController.State.Pointing);
				GrabCollider.SetActive(false);
				TouchCollider.SetActive(true);
				teleporter.SetActive(false);
				inTeleportMode = false;
			}
			else if (inTeleportMode)
            {
				GrabCollider.SetActive(false);
				TouchCollider.SetActive(false);
				teleporter.SetActive(true);
				teleporter.CustomUpdatePath(
					TouchCollider.transform.position,
					TouchCollider.transform.forward,
					teleportStrength
					);
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