using UnityEngine;
using System.Reflection;
using VRTCore;

public class PlayerManager : MonoBehaviour {
    public int      id;
    public string   orchestratorId;
    public TMPro.TextMeshProUGUI userName;
    public Camera   cam;
    public ITVMHookUp tvm;
    public GameObject avatar;
    public GameObject webcam;
    public GameObject pc;
    public GameObject audio;
    public GameObject teleporter;
}
