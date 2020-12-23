using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VRT.UserRepresentation.TVM.Utils
{
    public class ConcurrentCircularQueue<T> : IDisposable
    {
        #region Fields
        Queue<T> m_Queue = new Queue<T>();
        private int m_QueueSize = 100;
        private T m_DefaultReturnValue;

        private ManualResetEvent m_EnqueueWaitHandle = new ManualResetEvent(false);
        private ManualResetEvent m_DequeueWaitHandle = new ManualResetEvent(false);

        #endregion

        #region Constructors

        public ConcurrentCircularQueue()
        {

        }

        public ConcurrentCircularQueue(int queueSize)
        {
            QueueSize = queueSize;
        }

        #endregion

        #region Private Methods
        private T FitToQueueSize()
        {
            var lastValue = m_DefaultReturnValue;

            if (m_QueueSize == 0)       // special case. QueueSize == 0 means normal queue
                return lastValue;

            while (m_Queue.Count > m_QueueSize)
            {
                lastValue = m_Queue.Dequeue();
            }
            return lastValue;
        }

        private bool IsFullCore()
        {
            return m_Queue.Count >= m_QueueSize;
        }

        private bool IsEmptyCore()
        {
            return m_Queue.Count == 0;
        }

        private T EnqueueCore(T obj)
        {
            m_Queue.Enqueue(obj);
            var val = FitToQueueSize();
            m_EnqueueWaitHandle.Set();
            return val;
        }

        private T DequeueCore()
        {
            T val;
            val = m_Queue.Dequeue();   // throws exception on empty queue
            m_DequeueWaitHandle.Set();
            return val;
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Enqueues a new element to the end of the queue. If the queue is full, the first element is dequeued.
        /// Returns the last element removed from the queue's front line.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public T Enqueue(T obj)
        {
            lock (m_Queue)
            {
                return EnqueueCore(obj);
            }
        }

        public T Dequeue()
        {
            T val;
            lock (m_Queue)
            {
                val = DequeueCore();
            }
            return val;
        }

        /// <summary>
        /// Enqueues a new element to the end of the queue. If the queue is full, it waits for an element to be dequeued before enquing the item and returning.
        /// </summary>
        /// <param name="obj"></param>
        public void WaitAndEnqueue(T obj)
        {
            bool enqueued = false;
            while (!enqueued)
            {
                lock (m_Queue)
                {
                    if (!IsFullCore())
                    {
                        EnqueueCore(obj);
                        enqueued = true;
                    }
                    else
                    {
                        m_DequeueWaitHandle.Reset();
                    }
                }
                if (!enqueued)
                    m_DequeueWaitHandle.WaitOne();
            }
        }

        public T WaitAndDequeue()
        {
            bool dequeued = false;
            T val = m_DefaultReturnValue;

            while (!dequeued)
            {
                lock (m_Queue)
                {
                    if (!IsEmptyCore())
                    {
                        val = DequeueCore();
                        dequeued = true;
                    }
                    else
                    {
                        m_EnqueueWaitHandle.Reset();
                    }
                }
                if (!dequeued)
                    m_EnqueueWaitHandle.WaitOne();
            }
            return val;
        }

        /// <summary>
        /// Returns the front element of the queue without dequeing it. Returns the Default Return Value if the Queue is empty.
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            lock (m_Queue)
            {
                if (m_Queue.Count == 0)
                    return m_DefaultReturnValue;
                return m_Queue.Peek();
            }
        }

        public T WaitAndPeek()
        {
            bool peeked = false;
            T val = m_DefaultReturnValue;

            while (!peeked)
            {
                lock (m_Queue)
                {
                    if (!IsEmptyCore())
                    {
                        val = m_Queue.Peek();
                        peeked = true;
                    }
                    else
                    {
                        m_EnqueueWaitHandle.Reset();
                    }
                }
                if (!peeked)
                    m_EnqueueWaitHandle.WaitOne();
            }
            return val;
        }

        public void Clear()
        {
            lock (m_Queue)
            {
                m_Queue.Clear();
            }
        }
        #endregion

        #region Properties

        public int Count
        {
            get
            {
                lock (m_Queue)
                {
                    return m_Queue.Count;
                }
            }
        }

        public bool IsEmpty
        {
            get
            {
                lock (m_Queue)
                {
                    return IsEmptyCore();
                }
            }
        }

        public bool IsFull
        {
            get
            {
                lock (m_Queue)
                {
                    return IsFullCore();
                }
            }
        }

        public T DefaultReturnValue
        {
            get
            {
                return m_DefaultReturnValue;
            }
            set
            {
                lock (m_Queue)
                {
                    m_DefaultReturnValue = value;
                }
            }
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
                {
                    lock (m_Queue)
                    {
                        m_QueueSize = value;
                        FitToQueueSize();
                    }
                }
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            try
            {
                m_DequeueWaitHandle.Close();
                m_EnqueueWaitHandle.Close();
            }
            catch (Exception)
            {

            }
        }
        #endregion
    }
}
