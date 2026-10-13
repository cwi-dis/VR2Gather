using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps a leg IK target (the ankle) at its rest height above the floor, so the feet stay on
/// the floor when the avatar body moves down (crouching) and the IK bends the knees.
/// Horizontally the target keeps following the avatar through the hierarchy.
///
/// The floor is the origin of RootObject: the representation root, which sits at the player root
/// (where the CharacterController stands). The rest height is measured in RootObject's local space
/// at Awake(), so it scales along when SizeAdjust scales the representation.
/// </summary>
public class KeepFeetAboveGround : MonoBehaviour
{
    [Tooltip("Foot is kept at its rest height above the origin of this object")]
    public GameObject RootObject;
    [Tooltip("Print logging message if position adjusted")]
    [SerializeField] bool debug = false;

    float restHeight;

    void Awake()
    {
        restHeight = RootObject.transform.InverseTransformPoint(transform.position).y;
    }

    // Update, so the target is in place before the animation rigging evaluates
    void Update()
    {
        float floorY = RootObject.transform.TransformPoint(0, restHeight, 0).y;
        float delta = floorY - transform.position.y;
        if (delta != 0)
        {
            if (debug) Debug.Log($"KeepFeetAboveGround {name}: {Time.frameCount}: moving {delta:F3} to y={floorY:F3}");
            transform.Translate(0, delta, 0, Space.World);
        }
    }
}
