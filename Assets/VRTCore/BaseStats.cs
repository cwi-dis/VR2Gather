using UnityEngine;

namespace VRT.Core
{
    public class BaseStats
    {
        protected string name;
        protected System.DateTime statsLastTime;
        private const int statsInterval = 10;

        protected BaseStats(string _name)
        {
            name = _name;
            statsLastTime = System.DateTime.Now;
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