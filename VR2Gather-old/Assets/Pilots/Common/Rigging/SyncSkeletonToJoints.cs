using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Map one joint of rigging to one joint of position data (probably because it is tracking
/// some controller or skeleton capturer).
/// Either both vrTarget and rigTarget must be null or both must be non-null.
/// </summary>
[System.Serializable]
public class JointMap
{
    [Tooltip("Source of position/rotation data (probably a VR controller)")]
    public Transform vrTarget;
    [Tooltip("Destination of position/rotation data (probably some joint in the rigging constraints) ")]
    public Transform rigTarget;
    [Tooltip("Delta to add to source position before setting on destination")]
    public Vector3 trackingPositionOffset;
    [Tooltip("Delta to add to source rotation before setting on destination")]
    public Vector3 trackingRotationOffset;

    public void Map()
    {
        if (rigTarget == null && vrTarget == null) return;
        if (rigTarget == null || vrTarget == null)
        {
            Debug.LogError($"VRRig.Map: incomplete mapping: vrTarget={vrTarget} rigTarget={rigTarget}");
            return;
        }
        rigTarget.position = vrTarget.TransformPoint(trackingPositionOffset);
        rigTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }

}

/// <summary>
/// Component that modifies rigging contraints based on VR controllers (or captured skeletons)
/// before that rigging is applied. 
/// </summary>
public class SyncSkeletonToJoints : MonoBehaviour
{
    [Tooltip("The transform of the object to target(default this GameObject transform)")]
    public Transform targetTransform = null;
    [Tooltip("Source and destination of head position/rotation")]
    public JointMap head;
    [Tooltip("Source and destination of left hand position/rotation")]
    public JointMap leftHand;
    [Tooltip("Source and destination of right hand position/rotation")]
    public JointMap rightHand;
    [Tooltip("Source and destination of left knee position/rotation")]
    public JointMap leftKnee;
    [Tooltip("Source and destination of right knee position/rotation")]
    public JointMap rightKnee;
    [Tooltip("Source and destination of left foot position/rotation")]
    public JointMap leftFoot;
    [Tooltip("Source and destination of right foot position/rotation")]
    public JointMap rightFoot;

    [Tooltip("rig constraint of head")]
    public Transform headConstraint;
    [Tooltip("Offset of head (initialized during Start)")]
    public Vector3 headBodyOffset;
    //public K4AReader pc_reader;

    public float smothness;
    // Start is called before the first frame update
    void Start()
    {
        if (targetTransform == null) targetTransform = transform;
        headBodyOffset = targetTransform.position - headConstraint.position;
        
        //access the K4AReader
        //GameObject obj = this.transform.parent.gameObject;
        //PointCloudPipeline pc_pipeline = obj.GetComponentInChildren<PointCloudPipeline>();
        //pc_reader = (K4AReader)pc_pipeline.reader;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //if (pc_reader.has_skeleton())
        //{
        //    cwipc.cwipc_skeleton skl = pc_reader.mostRecentSkeleton;
        //    if (skl.joints.Count > 0)
        //    {
        //        cwipc.cwipc_skeleton_joint nose = skl.joints[27];
        //        float[] pos = nose.position;
        //        //transform.position = new Vector3(pos[0], pos[1], pos[2]);
        //        //transform.forward = Vector3.Lerp(transform.forward, Vector3.ProjectOnPlane(headConstraint.up, Vector3.up).normalized, Time.deltaTime * smothness);
        //    }

        //    transform.position = headConstraint.position + headBodyOffset;
        //    transform.forward = Vector3.Lerp(transform.forward, Vector3.ProjectOnPlane(headConstraint.up, Vector3.up).normalized, Time.deltaTime * smothness);

        //    head.Map();
        //    leftHand.Map();
        //    rightHand.Map();
        //}

        targetTransform.position = headConstraint.position + headBodyOffset;
        targetTransform.forward = Vector3.Lerp(targetTransform.forward, Vector3.ProjectOnPlane(headConstraint.up, Vector3.up).normalized, Time.deltaTime * smothness);

        head?.Map();
        leftHand?.Map();
        rightHand?.Map();
        leftKnee?.Map();
        rightKnee?.Map();
        leftFoot?.Map();
        rightFoot?.Map();
}
}
