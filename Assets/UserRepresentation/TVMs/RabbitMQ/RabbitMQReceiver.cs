using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Utils;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;

public class RabbitMQReceiver
{
    private ConnectionFactory m_ConnectionFactory = new ConnectionFactory();
    private IConnection m_Connection;
    private IModel m_Model;
    private bool m_Connected;
    //private IList<KeyValuePair<string, object>> m_QueueArgs = new List<KeyValuePair<string, object>>();
    private Dictionary<string, object> m_QueueArgs = new Dictionary<string, object>() { { "x-max-length", 1 } };
    private RabbitMQReceiverConnectionProperties m_ConnectionProperties = new RabbitMQReceiverConnectionProperties();        
    private Subscription m_Subscription;
    private QueueDeclareOk m_QueueDeclareResult;
    private BackgroundWorker m_BGWorker = new BackgroundWorker();
    private const int m_ReconnectionAttemptTimeout = 4000;        
    public event EventHandler<EventArgs<byte[]>> OnDataReceived;
    public event EventHandler OnStopped;
    public event EventHandler OnStarted;
   
    void BGWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        if (OnStopped != null)
            OnStopped(this, EventArgs.Empty);
    }

    void BGWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        if (OnStarted != null)
            OnStarted(this, EventArgs.Empty);

        while (!m_BGWorker.CancellationPending)
        {
            
            Subscription subscription = Connect();
            if (subscription == null)
            {
                Disconnect();
                Thread.Sleep(m_ReconnectionAttemptTimeout);
                continue;
            }
            BasicDeliverEventArgs eargs;
            while (!m_BGWorker.CancellationPending)
            {
                try
                {
                    
                    while (!m_BGWorker.CancellationPending && subscription.Next(500, out eargs))
                    {
                        if (OnDataReceived != null)
                            OnDataReceived(this, new EventArgs<byte[]>(eargs.Body));
                        subscription.Ack(eargs);
                    }
                }
                catch (Exception)
                {
                    Disconnect();
                    break;
                }
            }
           
        }
        Disconnect();
    }

    private Subscription Connect()
    {
        if (m_Connected)
            Disconnect();
        try
        {
            m_ConnectionFactory.Uri = m_ConnectionProperties.ConnectionURI;
            UnityEngine.Debug.Log($"xxxjack RabbitMQReceiver.Connect: uri={m_ConnectionProperties.ConnectionURI}, exchange={m_ConnectionProperties.ExchangeName}");
            m_Connection = m_ConnectionFactory.CreateConnection();
            m_Model = m_Connection.CreateModel();
            if (String.IsNullOrEmpty(m_ConnectionProperties.QueueName))
                m_QueueDeclareResult = m_Model.QueueDeclare(string.Empty, false, true, true, m_QueueArgs);
            else
                m_QueueDeclareResult = m_Model.QueueDeclare(m_ConnectionProperties.QueueName, false, true, true, m_QueueArgs);

            m_Model.QueueBind(m_QueueDeclareResult.QueueName, m_ConnectionProperties.ExchangeName, m_ConnectionProperties.RoutingKey);
            m_Model.BasicQos(0, 1, false);
            m_Subscription = new Subscription(m_Model, m_QueueDeclareResult.QueueName, false);
            m_Connected = true;
        }
        catch (Exception)
        {
            Disconnect();
            m_Connected = false;
            m_Subscription = null;
        }
        return m_Subscription;
    }
    private void Disconnect()
    {            
        try
        {
            if (m_Subscription != null)
                m_Subscription.Close();
        }
        catch (Exception) { }

        try
        {
            if (m_Model != null && m_Model.IsOpen)
                m_Model.Close();
        }
        catch (Exception) { }

        try
        {
            if (m_Connection != null && m_Connection.IsOpen)
                m_Connection.Close();
        }
        catch (Exception) { }

        m_Subscription = null;
        m_Model = null;
        m_Connection = null;
        m_Connected = false;
        
    }

    public RabbitMQReceiver()
    {
        m_BGWorker.WorkerSupportsCancellation = true;
        m_BGWorker.DoWork += new DoWorkEventHandler(BGWorker_DoWork);
        m_BGWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BGWorker_RunWorkerCompleted);
    }

    public bool Enabled
    {
        get
        {
            return m_BGWorker.IsBusy;
        }
        set
        {
            if(value && !m_BGWorker.IsBusy)
                m_BGWorker.RunWorkerAsync();
            else if (!value && m_BGWorker.IsBusy)
            {
                m_BGWorker.CancelAsync();
                //while (m_BGWorker.IsBusy)
                //    Thread.Sleep(1000);
            }
        }
    }

    public bool IsConnected
    {
        get
        {
            return m_Connected;
        }
    }

    public RabbitMQReceiverConnectionProperties ConnectionProperties
    {
        get
        {
            return m_ConnectionProperties;
        }
    }

}
