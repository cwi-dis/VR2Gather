using UnityEngine;
using System;

namespace Cwipc
{
#if VRT_WITH_STATS
    /// <summary>
    /// Gathering statistics during a run to allow later analysis.
    /// All statistics records consist of lines starting with "stats:" followed by name=value pairs.
    /// Each statistics record has some common fields: a field seqno= which gives the sequence number and a field ts= which
    /// gives the number of seconds since midnight, on this machines clock. Most statistics records had
    /// a component= field that gives the name of the object responsible for creating this statiscs record.
    ///
    /// Statistics records can be output every moment a new value is available (usually every Update call)
    /// or aggregated over some period of time (default: 10 seconds).
    ///
    /// The Statistics class can be subclassed in an object implementation, and it is also possible to use
    /// the 
    ///
    /// Statistics records are sent to the Unity Log by default, or written to a separate statistics file.
    ///
    /// Repository <https://github.com/cwi-dis/VRTstatistics> has various tools to collect and agregate the statistics, plot
    /// them, etc.
    /// </summary>
    public class Statistics
    {
        protected string name;
        private System.DateTime statsLastTime;
        private static System.DateTime globalStatsLastTime;
        private static bool initialized = false;
        private static double defaultStatsInterval = 10;
        private double statsInterval = 10;
        private static System.IO.StreamWriter statsStream;
        private bool usesDefaultInterval;
        private static bool muted = false;

        /// <summary>
        /// Call this static method early during initialization to configure Statistics logger.
        /// </summary>
        /// <param name="_defaultStatsInterval">Number of seconds over which statistics are aggregated</param>
        /// <param name="statsOutputFile">Output file name</param>
        /// <param name="append">If true append to the output file, otherwise overwrite</param>
        public static void Initialize(double _defaultStatsInterval = -1, string statsOutputFile = null, bool append = false)
        {
            if (initialized)
            {
                if (_defaultStatsInterval >= 0 || statsOutputFile != null)
                {
                    Debug.LogWarning("BaseStats: call to Initialize() is too late");
                }
            }
            if (_defaultStatsInterval >= 0)
            {
                defaultStatsInterval = _defaultStatsInterval;
            }
            DateTime now = DateTime.Now;
            globalStatsLastTime = now;
            if (statsOutputFile == "-")
            {
                muted = true;
                Debug.Log("stats: muted=1");
                return;
            }
            if (statsOutputFile != null && statsOutputFile != "")
            {
                string sfn = statsOutputFile;
                string host = Environment.MachineName;
                string ts = now.ToString("yyyyMMdd-HHmm");
                sfn = sfn.Replace("{host}", host);
                sfn = sfn.Replace("{ts}", ts);
                string statsFilename = $"{Application.persistentDataPath}/{sfn}";
                statsStream = new System.IO.StreamWriter(statsFilename, append);
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

        /// <summary>
        /// Base class constructor, called by subclasses.
        /// </summary>
        /// <param name="_name">Name of the component to which this instance pertains</param>
        /// <param name="interval">Number of seconds over which this instance aggregates</param>
        protected Statistics(string _name, double interval = -1)
        {
            if (!initialized) Initialize();
            name = _name;
            usesDefaultInterval = interval < 0;
            statsInterval = usesDefaultInterval ? defaultStatsInterval : interval;
            statsLastTime = globalStatsLastTime;
        }

        ~Statistics()
        {
            DeInit();
        }

        /// <summary>
        /// Clear the aggregation buffers after outputting a statistics record.
        /// </summary>
        protected void Clear()
        {
            statsLastTime = System.DateTime.Now;
            globalStatsLastTime = statsLastTime;
        }

        /// <summary>
        /// Duration of seconds (actual) of the current interval.
        /// </summary>
        /// <returns></returns>
        protected double Interval()
        {
            return (System.DateTime.Now - statsLastTime).TotalSeconds;
        }

        /// <summary>
        /// Return true if our interval is over and the subclass should output a statistics record.
        /// </summary>
        /// <returns></returns>
        protected bool ShouldOutput()
        {
            return System.DateTime.Now > statsLastTime + System.TimeSpan.FromSeconds(statsInterval);
        }

        /// <summary>
        /// Output a single record for the Statistics object.
        /// The parameter should only contain comma-separated name=value pairs, the common fields are automatically added.
        /// </summary>
        /// <param name="s">The statistics to output</param>
        protected void Output(string s)
        {
            Output(name, s);
        }

        static object lockObj = new object();
        static int seq = 0;

        /// <summary>
        /// Output a one-shot statistics line.
        /// This is used most often to show mappings between things, for example how ts= timestamps map to NTP clocks,
        /// or which renderer component uses which preparer component. The standard fields are automatically included.
        /// </summary>
        /// <param name="name">Name of the component outputting the record</param>
        /// <param name="s">The name=value string</param>
        public static void Output(string name, string s)
        {
            if (muted) return;
            if (!initialized) Initialize();
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
#endif
}