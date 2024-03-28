using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using VRT.Core;

namespace VRT.Profiler
{
    public class ProfilerManager : MonoBehaviour
    {
        
        private string fileName = "Profiler";
        private string csvOutputPathname;
        private float SamplingRate = 1 / 30f;
        private bool HMDActive = false;
        private bool printedStatMessage = false;
        private float timeToNext = 0.0f;
        private uint lineCount = 0;
        private Transform HMD;
        private Transform LH;
        private Transform RH;
        private bool headerWritten = false;
        private float logInterval = 10;
        private float lastLogWriteTime;
        public static ProfilerManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            HMDActive = false; // xxxjack: this needs to be fixed (it depended on pilot3logs which was also silly)
            csvOutputPathname = string.Format("{0}/../{1}.csv", Application.persistentDataPath, fileName);
        }

        List<BaseProfiler> profiles = new List<BaseProfiler>();

        public void AddProfiler(BaseProfiler profiler)
        {
            profiles.Add(profiler);
            foreach (var profile in profiles)
                profile.Flush();
            timeToNext = 0;
            lineCount = 0;
        }

        // Use this for initialization
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            // xxxjack should bail out quickly if no profilers active
            if (!printedStatMessage)
            {
                //VRT.Core.BaseStats.Output("ProfilerManager", $"started=1, csv_output={csvOutputPathname}, TimeSinceGameStart={Time.time}, HMDActive={HMDActive}, TVMActive={TVMActive}");
                printedStatMessage = true;
            }
            if (HMDActive == true)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    HMD = cam.transform;
                    AddProfiler(new FPSProfiler());
                    AddProfiler(new HMDProfiler(HMD));
                    HMDActive = false;
                    lastLogWriteTime = Time.time;
                }
                var h1 = GameObject.Find("PFB_LeftHand");
                if (h1 != null)
                {
                    LH = h1.transform;
                    AddProfiler(new GameObjectProfiler(LH, "LeftHandController"));
                }
                var h2 = GameObject.Find("PFB_RightHand");
                if (h2 != null)
                {
                    RH = h2.transform;
                    AddProfiler(new GameObjectProfiler(RH, "RightHandController"));
                }
            }
            if (Time.time > 0)
            {
                timeToNext -= Time.deltaTime;
                if (timeToNext < 0.0f)
                {
                    timeToNext += SamplingRate;
                    foreach (var profile in profiles)
                        profile.AddFrameValues();
                    lineCount++;
                }
            }
            //XXXShishir modified to write logs at a specified interval if profilers are active
            if (Time.time - lastLogWriteTime >= logInterval && profiles.Count > 0)
            {
                savelog();
            }
        }

        private void savelog()
        {
            lastLogWriteTime = Time.time;
            StringBuilder sb = new StringBuilder();
            //VRT.Core.BaseStats.Output("ProfilerManager", $"finished=0, csv_output={csvOutputPathname}, TimeSinceGameStart={Time.time}");
            if (!headerWritten)
            {
                foreach (var profile in profiles)
                    profile.GetHeaders(sb);
                sb.Length--;
                sb.AppendLine();
                headerWritten = true;
            }
            for (int i = 0; i < lineCount; i++)
            {
                foreach (var profile in profiles)
                    profile.GetFramesValues(sb, i);
                sb.Length--;
                sb.AppendLine();
            }
            System.IO.File.AppendAllText(csvOutputPathname, sb.ToString());
            foreach (var profile in profiles)
                profile.Flush();
            sb.Clear();
            lineCount = 0;
        }
        private void OnDestroy()
        {
            StringBuilder sb = new StringBuilder();
            //VRT.Core.BaseStats.Output("ProfilerManager", $"finished=1, csv_output={csvOutputPathname}, TimeSinceGameStart={Time.time}");
            if (profiles.Count > 0)
            {
                savelog();
            }
        }
    }
}