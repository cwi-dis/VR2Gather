using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class RabbitMQReceiverConnectionProperties : RabbitMQConnectionProperties
{
    private string m_QueueName = "";

    public string QueueName
    {
        get
        {
            return m_QueueName;
        }
        set
        {
            if(value != null)
                m_QueueName = value;
        }
    }
}

