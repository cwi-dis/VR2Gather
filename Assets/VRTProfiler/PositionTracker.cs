using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Core
{
    public class PositionTracker : MonoBehaviour
    {
        static int instanceCounter = 0;
        int instanceNumber = instanceCounter++;

        public string Name()
        {
            return $"{GetType().Name}#{instanceNumber}";
        }

        // Start is called before the first frame update
        void Start()
        {
            stats = new Stats(Name());
        }

        // Update is called once per frame
        void Update()
        {
            stats.statsUpdate(transform.position, transform.eulerAngles);
        }

        protected class Stats : BaseStats
        {
            public Stats(string name) : base(name)
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
                    Output($"px={px/count}, py={py/count}, pz={pz/count}, rx={rx/count}, ry={ry/count}, rz={rz/count}, count={count}");
                }
                if (ShouldClear())
                {
                    Clear();
                    px = py = pz = 0;
                    rx = ry = rz = 0;
                    count = 0;
                }
            }
        }

        protected Stats stats;
    }
}