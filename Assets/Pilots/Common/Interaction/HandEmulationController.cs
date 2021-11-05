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
    public Texture2D castingCursorTexture;
    public Texture2D castingCursorHitTexture;
    public float maxDistance = Mathf.Infinity;
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
        bool isCastingNow = Input.GetKey(KeyCode.LeftShift);
        if (isCasting != isCastingNow)
        {
            isCasting = isCastingNow;
            if (isCasting)
            {
                Cursor.SetCursor(castingCursorTexture, Vector2.zero, CursorMode.Auto);
                isHitting = false;
            } else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
        if (!isCasting) return;

        //
        // Check whether we are hitting any elegible object
        //
        bool isHittingNow = false;
        int layerMask = LayerMask.GetMask("TouchableObject") ^ Physics.DefaultRaycastLayers;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit = new RaycastHit();
        // xxxjack Unfortunately passing maxDistance seems to work more like minDistance?
        if (Physics.Raycast(ray, out hit, maxDistance, layerMask))
        {
            Debug.Log($"xxxjack mouse-hit {hit.collider.gameObject.name}");
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
        if (!isHitting) return;
        //
        // Now check whether the left mouse is clicked and perform the action.
        //
    }
}
