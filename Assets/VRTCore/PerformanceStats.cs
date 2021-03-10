using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace VRT.Core
{
    public class PerformanceStats : MonoBehaviour
    {
        System.Threading.Thread thread;
        public bool isRunning { get; private set; }

        [Tooltip("Interval in ms for performance measurements")]
        public int interval = 1000;

        // Start is called before the first frame update
        void Start()
        {
            stats = new Stats("PerformanceStats");
            thread = new System.Threading.Thread(new System.Threading.ThreadStart(_Update));
            thread.Name = "PerformanceStats";
            isRunning = true;
            thread.Start();
        }
        public virtual void OnDestroy()
        {
            isRunning = false;
        }

        // Update is called once per frame
        void Update()
        {
            stats.statsUpdate();
        }

        void _Update()
        {
            while(isRunning)
            {
                stats.statsUpdate();
                System.Threading.Thread.Sleep(interval);
            }
        }

        protected class Stats : BaseStats
        {
            public Stats(string name) : base(name)
            {
                totalCpuCounter = new PerformanceCounter("Process", "% Processor Time", "_Total");
                myProcess = Process.GetCurrentProcess();
                cpuCounter = new PerformanceCounter("Process", "% Processor Time", myProcess.ProcessName);
                ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            }

            Process myProcess;
            PerformanceCounter totalCpuCounter;
            PerformanceCounter cpuCounter;
            PerformanceCounter ramCounter;

            public void statsUpdate()
            {

                if (ShouldOutput())
                {
                    float totalCpu = totalCpuCounter.NextValue();
                    double cpu = myProcess.TotalProcessorTime.TotalSeconds;
                    long memory = myProcess.WorkingSet64;
                    double received = 0;
                    double sent = 0;
                    Output($"total_cpu={totalCpu}, cpu={cpu}, memory={memory}, received={received}, sent={sent}");
                }
                if (ShouldClear())
                {
                    Clear();
                }
            }
        }

        protected Stats stats;
    }
}