using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VRT.UserRepresentation.TVM;

namespace VRT.Profiler
{
    public class TVMProfiler : BaseProfiler
    {
        GameObject[] tvm;
        List<int[]> dataTVM;

        public TVMProfiler(GameObject[] tvms)
        {
            tvm = tvms;
            dataTVM = new List<int[]>();
        }

        public override void Flush()
        {
            dataTVM.Clear();
        }

        public override void AddFrameValues()
        {
            int[] tvmFrame = new int[4];
            for (int i = 0; i < tvm.Length; ++i)
            {
                tvmFrame[i] = tvm[i].GetComponent<MeshConstructor>().fps;
                tvm[i].GetComponent<MeshConstructor>().fps = 0;
            }
            dataTVM.Add(tvmFrame);
        }

        public override void GetHeaders(StringBuilder sb)
        {
            int i = 0;
            foreach (GameObject t in tvm)
            {
                sb.Append(string.Format("TVM {0} FPS;", ++i));
            }
        }

        public override void GetFramesValues(StringBuilder sb, int frame)
        {
            int[] data = dataTVM[frame];
            int i = 0;
            foreach (GameObject t in tvm)
            {
                sb.AppendFormat("{0:0.0000};", data[i++]);
            }
        }

    }
}