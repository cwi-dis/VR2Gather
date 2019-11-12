using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pulse : MonoBehaviour {

    [SerializeField] float duration = 1.0f;
    Color col;

    void Start() {
        col = gameObject.GetComponent<MeshRenderer>().material.color;
    }

    // Update is called once per frame
    void Update () {
        float lerp = Mathf.PingPong(Time.time, duration) / duration;
        gameObject.GetComponent<MeshRenderer>().material.color = new Color(col.r, col.g, col.b, Mathf.Lerp(0.0f, 1.0f, Mathf.SmoothStep(0.0f, 1.0f, lerp)));
    }
}
