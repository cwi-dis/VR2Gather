using UnityEngine;
using System;

namespace VRT.Core
{
    public class BaseStats
    {
        protected string name;
        private System.DateTime statsLastTime;
        private static bool initialized = false;
        private static double defaultStatsInterval = 10;
        private double statsInterval = 10;
        private static System.IO.StreamWriter statsStream;

        private static void Init()
        {
            if (initialized) return;
            defaultStatsInterval = Config.Instance.statsInterval;
            if (Config.Instance.statsOutputFile != "")
            {
                string sfn = Config.Instance.statsOutputFile;
                string host = Environment.MachineName;
                DateTime now = DateTime.Now;
                string ts = now.ToString("yyyyMMdd-HHmm");
                sfn = sfn.Replace("{host}", host);
                sfn = sfn.Replace("{ts}", ts);
                string statsFilename = $"{Application.persistentDataPath}/{sfn}";
                statsStream = new System.IO.StreamWriter(statsFilename, Config.Instance.statsOutputFileAppend);
                //
                // Write an identifying line to both the statsfile (so we can split runs) and the console (so we can find the stats file)
                //
                string statsLine = $"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component=stats, starting=1, wallClock={ts}, statsFilename={statsFilename}";
                statsStream.WriteLine(statsLine);
                statsStream.Flush();
                Debug.Log(statsLine);
            }
            initialized = true;
        }

        static void Flush()
        {
            // xxxjack: need to check performance penalty of this.
            // If it turns out to be too much we call at most every second or something like that.
            if (statsStream != null) statsStream.Flush();

        }
        private static void DeInit()
        {
            if (statsStream != null) statsStream.Flush();
        }
        protected BaseStats(string _name, double interval=0)
        {
            if (!initialized) Init();
            name = _name;
            statsInterval = interval > 0 ? interval : defaultStatsInterval;
            statsLastTime = System.DateTime.Now;
        }

        ~BaseStats()
        {
            DeInit();
        }
        protected bool ShouldClear()
        {
            return System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(statsInterval);
        }

        protected void Clear()
        {
            statsLastTime = System.DateTime.Now;
        }

        protected double Interval()
        {
            return (System.DateTime.Now - statsLastTime).TotalSeconds;
        }

        protected bool ShouldOutput()
        {
            return System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(statsInterval);
        }

        protected void Output(string s)
        {
            Output(name, s);
        }

        static object lockObj = new object();
        static int seq = 0;

        // statis method, for use when only one or two stats lines are produced.
        public static void Output(string name, string s)
        {
            if (!initialized) Init();
            lock (lockObj)
            {
                seq++;
                string statsLine = $"stats: seq={seq}, ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={name}, {s}";
                if (statsStream == null)
                {
                    Debug.Log(statsLine);
                }
                else
                {
                    statsStream.WriteLine(statsLine);
                    Flush();
                }
            }
        }
    }
}