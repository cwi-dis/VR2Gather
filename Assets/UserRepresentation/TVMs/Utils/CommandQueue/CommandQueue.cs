using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Utils
{
    public class CommandQueue : SynchronizationContext
    {        
        Queue<Command> m_CommandQueue = new Queue<Command>();

        public void Enqueue(Command cmd)
        {
            lock (m_CommandQueue)
            {
                m_CommandQueue.Enqueue(cmd);
            }            
        }

        /// <summary>
        /// Make sure that the command's property AutoDisposeOnExecution is not set. Otherwise, this function call has undefined behaviour
        /// </summary>
        /// <param name="cmd"></param>
        public void EnqueueAndWaitForExecution(Command cmd)
        {
            Enqueue(cmd);
            cmd.WaitForExecution();            
        }

        public bool ExecuteOne()
        {
            if (IsEmpty)
                return false;

            Command cmd = null;

            lock (m_CommandQueue)
            {
                cmd = m_CommandQueue.Dequeue();
            }

            cmd.Execute();

            return true;
        }

        public int ExecuteAll()
        {
            int cmdCount = 0;
            while (ExecuteOne())
                cmdCount++;
            return cmdCount;
        }

        public void Clear()
        {
            lock (m_CommandQueue)
            {
                m_CommandQueue.Clear();
            }            
        }

        public int Count
        {
            get
            {
                int cnt = 0;
                lock (m_CommandQueue)
                    cnt = m_CommandQueue.Count;
                return cnt;
            }
        }

        public bool IsEmpty
        {
            get
            {
                lock (m_CommandQueue)
                    return m_CommandQueue.Count == 0;
            }
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            Command cmd = new Command(d, state);
            Enqueue(cmd);
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            Command cmd = new Command(false, d, state);
            Enqueue(cmd);
            cmd.WaitForExecution();
            cmd.Dispose();            
        }
    }
}
