using UnityEngine;
using UnityEngine.XR;
using VRT.Core;

public class MovePlayer : MonoBehaviour {
    public string leftRightAxis = "Horizontal";
    public string forwardAxis = "Vertical";
    public bool invertForwardAxis = false;
    public float speed = 5f;
    public CharacterController controller;

    // Update is called once per frame
    void Update() {
        float x = Input.GetAxis(leftRightAxis);
        float z = Input.GetAxis(forwardAxis);
        if (invertForwardAxis) z = -z;

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);
    }
}
