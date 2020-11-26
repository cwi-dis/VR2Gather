using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using VRTCore;
using RabbitMQ;
using RabbitMQ.Utils;

namespace DataProviders
{

    public class NetworkDataProvider : MonoBehaviour, IDataProvider {
        public bool isMaster;
        public string connectionURI;
        public string exchangeName;
        private Config cfg;
        private Config._TVMs tvm;
        private bool isReceiverConnected = false;
        //public Config config;
        public event EventHandler<EventArgs<byte[]>> OnNewData;

        private RabbitMQReceiver m_RabbitMQReceiver = new RabbitMQReceiver();

        private void RabbitMQReceiver_OnDataReceived(object sender, EventArgs<byte[]> e) {
			if (OnNewData != null) {
                OnNewData (this, e);
			}
        }

        private void Awake() {
            m_RabbitMQReceiver.OnDataReceived += RabbitMQReceiver_OnDataReceived;

            cfg = Config.Instance;
            tvm = cfg.TVMs;
        }

        private void Start() {
            if (isMaster) DllFunctions.set_number_TVMS(8);
            m_RabbitMQReceiver.ConnectionProperties.ConnectionURI = connectionURI;
            m_RabbitMQReceiver.ConnectionProperties.ExchangeName = exchangeName;
            m_RabbitMQReceiver.Enabled = true;
        }

        //private void OnEnable() {
        //    m_RabbitMQReceiver.Enabled = true;
        //}

		private void Update() {
			if (this.isReceiverConnected != this.m_RabbitMQReceiver.IsConnected) {
				this.isReceiverConnected = this.m_RabbitMQReceiver.IsConnected;				
			}
		}

        private void OnDisable() {
            m_RabbitMQReceiver.Enabled = false;
        }

        private void OnDestroy() {
			m_RabbitMQReceiver.OnDataReceived -= RabbitMQReceiver_OnDataReceived;
            m_RabbitMQReceiver.Enabled = false;
        }

    }
}
