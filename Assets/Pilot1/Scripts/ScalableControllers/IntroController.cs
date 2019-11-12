using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;

public class IntroController : MonoBehaviour {

    private float timer = 0.0f;
    [SerializeField] private float logoVRTDuration = 5.0f;
    [SerializeField] private float logoECDuration = 5.0f;
    private GameObject coches;
    private GameObject logoVRT;
    private GameObject logoEC;

    // Use this for initialization
    IEnumerator Start () {
        coches = transform.Find("Coches").gameObject;
        logoVRT = transform.Find("LogoVRT").gameObject;
        logoEC = transform.Find("LogoEC").gameObject;

        coches.SetActive(false);
        logoVRT.SetActive(false);
        logoEC.SetActive(false);
        
        // Load XR Device
        XRSettings.LoadDeviceByName(new string[] { "Oculus", "OPenVR" });
        yield return null;
        XRSettings.enabled = true;
    }
	
	// Update is called once per frame
    void Update () {
        //Timer
        timer += Time.deltaTime;
        //Activate VRT Logo
        if (timer <= logoVRTDuration && !logoVRT.activeSelf) logoVRT.SetActive(true);
        //Activate EC Logo
        if (timer >= logoVRTDuration && logoVRT.activeSelf) {
            logoVRT.SetActive(false);
            logoEC.SetActive(true);
        }
        if (timer >= (logoVRTDuration + logoECDuration) && logoEC.activeSelf) {
            logoEC.SetActive(false);
            coches.SetActive(true);
        }
    }
}
