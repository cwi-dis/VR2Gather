using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour {
    public int      id;
    public string   orchestratorId;
    public TMPro.TextMeshProUGUI userName;
    public Camera   cam;
    public DataProviders.NetworkDataProvider tvm;
    public GameObject avatar;
    public GameObject webcam;
    public GameObject pc;
    public GameObject audio;
}
