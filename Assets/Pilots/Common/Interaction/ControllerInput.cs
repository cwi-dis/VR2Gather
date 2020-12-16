using UnityEngine;
using UnityEngine.XR;

namespace VRT.Pilots.Common
{
	public class ControllerInput : MonoBehaviour
	{
		private static ControllerInput _Instance;

		public static ControllerInput Instance
		{
			get
			{
				if (_Instance == null)
				{
					_Instance = FindObjectOfType<ControllerInput>();
				}
				return _Instance;
			}
		}

		private static string PrimaryTriggerLeft = "PrimaryTriggerLeft";
		private static string PrimaryTriggerRight = "PrimaryTriggerRight";
		private static string SecondaryTriggerLeft = "SecondaryTriggerLeft";
		private static string SecondaryTriggerRight = "SecondaryTriggerRight";

		[System.Obsolete]
		public void Awake()
		{
			if (_Instance != null)
			{
				Destroy(gameObject);
			}
			else
			{
				_Instance = this;
			}

			XRDevice.SetTrackingSpaceType(TrackingSpaceType.RoomScale);
			Setup();
		}

		public void Setup()
		{
		}

		public bool PrimaryTrigger(XRNode Node)
		{
			if (Node == XRNode.LeftHand)
			{
				return Input.GetAxis(PrimaryTriggerLeft) >= 0.01f;
			}
			else if (Node == XRNode.RightHand)
			{
				return Input.GetAxis(PrimaryTriggerRight) >= 0.01f;
			}
			return false;
		}

		public bool SecondaryTrigger(XRNode Node)
		{
			if (Node == XRNode.LeftHand)
			{
				return Input.GetAxis(SecondaryTriggerLeft) >= 0.01f;
			}
			else if (Node == XRNode.RightHand)
			{
				return Input.GetAxis(SecondaryTriggerRight) >= 0.01f;
			}
			return false;
		}

		public bool ButtonA()
		{
			return Input.GetKey(KeyCode.JoystickButton0);
		}
	}
}