using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRT.UserRepresentation.TVM.Utils
{
    public class CircularQueue<T> : Queue<T>
    {
        protected int m_QueueSize = 100;
        protected T m_DefaultReturnValue;
        protected T m_LastValue;

        public CircularQueue()
        {

        }

        public CircularQueue(int size)
        {
            if (size >= 0)
                m_QueueSize = size;
        }

        public int QueueSize
        {
            get
            {
                return m_QueueSize;
            }
            set
            {
                if (value >= 0)
                    m_QueueSize = value;
            }
        }

        public new T Enqueue(T item)
        {
            base.Enqueue(item);
            FitToQueueSize();
            return m_LastValue;
        }

        public T DefaultReturnValue
        {
            get
            {
                return m_DefaultReturnValue;
            }
            set
            {
                m_DefaultReturnValue = value;
            }
        }

        private void FitToQueueSize()
        {
            m_LastValue = m_DefaultReturnValue;
            if (m_QueueSize == 0)       // special case. QueueSize == 0 means normal queue
                return;


            while (Count > m_QueueSize)
            {
                m_LastValue = Dequeue();
            }
        }
    }
}
