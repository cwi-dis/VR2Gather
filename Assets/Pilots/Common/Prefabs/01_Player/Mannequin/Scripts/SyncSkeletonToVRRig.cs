using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncSkeletonToVRRig : MonoBehaviour
{
    [System.Serializable]
    public class VRMap
    {
        [Tooltip("The VRRig object that is being tracked")]
        public Transform vrTarget;
        [Tooltip("The skeleton constraint target that is updated")]
        public Transform rigTarget;

        public void Map()
        {
            rigTarget.position = vrTarget.position;
            rigTarget.rotation = vrTarget.rotation;
        }
    }
    [Tooltip("VRRig to Skeleton mapping for player head")]
    public VRMap head;
    [Tooltip("VRRig to Skeleton mapping for player neck")]
    public VRMap neck;
    [Tooltip("VRRig to Skeleton mapping for player left hand")]
    public VRMap leftHand;
    [Tooltip("VRRig to Skeleton mapping for player right hand")]
    public VRMap rightHand;
    [Tooltip("Player main object (tracks skeleton neck position in XZ but not Y")]
    public Transform playerTransform;
    [Tooltip("Skeleton head")]
    public Transform headConstraint;
    [Tooltip("Computed offset between skeleton head and main object")]
    public Vector3 headBodyOffset;
    //xxxshishir added transform variable to track the mannequin directly rather than the player
    [Tooltip("Mannequin body turn speed")]
    public float turnSmoothness = 5;
    [Tooltip("Mannequin transform")]
    public Transform mannequinTransform;

    // Start is called before the first frame update
    void Start()
    {
        AdjustHeight();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //xxxshishir trying out the new method from: https://blog.immersive-insiders.com/animate-avatar-for-vr-in-unity/, seems to work well
        mannequinTransform.position = headConstraint.position + headBodyOffset;
        mannequinTransform.forward = Vector3.Lerp(mannequinTransform.forward, Vector3.ProjectOnPlane(headConstraint.forward, Vector3.up).normalized, Time.deltaTime * turnSmoothness);
        head.Map();
        leftHand.Map();
        rightHand.Map();
    }

    public void AdjustHeight()
    {
        const bool heightOnly = true;
        headBodyOffset = playerTransform.position - headConstraint.position;
        if (heightOnly )
        {
            headBodyOffset.x = 0;
            headBodyOffset.z = 0;
        }
        Debug.Log($"SyncSkeletonToVRRig: {Time.frameCount}: headBodyOffset is now {headBodyOffset.y}");
    }
}
