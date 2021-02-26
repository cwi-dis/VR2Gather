using UnityEngine;
using VRTCore;

namespace VRT.Core
{
    public class BaseStats
    {
        protected string name;
        private System.DateTime statsLastTime;
        private static bool initialized = false;
        private static double statsInterval = 10;
        private static System.IO.StreamWriter statsStream;

        private static void Init()
        {
            if (initialized) return;
            statsInterval = Config.Instance.statsInterval;
            if (Config.Instance.statsOutputFile != "")
            {
                string statsFilename = $"{Application.persistentDataPath}/{Config.Instance.statsOutputFile}";
                statsStream = new System.IO.StreamWriter(statsFilename, true);
                //
                // Write an identifying line to both the statsfile (so we can split runs) and the console (so we can find the stats file)
                //
                string statsLine = $"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component=stats, starting=1, statsFilename={statsFilename}";
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
        protected BaseStats(string _name)
        {
            if (!initialized) Init();
            name = _name;
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

        // statis method, for use when only one or two stats lines are produced.
        public static void Output(string name, string s)
        {
            if (!initialized) Init();
            string statsLine = $"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={name}, {s}";
            if (statsStream == null)
            {
                Debug.Log(statsLine);
            }
            else
            {
                lock (lockObj)
                {
                    statsStream.WriteLine(statsLine);
                    Flush();
                }
            }
        }
    }
}