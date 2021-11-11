using UnityEngine;
using UnityEngine.XR;
using VRT.Core;

public class PlayerMovement : MonoBehaviour {

    public float speed = 5f;
    public CharacterController controller;

    void Awake() {
        if (!VRConfig.Instance.useControllerEmulation())
        {
            enabled = false;
        }
    }

    // Update is called once per frame
    void Update() {
#if XXXJACK_DISABLED_BECAUSE_IT_SHOULD_NOT_BE_NEEDED_ANYMORE
        if (!gameObject.GetComponentInParent<PlayerManager>().cam.gameObject.activeSelf)
            enabled = false; // Check if it's the active/your player
#endif
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);
    }
}
