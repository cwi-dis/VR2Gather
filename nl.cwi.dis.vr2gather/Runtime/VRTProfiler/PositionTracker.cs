using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !VRT_WITHOUT_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Profiler
{
    public class PositionTracker : MonoBehaviour
    {
        public double interval = 0;
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public string Name()
        {
            return $"{GetType().Name}#{transform.parent.gameObject.name}.{instanceNumber}";
        }

#if VRT_WITHOUT_STATS
        private void Start()
        {
            Debug.LogError($"{Name()}: VRT_WITHOUT_STATS defined, making this a bit pointless");
        }
#else
        // Its own timing and the static Statistics.Output(), not a Statistics subclass: the
        // Inspector field interval stays per component (#451). 0: a record on every update,
        // > 0: every so many seconds, < 0: only when destroyed. Always a last record when
        // destroyed, with what was added up since the previous one.
        string statsName;
        System.DateTime lastOutput;
        float px = 0, py = 0, pz = 0;
        float rx = 0, ry = 0, rz = 0;
        int count = 0;

        // Start is called before the first frame update
        void Start()
        {
            // Here: when destroyed, the parent may be gone already.
            statsName = Name();
            lastOutput = System.DateTime.Now;
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 pos = transform.position;
            Vector3 rot = transform.eulerAngles;
            px += pos.x;
            py += pos.y;
            pz += pos.z;
            rx += rot.x;
            ry += rot.y;
            rz += rot.z;
            count++;
            if (interval >= 0 && (System.DateTime.Now - lastOutput).TotalSeconds >= interval)
            {
                OutputRecord();
            }
        }

        void OnDestroy()
        {
            if (count > 0) OutputRecord();
        }

        void OutputRecord()
        {
            Statistics.Output(statsName, $"px={Statistics.Num(px / count)}, py={Statistics.Num(py / count)}, pz={Statistics.Num(pz / count)}, rx={Statistics.Num(rx / count, 0)}, ry={Statistics.Num(ry / count, 0)}, rz={Statistics.Num(rz / count, 0)}, count={count}");
            lastOutput = System.DateTime.Now;
            px = py = pz = 0;
            rx = ry = rz = 0;
            count = 0;
        }
#endif
    }
}