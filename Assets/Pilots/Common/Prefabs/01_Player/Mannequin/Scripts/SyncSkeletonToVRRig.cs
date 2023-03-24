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
        [Tooltip("The skeleton constraint that corresponds to the vrTarget (default: rigTarget)")]
        public Transform rigSource;
        [Tooltip("Only map position, not rotation")]
        public bool positionOnly = false;
        [Tooltip("But do map Y rotation")]
        public bool includeYRotation = false;

        public void Map()
        {
            if (rigSource == null) rigSource = rigTarget;
            Vector3 delta = vrTarget.position - rigSource.position;
            rigTarget.position += delta;
            if (!positionOnly)
            {
                rigTarget.rotation = vrTarget.rotation;
            } else if (includeYRotation)
            {
                Vector3 rot = rigTarget.rotation.eulerAngles;
                rot.y = vrTarget.rotation.eulerAngles.y;
                rigTarget.rotation = Quaternion.Euler(rot);
            }
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
    [Tooltip("Mannequin body turn speed")]
    public float turnSmoothness = 5;
    [Tooltip("Mannequin transform")]
    public Transform mannequinTransform;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void LateUpdate()
    {
       head.Map();
        neck.Map();
        leftHand.Map();
        rightHand.Map();
         //xxxshishir trying out the new method from: https://blog.immersive-insiders.com/animate-avatar-for-vr-in-unity/, seems to work well
        mannequinTransform.forward = Vector3.Lerp(mannequinTransform.forward, Vector3.ProjectOnPlane(head.rigTarget.forward, Vector3.up).normalized, Time.deltaTime * turnSmoothness);
  }

}
