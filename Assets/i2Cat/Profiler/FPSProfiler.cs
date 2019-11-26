using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class FPSProfiler : BaseProfiler {
    List<Vector2> data = new List<Vector2>();

    public override void Flush() { 
        data.Clear();
    }

    public override void AddFrameValues() {
        data.Add(new Vector2(Time.frameCount, Time.frameCount / Time.time));
    }

    public override void GetHeaders(StringBuilder sb) { 
        sb.Append("FRAME;FPS;"); 
    }

    public override void GetFramesValues(StringBuilder sb, int frame) {
        sb.AppendFormat("{0};{1:0.00};", (int)data[frame].x, data[frame].y);
    }
}
