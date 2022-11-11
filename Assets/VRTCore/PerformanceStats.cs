using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Core
{
#if VRT_WITH_STATS
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

 
        protected class Stats : Statistics
        {
            public Stats(string name) : base(name)
            {
                myProcess = Process.GetCurrentProcess();
            }

            Process myProcess;
            double prevCpu = -1;

            public void statsUpdate()
            {
                if (ShouldOutput())
                {
                    myProcess.Refresh();
                    double cpu = myProcess.TotalProcessorTime.TotalSeconds;
                    if (prevCpu < 0) prevCpu = cpu; // Stop-gap for extremely large value at beginning
                    long memory = myProcess.VirtualMemorySize64;
                    
                    Output($"cpu={(cpu-prevCpu)/Interval()}, memory={memory}");
                    Clear();
                    prevCpu = myProcess.TotalProcessorTime.TotalSeconds;
                }
            }
        }

        protected Stats stats;
    }
#endif
}