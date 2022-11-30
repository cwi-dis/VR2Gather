using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Interactive360.Utils;

public class CameraScript : MonoBehaviour
{

    public Transform Cameraposition;
    public float speed = 2;
    float anglez;
    bool drag;
    string button;
    Vector3 NewPosition, OldPosition;
    void Start() {
        drag = false;
        Cameraposition.eulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
        OldPosition = new Vector3(0.0f, 0.0f, 0.0f);
        NewPosition = new Vector3(0.0f, 0.0f, 0.0f);
        button = "Fire1";
    }
    void Update() {
        if (Input.GetButtonDown("Fire1")) drag = true;
        if (Input.GetButtonUp("Fire1")) drag = false;
        if (drag == false) return;
        Debug.Log("Fire1");
        anglez = transform.eulerAngles.z;
        if (Input.GetAxis("Mouse X") < -0)
        {
            NewPosition.y += (Input.GetAxis("Mouse X")) * (-1.0f) * Time.deltaTime * speed;
            //transform.Rotate(0, (Input.GetAxis("Mouse X")) * (-1.0f)*Time.deltaTime * speed, -anglez);
        }
        if (Input.GetAxis("Mouse X") > 0)
        {
            //transform.Rotate(0, (Input.GetAxis("Mouse X")) * (-1.0f)*Time.deltaTime * speed, -anglez);
            NewPosition.y += (Input.GetAxis("Mouse X")) * (-1.0f) * Time.deltaTime * speed;

        }

        if (Input.GetAxis("Mouse Y") < -0)
        {
            NewPosition.x += (Input.GetAxis("Mouse Y")) *Time.deltaTime * speed;
            //transform.Rotate((Input.GetAxis("Mouse Y")) * Time.deltaTime * speed,0, -anglez);
        }
        if (Input.GetAxis("Mouse Y") > 0)
        {
            //transform.Rotate( (Input.GetAxis("Mouse Y")) * Time.deltaTime * speed,0, -anglez);
            NewPosition.x += (Input.GetAxis("Mouse Y")) * Time.deltaTime * speed;

        }

        transform.eulerAngles = NewPosition;
    }
    
    
}