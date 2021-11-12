using UnityEngine;
using UnityEngine.XR;
using VRT.Core;

public class MoveCamera : MonoBehaviour {
    public string xAxisName = "Mouse X";
    public string yAxisName = "Mouse Y";
    public string heightAxisName = "Mouse ScrollWheel";
    public float xySensitivity = 100.0f;
    public float heightSensitivity = 0.05f; // 5 Centimeters
    public bool allowHJKLforMouse = true;
    public GameObject cameraToControl;
    public bool spectator = false;

    float xRotation = 0f;

    public Transform playerBody;
    public Transform avatarHead;

    void Awake() {
        if (!VRConfig.Instance.useHMD()) {
            cameraToControl.transform.localPosition = Vector3.up * Config.Instance.nonHMDHeight;
        }
    }

    void Start() {
        //Cursor.lockState = CursorLockMode.Confined;
    }

    void Update() {
        // If axis names are not specified we use the mouse
        if (xAxisName == "" && yAxisName == "" && heightAxisName == "")
        {
            float deltaHeight = Input.mouseScrollDelta.y;

            // Note by Jack: spectators and no-representation users should be able to move their viewpoint up and down.
            // with the current implementation all users have this ability, which may or may not be a good idea.
            if (deltaHeight != 0)
            {
                //Debug.Log($"MoveCamera: xxxjack deltaHeight={deltaHeight}");
                // Do Camera movement
                cameraToControl.transform.localPosition = new Vector3(
                    cameraToControl.transform.localPosition.x,
                    cameraToControl.transform.localPosition.y + deltaHeight * heightSensitivity,
                    cameraToControl.transform.localPosition.z);
            }


            // Camera Rotation
            if (Input.GetKey(KeyCode.Mouse0))
            {
                float mouseX = Input.GetAxis("Mouse X") * xySensitivity * Time.deltaTime;
                float mouseY = Input.GetAxis("Mouse Y") * xySensitivity * Time.deltaTime;

                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                cameraToControl.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

                if (!spectator)
                {
                    playerBody.Rotate(Vector3.up, mouseX);
                    avatarHead.Rotate(Vector3.right, -mouseY);
                }
            }
            if (allowHJKLforMouse)
            {
                // Use HJKL keys to simulate mouse movement, mainly for debugging
                // because mouse doesn't work over screen sharing connections.
                float hAngle = 0;
                float vAngle = 0;
                if (Input.GetKey(KeyCode.J)) hAngle = 5;
                if (Input.GetKey(KeyCode.L)) hAngle = -5;
                if (Input.GetKey(KeyCode.I)) vAngle = -5;
                if (Input.GetKey(KeyCode.K)) vAngle = 5;
                if (hAngle != 0 || vAngle != 0)
                {
                    xRotation += vAngle;
                    xRotation = Mathf.Clamp(xRotation, -90f, 90f);

                    cameraToControl.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

                    if (!spectator)
                    {
                        playerBody.Rotate(Vector3.up, -hAngle);
                        avatarHead.Rotate(Vector3.right, vAngle);
                    }

                }
            }
        }
        else
        {
            // If axis names are specified we are (probably) using a gamepad
            float mouseX = Input.GetAxis(xAxisName) * xySensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis(yAxisName) * xySensitivity * Time.deltaTime;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            cameraToControl.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            if (!spectator)
            {
                playerBody.Rotate(Vector3.up, mouseX);
                avatarHead.Rotate(Vector3.right, -mouseY);
            }
        }
        // UpDown Movement
       
        // Joystick cameraToControl Rotation
        if(VRConfig.Instance.useControllerGamepad())
        {

            
        }
        // Keyboard Camera rotation
        {
 
        }

    }
}
