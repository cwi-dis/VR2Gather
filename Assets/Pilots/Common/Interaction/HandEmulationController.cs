using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;

//
// This script emulates HandController for use when you have no HMD or controllers,
// only keyboard and mouse.
// When you press shift you will see an indication whether the object under the mouse is
// touchable. If so you can press left-click and touch it.
//
// Grabbing not implemented, because it doesn't seem to useful (without hands). But doable
// if we want to.
//
public class HandEmulationController : MonoBehaviour
{
    [Tooltip("Mouse cursor to use while looking for touchable items")]
    public Texture2D gropingCursorTexture;
    [Tooltip("Mouse cursor to use when over a touchable item")]
    public Texture2D touchingCursorTexture;
    [Tooltip("Maximum distance of touchable objects")]
    public float maxDistance = Mathf.Infinity;
    [Tooltip("Key to press to start looking for touchable items")]
    public KeyCode gropeKey = KeyCode.LeftShift;
    [Tooltip("Key to press to touch an item")]
    public KeyCode touchKey = KeyCode.Mouse0;
    [Tooltip("Auto-center mouse, to allow use with gamepads")]
    public bool autoCenterMouse = false;
    [Tooltip("Collider that actually presses the button")]
    public Collider touchCollider = new SphereCollider();
    protected bool isGroping;
    protected bool isTouching;
    // Start is called before the first frame update
    void Start()
    {
        if (!VRConfig.Instance.useControllerEmulation())
        {
            enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool isGropingingNow = Input.GetKey(gropeKey);
        if (isGroping != isGropingingNow)
        {
            if (autoCenterMouse)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            isGroping = isGropingingNow;
            if (isGroping)
            {
                if (autoCenterMouse)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
                Cursor.SetCursor(gropingCursorTexture, Vector2.zero, CursorMode.Auto);
                touchCollider.enabled = false;
                isTouching = false;
            } else
            {
                if (autoCenterMouse)
                {
                    Cursor.lockState = CursorLockMode.None;
                }
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                touchCollider.enabled = false;
            }
        }
        if (!isGroping) return;

        //
        // Check whether we are hitting any elegible object
        //
        bool isHittingNow = false;
        int layerMask = LayerMask.GetMask("TouchableObject");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, maxDistance, layerMask))
        {
            //Debug.Log($"xxxjack mouse-hit {hit.collider.gameObject.name}");
            isHittingNow = true;
        }
        if (isTouching != isHittingNow)
        {
            isTouching = isHittingNow;
            if (isTouching)
            {
                Cursor.SetCursor(touchingCursorTexture, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(gropingCursorTexture, Vector2.zero, CursorMode.Auto);
            }
        }
        if (!isTouching)
        {
            touchCollider.enabled = false;
            return;
        }
        //
        // Now check whether the left mouse is clicked and perform the action.
        //
        if (Input.GetKey(touchKey))
        {
            GameObject objHit = hit.collider.gameObject;
            Debug.Log($"xxxjack Moving touchCollider to {objHit.name} at {objHit.transform.position}");
            touchCollider.enabled = true;
            touchCollider.transform.position = hit.collider.transform.position;
        }
        if (Input.GetKeyUp(touchKey))
        {
            touchCollider.enabled = false;
        }
    }
}
