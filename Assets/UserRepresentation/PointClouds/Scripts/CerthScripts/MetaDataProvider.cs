using System;
using UnityEngine;
using Utils;
using System.Runtime.InteropServices;
using System.IO;

namespace PCLDataProviders
{

    public class MetaDataProvider : MonoBehaviour, PCLIdataProvider
    {
        public string MetaDataRMQExchangeName = "";
        private bool isReceiverConnected = false;
        public Config config;
        public event EventHandler<EventArgs<byte[]>> OnNewPCLData;
        public event EventHandler<EventArgs<byte[]>> OnNewMetaData;

        private RabbitMQReceiver m_RabbitMQReceiver = new RabbitMQReceiver();

        private void RabbitMQReceiver_OnMetaDataReceived(object sender, EventArgs<byte[]> e)
        {
            if (OnNewMetaData != null)
                OnNewMetaData(this, e);
        }

        private void Awake()
        {
            m_RabbitMQReceiver.OnDataReceived += RabbitMQReceiver_OnMetaDataReceived;
        }

        private void Start()
        {
            config = JsonUtility.FromJson<Config>(System.IO.File.ReadAllText(Application.streamingAssetsPath + "/" + MetaDataRMQExchangeName));
            m_RabbitMQReceiver.ConnectionProperties.ConnectionURI = config.remote_tvm_address;
            m_RabbitMQReceiver.ConnectionProperties.ExchangeName = config.remote_tvm_exchange_name;
            m_RabbitMQReceiver.Enabled = true;
        }

        private void OnEnable()
        {
            m_RabbitMQReceiver.Enabled = true;
        }

		private void Update()
		{
			if (this.isReceiverConnected != this.m_RabbitMQReceiver.IsConnected)
			    this.isReceiverConnected = this.m_RabbitMQReceiver.IsConnected;
		}

        private void OnDisable()
        {
            m_RabbitMQReceiver.Enabled = false;
        }

        private void OnDestroy()
        {
			m_RabbitMQReceiver.OnDataReceived -= RabbitMQReceiver_OnMetaDataReceived;
            m_RabbitMQReceiver.Enabled = false;
        }

    }
}
