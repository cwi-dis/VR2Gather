using UnityEngine;
using System.Reflection;
using VRT.Core;

public class PlayerManager : MonoBehaviour {
    public int      id;
    public string   orchestratorId;
    public UserRepresentationType userRepresentationType;
    public TMPro.TextMeshProUGUI userName;
    public Camera   cam;
	public GameObject holoCamera;
    public GameObject avatar;
    public GameObject webcam;
    public GameObject pc;
    public GameObject voice;
    public GameObject[] localPlayerOnlyObjects;
    public GameObject[] inputEmulationOnlyObjects;
    public GameObject[] inputGamepadOnlyObjects;
    public GameObject[] inputOculusOnlyObjects;
    public GameObject[] inputOpenVROnlyObjects;
    public GameObject[] inputNonHMDObjects;

	//
	// Enable camera (or camera-like object) and input handling.
	// If not the local player most things will be disabled.
	// If disableInput is true the input handling will be disabled (probably because we are in the calibration
	// scene or some other place where input is handled differently than through the PFB_Player).
	//
    public void setupInputOutput(bool isLocalPlayer, bool disableInput=false)
    {
		// Unity has two types of null. We need the C# null.
		if (holoCamera == null) holoCamera = null;
		// Enable either the normal camera or the holodisplay camera for the local user.
		// Enable various other objects only for the local user
		bool useLocalHoloDisplay = isLocalPlayer && VRConfig.Instance.useHoloDisplay();
		bool useLocalNormalCam = isLocalPlayer && !VRConfig.Instance.useHoloDisplay();
		if (useLocalNormalCam)
        {
			cam.gameObject.SetActive(true);
			cam.transform.localPosition = Vector3.up * VRConfig.Instance.cameraDefaultHeight();
			holoCamera?.SetActive(false);
		}
		else if (useLocalHoloDisplay)
        {
			cam.gameObject.SetActive(false);
			holoCamera.SetActive(true);
			holoCamera.transform.localPosition = Vector3.up * VRConfig.Instance.cameraDefaultHeight();
		}
		else
        {
			cam.gameObject.SetActive(false);
			holoCamera?.SetActive(false);
        }

		// Enable various other objects only for the local user
		foreach (var obj in localPlayerOnlyObjects)
		{
			obj.SetActive(isLocalPlayer);
		}
		// Enable controller emulation (keyboard/mouse) objects only for the local user when using emulation
		bool isLocalEmulationPlayer = isLocalPlayer && !disableInput && VRConfig.Instance.useControllerEmulation();
		foreach (var obj in inputEmulationOnlyObjects)
		{
			obj.SetActive(isLocalEmulationPlayer);
		}
		// Enable gamepad objects only for the local user when using gamepad
		bool isLocalGamepadPlayer = isLocalPlayer && !disableInput && VRConfig.Instance.useControllerGamepad();
		foreach (var obj in inputGamepadOnlyObjects)
		{
			obj.SetActive(isLocalGamepadPlayer);
		}
		// Enable oculus objects only for the local user when using oculus
		bool isLocalOculusPlayer = isLocalPlayer && !disableInput && VRConfig.Instance.useControllerOculus();
		foreach (var obj in inputOculusOnlyObjects)
		{
			obj.SetActive(isLocalOculusPlayer);
		}
		// Enable gamepad objects only for the local user when using gamepad
		bool isLocalOpenVRPlayer = isLocalPlayer && !disableInput && VRConfig.Instance.useControllerOpenVR();
		foreach (var obj in inputOpenVROnlyObjects)
		{
			obj.SetActive(isLocalOpenVRPlayer);
		}
		// Disable objects that should not be used with an HMD (to forestall motion sickness)
		if (VRConfig.Instance.useHMD())
		{
			foreach (var obj in inputNonHMDObjects)
			{
				obj.SetActive(false);
			}
		}
	}

	public Transform getCameraTransform()
    {
		if (VRConfig.Instance.useHoloDisplay())
        {
			return holoCamera.transform;
		}
		else
        {
			return cam.transform;
		}
	}
}
