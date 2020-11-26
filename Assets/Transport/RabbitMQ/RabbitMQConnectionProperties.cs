using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public enum RabbitMQExchangeType
{
    Fanout,
    Direct
}
public class RabbitMQConnectionProperties
{
    private string m_ConnectionURI = "guest:guest@localhost:5672";
    private string m_ExchangeName = "3DLive";
    private RabbitMQExchangeType m_ExchangeType = RabbitMQExchangeType.Fanout;
    private string m_RoutingKey = "";

    public string ConnectionURI
    {
        get
        {
            return m_ConnectionURI;
        }
        set
        {
            if(value != null)
                m_ConnectionURI = value;
        }
    }

    public string ExchangeName
    {
        get
        {
            return m_ExchangeName;
        }
        set
        {
            if(value != null)
                m_ExchangeName = value;
        }
    }

    public string RoutingKey
    {
        get
        {
            return m_RoutingKey;
        }
        set
        {
            if(value != null)
                m_RoutingKey = value;
        }
    }

    public RabbitMQExchangeType ExchangeType
    {
        get
        {
            return m_ExchangeType;
        }
        set
        {
            m_ExchangeType = value;
        }
    }

}

