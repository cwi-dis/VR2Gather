using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VRT.Profiler
{
    public class GameObjectProfiler : BaseProfiler
    {
        Transform target;
        string Objectname;
        List<Vector3> dataPosition = new List<Vector3>();
        List<Quaternion> dataRotation = new List<Quaternion>();

        public GameObjectProfiler(Transform target, string name)
        {
            this.target = target;
            Objectname = name;
        }

        public override void Flush()
        {
            dataPosition.Clear();
            dataRotation.Clear();
        }

        public override void AddFrameValues()
        {
            dataPosition.Add(target.position);
            dataRotation.Add(target.rotation);
        }

        public override void GetHeaders(StringBuilder sb)
        {
            sb.Append(Objectname + " PX;" + Objectname + " PY;" + Objectname + " PZ;" + Objectname + " RX;" + Objectname + " RY;" + Objectname + " RZ;");
        }

        public override void GetFramesValues(StringBuilder sb, int frame)
        {
            Vector3 pos = dataPosition[frame];
            Vector3 ang = dataRotation[frame].eulerAngles;
            sb.AppendFormat("{0:0.0000};{1:0.0000};{2:0.0000};{3:0.0000};{4:0.0000};{5:0.0000};", pos.x, pos.y, pos.z, ang.x, ang.y, ang.z);
        }
    }
}