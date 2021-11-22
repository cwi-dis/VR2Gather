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
}
