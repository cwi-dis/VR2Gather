using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using VRT.Orchestrator.Wrapping;

namespace VRT.Pilots.Common
{
	public class HandInteraction : MonoBehaviour
	{

		[Tooltip("When this key is pressed we are in teleporting mode, using a ray from this hand")]
		public KeyCode teleportModeKey = KeyCode.None;

		[Tooltip("When this key is pressed while in teleporting mode we teleport home")]
		public KeyCode teleportHomeKey = KeyCode.None;

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
		
		public GameObject GrabCollider;
		public GameObject TouchCollider;

		private NetworkPlayer _Player;
		private HandController _Controller;
		
		public void Awake()
		{
		}

		void Start()
		{
			_Player = GetComponentInParent<NetworkPlayer>();
			_Controller = GetComponent<HandController>();

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

				if (grabbingModeAxisIsPressed)
				{
					_Controller.SetHandState(HandController.State.Grabbing);
					GrabCollider.SetActive(true);
					TouchCollider.SetActive(false);
				}
				else if (pointingModeAxisIsPressed)
				{
					_Controller.SetHandState(HandController.State.Pointing);
					GrabCollider.SetActive(false);
					TouchCollider.SetActive(true);
				}
				else
				{
					_Controller.SetHandState(HandController.State.Idle);
					GrabCollider.SetActive(false);
					TouchCollider.SetActive(false);
				}
			}
		}



	}
}