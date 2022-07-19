using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VRT.Profiler
{
    public class BaseProfiler
    {
        public virtual void Flush() { }
        public virtual void AddFrameValues() { }
        public virtual void GetHeaders(StringBuilder sb) { }
        public virtual void GetFramesValues(StringBuilder sb, int frame) { }
    }
}