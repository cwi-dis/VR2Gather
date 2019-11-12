using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class PositionProfilerHelper : MonoBehaviour {
    public class PositionProfiler : ProfilerManager.BaseProfiler
    {
        Transform target;
        public PositionProfiler(Transform target) {
            this.target = target;
        }

        List<Vector3> dataPosition = new List<Vector3>();
        List<Quaternion> dataRotation = new List<Quaternion>();
        public override void Flush()
        {
            dataPosition.Clear();
            dataRotation.Clear();
        }

        public override void AddFrameValues() {
            dataPosition.Add(target.position);
            dataRotation.Add(target.rotation);
        }
        public override void GetHeaders(StringBuilder sb) { sb.Append( "HMDPX;HMDPY;HMDPZ;HMDRX;HMDRY;HMDRZ;"); }
        public override void GetFramesValues(StringBuilder sb, int frame) {
            Vector3 pos = dataPosition[frame];
            Vector3 ang = dataRotation[frame].eulerAngles;
            sb.AppendFormat("{0:0.0000};{1:0.0000};{2:0.0000};{3:0.0000};{4:0.0000};{5:0.0000};", pos.x, pos.y, pos.z, ang.x, ang.y, ang.z);
        }
    }

    void Start () {
        ProfilerManager.Instance.AddProfiler(new PositionProfiler(transform));

    }
}
