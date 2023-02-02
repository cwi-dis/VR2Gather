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
    [Tooltip("VRRig to Skeleton mapping for player left hand")]
    public VRMap leftHand;
    [Tooltip("VRRig to Skeleton mapping for player right hand")]
    public VRMap rightHand;
    [Tooltip("Player main object (tracks skeleton head position in XZ but not Y")]
    public Transform playerTransform;
    [Tooltip("Skeleton head")]
    public Transform headConstraint;
    [Tooltip("Computed offset between skeleton head and main object")]
    public Vector3 headBodyOffset;
    [Tooltip("Computed rotation angle difference")]
    public float headRotationY;
    [Tooltip("If set: body follows head position")]
    public bool followPosition;
    [Tooltip("If set: body follows head rotation")]
    public bool followRotation;
    // Start is called before the first frame update
    void Start()
    {
        headBodyOffset = playerTransform.position - headConstraint.position;
        headRotationY = headConstraint.eulerAngles.y;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // xxxjack this is wrong. What we think we should do:
        // - Map camera rotation to skeleton head
        // - compute skeleton head position - player position
        // - update player position so skeleton head position matches camera position.
        // and think about other users.
        head.Map();
        leftHand.Map();
        rightHand.Map();
        if (followPosition)
        {
            playerTransform.position = headConstraint.position + headBodyOffset;
            headBodyOffset = playerTransform.position - headConstraint.position;
        }
        if (followRotation)
        {
            //transform.forward = Vector3.ProjectOnPlane(headConstraint.forward, Vector3.up).normalized;
            playerTransform.Rotate(0, -headRotationY, 0);
            headRotationY = headConstraint.eulerAngles.y;
        }
    }
}
