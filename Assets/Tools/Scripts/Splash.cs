using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Splash : MonoBehaviour {
    public float distance;
    public float velocity;
    public float scaleVel;
    Vector3 scale;

    private void Awake() {
        scale = transform.localScale;
        transform.localScale = new Vector3(0.4f,0.4f,0.4f);
    }

    //bool first = true;
    // Update is called once per frame
    void LateUpdate () {
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        forward = forward.normalized;

        Vector3 position = Camera.main.transform.position + forward * distance;
        Quaternion rotation = Quaternion.LookRotation(forward);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, velocity);
        transform.position = Vector3.Lerp(transform.position, position, velocity);
        transform.localScale = Vector3.Lerp(transform.localScale, scale, scaleVel);
        //first = false;

    }
}
