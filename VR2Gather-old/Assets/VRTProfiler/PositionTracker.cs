using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if VRT_WITH_STATS
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

#if !VRT_WITH_STATS
        private void Start()
        {
            Debug.LogError($"{Name()}: VRT_WITH_STATS not enabled, making this a bit pointless");
        }
#else
        // Start is called before the first frame update
        void Start()
        {
            stats = new Stats(Name(), interval);
        }

        // Update is called once per frame
        void Update()
        {
            stats.statsUpdate(transform.position, transform.eulerAngles);
        }

        protected class Stats : Statistics
        {
            public Stats(string name, double interval) : base(name, interval)
            {
            }

            float px = 0, py = 0, pz = 0;
            float rx = 0, ry = 0, rz = 0;
            int count = 0;

            public void statsUpdate(Vector3 pos, Vector3 rot)
            {
                px += pos.x;
                py += pos.y;
                pz += pos.z;
                rx += rot.x;
                ry += rot.y;
                rz += rot.z;
                count++;
                if (ShouldOutput())
                {
                    Output($"px={px/count:f2}, py={py/count:f2}, pz={pz/count:f2}, rx={rx/count:f0}, ry={ry/count:f0}, rz={rz/count:f0}, count={count}");
                    Clear();
                    px = py = pz = 0;
                    rx = ry = rz = 0;
                    count = 0;
                }
            }
        }

        protected Stats stats;
#endif
    }
}