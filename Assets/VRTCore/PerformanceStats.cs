using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace VRT.Core
{
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

 
        protected class Stats : BaseStats
        {
            public Stats(string name) : base(name)
            {
                myProcess = Process.GetCurrentProcess();
            }

            Process myProcess;
            double prevCpu = 0;

            public void statsUpdate()
            {
                if (ShouldOutput())
                {
                    myProcess.Refresh();
                    double cpu = myProcess.TotalProcessorTime.TotalSeconds;
                    long memory = myProcess.VirtualMemorySize64;
                    
                    Output($"cpu={(cpu-prevCpu)/Interval()}, memory={memory}");
                }
                if (ShouldClear())
                {
                    Clear();
                    prevCpu = myProcess.TotalProcessorTime.TotalSeconds;
                }
            }
        }

        protected Stats stats;
    }
}