using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroSceneController : MonoBehaviour {
    public static IntroSceneController Instance { get; private set; }

    public float introDuration = 20;
    public float logoVRTDuration = 5;
    public float logoECDuration = 6;

    private void Awake() {
        Instance = this;
    }

    private void OnEnable() {
        RenderSettings.fog = false;
    }

}
