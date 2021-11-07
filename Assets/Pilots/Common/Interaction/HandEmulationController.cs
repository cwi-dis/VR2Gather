using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//
// This script emulates HandController for use when you have no HMD or controllers,
// only keyboard and mouse.
// When you press shift you will see an indication whether the object under the mouse is
// pressable or grabbable. If so you can press with left-click and grab with right-click.
//
public class HandEmulationController : MonoBehaviour
{
    [Tooltip("Mouse cursor to use while looking for touchable items")]
    public Texture2D castingCursorTexture;
    [Tooltip("Mouse cursor to use when over a touchable item")]
    public Texture2D castingCursorHitTexture;
    [Tooltip("Maximum distance of touchable objects")]
    public float maxDistance = Mathf.Infinity;
    [Tooltip("Key to press to start looking for touchable items")]
    public KeyCode castKey = KeyCode.LeftShift;
    [Tooltip("Key to press to touch an item")]
    public KeyCode touchKey = KeyCode.Mouse0;
    [Tooltip("Collider that actually presses the button")]
    public Collider touchCollider = new SphereCollider();
    protected bool isCasting;
    protected bool isHitting;
    // Start is called before the first frame update
    void Start()
    {
        if (XRUtility.isPresent())
        {
            enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        bool isCastingNow = Input.GetKey(castKey);
        if (isCasting != isCastingNow)
        {
            isCasting = isCastingNow;
            if (isCasting)
            {
                Cursor.SetCursor(castingCursorTexture, Vector2.zero, CursorMode.Auto);
                touchCollider.enabled = false;
                isHitting = false;
            } else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                touchCollider.enabled = false;
            }
        }
        if (!isCasting) return;

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
        if (isHitting != isHittingNow)
        {
            isHitting = isHittingNow;
            if (isHitting)
            {
                Cursor.SetCursor(castingCursorHitTexture, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(castingCursorTexture, Vector2.zero, CursorMode.Auto);
            }
        }
        if (!isHitting)
        {
            touchCollider.enabled = false;
            return;
        }
        //
        // Now check whether the left mouse is clicked and perform the action.
        //
        if (Input.GetKeyDown(touchKey))
        {
            GameObject objHit = hit.collider.gameObject;
            Debug.Log($"xxxjack Should do something to {objHit.name} at {objHit.transform.position}");
            touchCollider.enabled = true;
            touchCollider.transform.position = objHit.transform.position;
        }
        if (Input.GetKeyUp(touchKey))
        {
            touchCollider.enabled = false;
        }
    }
}
