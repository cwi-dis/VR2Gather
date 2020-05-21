using UnityEngine;

public class VersionLog : MonoBehaviour {
    void Awake() {
        Debug.Log("Application Name: " + Application.productName);
        Debug.Log("Application Version: " + Application.version);
    }
}

