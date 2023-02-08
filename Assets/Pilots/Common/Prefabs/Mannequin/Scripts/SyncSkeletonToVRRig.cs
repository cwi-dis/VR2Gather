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
        //xxxshishir added new temporary functions to test mannequin movement - will remove these functions one the mannequin behaves correctly
        public void MapPositionOnly()
        {
            rigTarget.position = vrTarget.position;
        }
        public void MapRotationOnly()
        {
            rigTarget.rotation = vrTarget.rotation;
        }
        public void ManualMap(Transform newTarget)
        {
            rigTarget.position = newTarget.position;
            rigTarget.rotation = newTarget.rotation;
        }
        public void ManualMapPosition(Vector3 newTargetPosition)
        {
            rigTarget.position = newTargetPosition;
        }
        public void ManualMapRotation(Quaternion newTargetRotation)
        {
            rigTarget.rotation = newTargetRotation;
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
    //xxxshishir addition debug variable to track camera movement
    [Tooltip("Camera transform")]
    public Transform cameraTransform;
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

        //xxxshishir initial approach - movement is too fast if position is tracked and rotation is in the opposite direction if rotation is tracked
        //head.Map();
        //leftHand.Map();
        //rightHand.Map();

        //if (followRotation)
        //{
        //    //transform.forward = Vector3.ProjectOnPlane(headConstraint.forward, Vector3.up).normalized;
        //    playerTransform.Rotate(0, -headRotationY, 0);
        //    headRotationY = headConstraint.eulerAngles.y;
        //}
        //if (followPosition)
        //{
        //    playerTransform.position = headConstraint.position + headBodyOffset;
        //    headBodyOffset = playerTransform.position - headConstraint.position;
        //}

        //xxxshishir trying out new approach based on jack's previous comment - this still doesn't work properly - movement is at the right speed but inverted
        head.ManualMapRotation(cameraTransform.rotation);

        headBodyOffset = playerTransform.position - headConstraint.position;
        var newPlayerPosition = headConstraint.position - cameraTransform.position;
        if(followPosition)
        {
            playerTransform.position = playerTransform.position + newPlayerPosition;
        }
        head.MapPositionOnly();
        if(followRotation)
        {
            playerTransform.rotation = cameraTransform.rotation;
        }
        //head.MapPositionOnly();
        leftHand.Map();
        rightHand.Map();

    }
}
