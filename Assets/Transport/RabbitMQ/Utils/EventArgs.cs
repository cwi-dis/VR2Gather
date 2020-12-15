using System;

namespace VRT.Transport.RabbitMQ.Utils
{
    public class EventArgs<T> : EventArgs
    {
        private T m_Value;

        public EventArgs(T value)
        {
            m_Value = value;
        }

        public T Value
        {
            get
            {
                return m_Value;
            }
        }
    }
}
