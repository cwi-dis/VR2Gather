using UnityEngine;
using System.Reflection;
using VRT.Core;

public class PlayerManager : MonoBehaviour {
    public int      id;
    public string   orchestratorId;
    public UserRepresentationType userRepresentationType;
    public TMPro.TextMeshProUGUI userName;
    public Camera   cam;
    public ITVMHookUp tvm;
    public GameObject avatar;
    public GameObject webcam;
    public GameObject pc;
    public GameObject audio;
    public GameObject[] localPlayerOnlyObjects;
    public GameObject[] inputEmulationOnlyObjects;
    public GameObject[] inputGamepadOnlyObjects;
    public GameObject[] inputOculusOnlyObjects;
    public GameObject[] inputOpenVROnlyObjects;
    public GameObject[] inputNonHMDObjects;

    public void setupInputOutput(bool isLocalPlayer)
    {
		// Enable the camera only for the local user
		cam.gameObject.SetActive(isLocalPlayer);
		// Enable various other objects only for the local user
		foreach (var obj in localPlayerOnlyObjects)
		{
			obj.SetActive(isLocalPlayer);
		}
		// Enable controller emulation (keyboard/mouse) objects only for the local user when using emulation
		bool isLocalEmulationPlayer = isLocalPlayer && VRConfig.Instance.useControllerEmulation();
		foreach (var obj in inputEmulationOnlyObjects)
		{
			obj.SetActive(isLocalEmulationPlayer);
		}
		// Enable gamepad objects only for the local user when using gamepad
		bool isLocalGamepadPlayer = isLocalPlayer && VRConfig.Instance.useControllerGamepad();
		foreach (var obj in inputGamepadOnlyObjects)
		{
			obj.SetActive(isLocalGamepadPlayer);
		}
		// Enable oculus objects only for the local user when using oculus
		bool isLocalOculusPlayer = isLocalPlayer && VRConfig.Instance.useControllerOculus();
		foreach (var obj in inputOculusOnlyObjects)
		{
			obj.SetActive(isLocalOculusPlayer);
		}
		// Enable gamepad objects only for the local user when using gamepad
		bool isLocalOpenVRPlayer = isLocalPlayer && VRConfig.Instance.useControllerOpenVR();
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
}
