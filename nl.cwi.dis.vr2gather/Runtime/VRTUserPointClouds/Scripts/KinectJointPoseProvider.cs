using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.Experimental.XR.Interaction;
using Cwipc;
using cwipc_skeleton = Cwipc.SkeletonSupport.cwipc_skeleton;
using cwipc_skeleton_joint = Cwipc.SkeletonSupport.cwipc_skeleton_joint;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif
using VRT.UserRepresentation.PointCloud;

public class KinectJointPoseProvider : BasePoseProvider
{
    [Tooltip("GameObject we search for K4AReader")]
    public GameObject pcPipelineParent;
    [Tooltip("K4AReader used (initialized from previous)")]
    public ISkeletonPointCloudReader pc_reader;
    [Tooltip("Joint index (which this BasePoseProvider will follow)")]
    public cwipc_skeleton.JointIndex jointIndex;
    private Vector3 previousPosition;
    private Vector3 currentPosition;
    private Quaternion currentOrientation;
    public virtual string Name()
    {
        return $"{GetType().Name}.{jointIndex}";
    }

    // Start is called before the first frame update

    void Start()
    {
#if VRT_WITH_STATS
        stats = new Stats(Name());
#endif
        GameObject obj = this.transform.parent.gameObject;
        PointCloudPipelineSelf pc_pipeline = pcPipelineParent?.GetComponentInChildren<PointCloudPipelineSelf>();
        if (pc_pipeline == null)
        {
            Debug.Log($"{Name()}: no pointcloud pipeline, disabling GameObject {gameObject.name}");
            gameObject.SetActive(false);
            return;
        }
        pc_reader = pc_pipeline.reader as AsyncKinectReader;
        if (pc_reader == null || !pc_reader.supports_skeleton())
        {
            Debug.LogWarning($"{Name()}: no skeleton support in pc_reader, disabling GameObject {gameObject.name}");
            gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (pc_reader.has_skeleton())
        {
            cwipc_skeleton skl = pc_reader.get_skeleton();
            if (skl.joints.Count > 0)
            {
#if VRT_WITH_STATS
                stats.statsUpdate(true, skl.joints.Count);
#endif
                cwipc_skeleton_joint jointData = skl.joints[(int)jointIndex];
                Vector3 pos = jointData.position;
                Quaternion rot = jointData.orientation;
                Debug.Log($"xxxjack AccessKinectJoint: joint={jointIndex}, pos={pos}");

                //not sure for this
                currentPosition= new Vector3(-pos.x, pos.y, pos.z);
                currentOrientation = rot;
                this.transform.localPosition = currentPosition;
                //this.transform.localPosition = Vector3.Lerp(currentPosition, previousPosition, Time.deltaTime * 5.0f);

                //previousPosition = this.transform.localPosition;
            }
        } 
        else
        {
#if VRT_WITH_STATS
            stats.statsUpdate(false, 0);
#endif
        }
    }

    public override PoseDataFlags GetPoseFromProvider(out Pose output)
    {
        output = new Pose(currentPosition, currentOrientation);
        return PoseDataFlags.Position | PoseDataFlags.Rotation;
    }
#if VRT_WITH_STATS
 
    protected class Stats : Statistics
    {
        public Stats(string name) : base(name) { }

        double statsTotalSkeletons = 0;
        double statsTotalNoSkeletons = 0;
        double statsTotalJoints = 0;
       
        public void statsUpdate(bool hasSkeleton, int nJoints)
        {
            if (hasSkeleton)
            {
                statsTotalSkeletons++;
                statsTotalJoints += nJoints;
            }
            else
            {
                statsTotalNoSkeletons++;
            }
            if (ShouldOutput())
            {
                double factor = statsTotalSkeletons == 0 ? 1 : statsTotalSkeletons;
                Output($"fps_skeleton={statsTotalSkeletons / Interval():F2}, joints_per_skeleton={(int)(statsTotalJoints / factor)}, fps_noskeleton={statsTotalNoSkeletons / Interval():F2}");
                Clear();
                statsTotalSkeletons = 0;
                statsTotalNoSkeletons = 0;
                statsTotalJoints = 0;
             }
        }
    }

    protected Stats stats;
#endif
}
