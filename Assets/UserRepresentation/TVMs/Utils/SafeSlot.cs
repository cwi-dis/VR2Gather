using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRT.UserRepresentation.TVM.Utils
{
    public class SafeSlot<T>
    {
        private T m_Slot = default;
        private object m_LockObject = new object();
        private bool m_HasNew = false;

        public bool HasNew
        {
            get
            {
                return m_HasNew;
            }
        }

        public T Value
        {
            get
            {
                lock (m_LockObject)
                {
                    m_HasNew = false;
                    return m_Slot;
                }
            }
            set
            {
                lock (m_LockObject)
                {
                    m_Slot = value;
                    m_HasNew = true;
                }
            }
        }

    }
}
