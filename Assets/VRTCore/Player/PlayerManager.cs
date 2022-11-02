using UnityEngine;
using System.Reflection;
using VRT.Core;

public class PlayerManager : MonoBehaviour {
    public int      id;
    public string   orchestratorId;
    public UserRepresentationType userRepresentationType;
    public TMPro.TextMeshProUGUI userName;
    public string userNameStr;
    public Camera   cam;
	public GameObject holoCamera;
    public GameObject avatar;
    public GameObject webcam;
    public GameObject pc;
    public GameObject voice;
    public GameObject[] localPlayerOnlyObjects;
   
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
		// xxxjack This currentaly always enables the normal camera and disables the holoCamera.
		// xxxjack to be fixed at some point.
		bool useLocalHoloDisplay = isLocalPlayer && false;
		bool useLocalNormalCam = isLocalPlayer && true;
		if (useLocalNormalCam)
        {
			cam.gameObject.SetActive(true);
			holoCamera?.SetActive(false);
		}
		else if (useLocalHoloDisplay)
        {
			cam.gameObject.SetActive(false);
			holoCamera.SetActive(true);
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
	}

	public Transform getCameraTransform()
    {
		if (holoCamera != null && holoCamera.activeSelf)
        {
			return holoCamera.transform;
		}
		else
		{
			return cam.transform;
		}
	}
}
