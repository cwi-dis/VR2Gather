using UnityEngine;
using UnityEngine.XR;
using VRT.Core;

public class MoveCamera : MonoBehaviour {
    public float mouseSensitivity = 100.0f;
    public float joystickSensitivity = 100.0f;
    public GameObject camera;
    public float wheelSlope = 0.05f; // 5 Centimeters
    public bool spectator = false;
    float xRotation = 0f;

    public Transform playerBody;
    public Transform avatarHead;

    void Awake() {
        if (XRUtility.isPresent() ) {
            enabled = false;
            return;
        }
        // xxxjack this is a strange location to initialize non-HMD camera height.
        // Because it really depends on hmd/non HMD, not on which input device is used for
        // navigation.
        camera.transform.localPosition = Vector3.up * Config.Instance.nonHMDHeight;
        if (Config.Instance.VR.disableKeyboardMouse)
        {
            enabled = false;
        }
    }

    void Start() {
        //Cursor.lockState = CursorLockMode.Confined;
    }

    void Update() {
        // UpDown Movement
        float deltaHeight = Input.mouseScrollDelta.y;

        // Note by Jack: spectators and no-representation users should be able to move their viewpoint up and down.
        // with the current implementation all users have this ability, which may or may not be a good idea.
        if (deltaHeight != 0) {
            //Debug.Log($"MoveCamera: xxxjack deltaHeight={deltaHeight}");
            // Do Camera movement
            camera.transform.localPosition = new Vector3(camera.transform.localPosition.x, camera.transform.localPosition.y + deltaHeight * wheelSlope, camera.transform.localPosition.z);
        }


        // Camera Rotation
        if (Input.GetKey(KeyCode.Mouse0)) {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            if (!spectator) {
                playerBody.Rotate(Vector3.up, mouseX);
                avatarHead.Rotate(Vector3.right, -mouseY);
            }
        }
        // Joystick Camera Rotation
        if(Config.Instance.allowControllerMovement)
        {
            float mouseX = Input.GetAxis("JoystickRightThumbstickLeftRight") * joystickSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("JoystickRightThumbstickUpDown") * joystickSensitivity * Time.deltaTime;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            if (!spectator)
            {
                playerBody.Rotate(Vector3.up, mouseX);
                avatarHead.Rotate(Vector3.right, -mouseY);
            }
            
        }
        // Keyboard Camera rotation
        {
            float hAngle = 0;
            float vAngle = 0;
            if (Input.GetKey(KeyCode.J)) hAngle = 5;
            if (Input.GetKey(KeyCode.L)) hAngle = -5;
            if (Input.GetKey(KeyCode.I)) vAngle = -5;
            if (Input.GetKey(KeyCode.K)) vAngle = 5;
            if (hAngle != 0 || vAngle != 0) {
                xRotation += vAngle;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

                if (!spectator)
                {
                    playerBody.Rotate(Vector3.up, -hAngle);
                    avatarHead.Rotate(Vector3.right, vAngle);
                }

            }
        }

    }
}
