using UnityEngine;
using VRTCore;

namespace VRT.Core
{
    public class BaseStats
    {
        protected string name;
        protected System.DateTime statsLastTime;
        private static bool initialized = false;
        private static double statsInterval = 10;

        private static void Init()
        {
            statsInterval = Config.Instance.statsInterval;
            initialized = true;
        }

        private static void DeInit()
        {
            Debug.Log("xxxjack BaseStats DeInit called");
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
            return false;
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
            Debug.Log($"stats: ts={System.DateTime.Now.TimeOfDay.TotalSeconds:F3}, component={name}, {s}");
        }
    }
}