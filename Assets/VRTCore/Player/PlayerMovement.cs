using UnityEngine;
using UnityEngine.XR;

public class PlayerMovement : MonoBehaviour {

    public float speed = 5f;
    public CharacterController controller;

    void Awake() {
        if (XRUtility.isPresent())
            enabled = false; // Check if you're wearing an HMD
    }

    // Update is called once per frame
    void Update() {
        if (!gameObject.GetComponentInParent<PlayerManager>().cam.gameObject.activeSelf)
            enabled = false; // Check if it's the active/your player
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);
    }
}
