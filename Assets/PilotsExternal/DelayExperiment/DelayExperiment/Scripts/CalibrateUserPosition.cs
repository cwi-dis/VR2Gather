using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CalibrateUserPosition : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform dualcameratransformposition;
    void Start()
    {
        StartCoroutine(LateStart(3));
    }
    IEnumerator LateStart(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        //Your Function You Want to Call
        GameObject.Find("CameraReference").transform.eulerAngles = new Vector3(0, -90, 0);
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetAxis("PrimaryTriggerRight") >= 0.9) {
            GameObject.Find("CameraReference").transform.eulerAngles  = new Vector3(0, 0, 0);
        }
        if (dualcameratransformposition != null) dualcameratransformposition.SetPositionAndRotation(GameObject.Find("CameraReference").transform.position, GameObject.Find("CameraReference").transform.rotation);
    }
}
