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
            // Note by Jack: there is something to be said for allowing this behaviour also for HMD
            // users, *for some scenarios*. It may be useful (even though the usual caveats about
            // motion sickness when forcibly moving HMD users' viewpoint apply).
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
            {
                playerBody.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
            }
            else
            {
                transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
                playerBody.Rotate(Vector3.up, mouseX);
                avatarHead.Rotate(Vector3.right, -mouseY);
            }
            // Note by Jack: spectators and no-representation users should be able to move their viewpoint up and down.
            // with the current implementation all users have this ability, which may or may not be a good idea.
            float deltaHeight = Input.GetAxis("Mouse ScrollWheel");
            if (deltaHeight != 0)
            {
                playerBody.Translate(0, deltaHeight, 0);
            }
        }
    }
}
