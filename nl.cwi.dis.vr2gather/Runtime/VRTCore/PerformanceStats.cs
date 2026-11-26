using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
#if !VRT_WITHOUT_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Core
{
#if !VRT_WITHOUT_STATS
    public class PerformanceStats : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            stats = new Stats("PerformanceStats");
        }
  
        // Update is called once per frame
        void Update()
        {
            stats.statsUpdate();
        }

        void OnDestroy()
        {
            stats?.Final();
        }
 
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name)
            {
                myProcess = Process.GetCurrentProcess();
                // Here, not in the first record: with final-only that is the only record, and it
                // would always say cpu=0 (#451).
                prevCpu = myProcess.TotalProcessorTime.TotalSeconds;
            }

            Process myProcess;
            double prevCpu;

            public void statsUpdate()
            {
                lock (statsLock)
                {
                    MaybeOutput();
                }
            }

            protected override void OutputAggregate()
            {
                myProcess.Refresh();
                double cpu = myProcess.TotalProcessorTime.TotalSeconds;
                long memory = myProcess.VirtualMemorySize64;
                // cpu: processor seconds per second over the interval of this record.
                OutputFields(
                    Rate("cpu", cpu - prevCpu, "F3"),
                    Field("memory", memory));
                prevCpu = cpu;
            }
        }

        protected Stats stats;
    }
#endif
}