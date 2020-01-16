using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace Utils
{
    public class StatisticsEngine
    {
        public StatisticsEngine()
        {
            Capacity = 0;       // default unlimited queue
        }

        public StatisticsEngine(int capacity)
        {
            Capacity = capacity;
        }

        public void Reset()
        {
            m_Sum = 0;
            m_SumSquared = 0;
            m_Values.Clear();
        }
        public void AddValue(double val)
        {
            double v = m_Values.Enqueue(val);
            m_Sum += val - v;
            m_SumSquared += val * val - v * v;
        }

        public int Count
        {
            get
            {
                return m_Values.Count;
            }
        }

        public double Std
        {
            get
            {
                if (m_Values.Count > 0)
                {
                    double mean = m_Sum / (double)(m_Values.Count);
                    return Math.Sqrt(m_SumSquared / (double)(m_Values.Count) - mean * mean);
                }
                return 0;
            }
        }

        public double Mean
        {
            get
            {
                if (m_Values.Count > 0)
                {
                    return m_Sum / (double)(m_Values.Count);
                }
                return 0.0;
            }
        }

        public int Capacity
        {
            get
            {
                return m_Values.QueueSize;
            }
            set
            {
                m_Values.QueueSize = value;
            }
        }

        private double m_Sum;
        private double m_SumSquared;
        private CircularQueue<double> m_Values = new CircularQueue<double>();
    }
}
