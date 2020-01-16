using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Utils
{
    public class Command : IDisposable
    {
        private Delegate m_Delegate;
        private object[] m_Arguments;

        private bool m_Executed = false;
        private ManualResetEvent m_WaitHandle = new ManualResetEvent(false);

        private bool m_AutoDisposeOnExecute = true;

        private object m_ReturnValue;

        public Command(Delegate del, params object[] args)
        {           
            m_Delegate = del;
            m_Arguments = args;            
        }

        public Command(bool autoDisposeOnExecute, Delegate del, params object[] args)
        {
            m_AutoDisposeOnExecute = autoDisposeOnExecute;
            m_Delegate = del;
            m_Arguments = args;
        }

        public static implicit operator Command(Delegate del)
        {
            return new Command(del, null);
        }

        public object Execute()
        {
            // forbid multiple executions of the command
            if (m_Executed)
                return m_ReturnValue;

            m_WaitHandle.Reset();
            m_ReturnValue = m_Delegate.DynamicInvoke(m_Arguments);
            m_WaitHandle.Set();

            if (m_AutoDisposeOnExecute)
                Dispose();

            m_Executed = true;
            return m_ReturnValue;
        }

        /// <summary>
        /// Wait for execution of the command. If AutoDisposeOnExecute is true this call has undefined behaviour.
        /// To properly wait for execution of the command set AutoDisposeOnExecute to false and manually dispose the command object after the execution finishes.
        /// </summary>
        public void WaitForExecution()
        {
            m_WaitHandle.WaitOne();
        }

        public Delegate Delegate
        {
            get
            {
                return m_Delegate;
            }
        }

        public object[] Arguments
        {
            get
            {
                return m_Arguments;
            }
        }

        public object ReturnValue
        {
            get
            {
                return m_ReturnValue;
            }
        }

        public bool IsExecuted
        {
            get
            {
                return m_Executed;
            }
        }

        public void Dispose()
        {
            try
            {
                m_WaitHandle.Close();
            }
            catch(Exception)
            {

            }
        }
    }
}
