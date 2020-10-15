using UnityEngine;
using UnityEngine.XR;

public class MoveCamera : MonoBehaviour {
    public float mouseSensitivity = 100.0f;
    public bool spectator = false;
    float xRotation = 0f;
    float yRotation = 0f;

    public Transform playerBody;
    public Transform avatarHead;

    void Awake() {
        if (XRDevice.isPresent) {
            enabled = false;
        }
        else {
            transform.localPosition = Vector3.up * Config.Instance.nonHMDHeight;
        }
    }

    void Start() {
        //Cursor.lockState = CursorLockMode.Confined;
    }

    void Update() {
        if (Input.GetKey(KeyCode.Mouse0)) {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            yRotation += mouseX;

            if (spectator)
                playerBody.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
            else {
                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                playerBody.Rotate(Vector3.up, mouseX);
                avatarHead.Rotate(Vector3.right, -mouseY);
            }
        }
    }
}
