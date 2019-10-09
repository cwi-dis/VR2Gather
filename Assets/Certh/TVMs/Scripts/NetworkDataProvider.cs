using System;
using UnityEngine;
using Utils;
using System.Runtime.InteropServices;
using System.IO;

namespace DataProviders
{
    //[System.Serializable]
    //public class Config
    //{
    //    public bool is_player_1 = false;
    //    public string player2_ip = "127.0.0.1";
    //    public int port = 55556;
    //    public bool use_sprites = false;

    //    public bool single_player = false;
    //    public string microphone_device = "";

    //    public string local_tvm_position = string.Empty;
    //    public string local_tvm_rotation = string.Empty;
    //    public string local_tvm_scale = string.Empty;
    //    public string local_tvm_address = string.Empty; // amqp://tofis:tofis@195.521.117.145:5672
    //    public string local_tvm_exchange_name = string.Empty; // player1

    //    public string remote_tvm_position = string.Empty;
    //    public string remote_tvm_rotation = string.Empty;
    //    public string remote_tvm_scale = string.Empty;
    //    public string remote_tvm_address = string.Empty; // amqp://tofis:tofis@195.521.117.145:5672
    //    public string remote_tvm_exchange_name = string.Empty; // player2

    //    public string local_pointcloud_url = string.Empty;
    //    public string local_pointcloud_position = string.Empty;
    //    public string local_pointcloud_rotation = string.Empty;
    //    public string local_pointcloud_scale = string.Empty;
    //    public string local_pointcloud_size = string.Empty;

    //    public string remote_pointcloud_url = string.Empty;
    //    public string remote_pointcloud_position = string.Empty;
    //    public string remote_pointcloud_rotation = string.Empty;
    //    public string remote_pointcloud_scale = string.Empty;
    //    public string remote_pointcloud_size = string.Empty;
    //    public string camera_height = string.Empty;

    //}

    public class NetworkDataProvider : MonoBehaviour, IDataProvider
    {
        private bool isReceiverConnected = false;
        //public Config config;
        public Config config;
        private Config._TVMs tvm;
        public string connectionURI;
        public string exchangeName;
        public event EventHandler<EventArgs<byte[]>> OnNewData;

        private RabbitMQReceiver m_RabbitMQReceiver = new RabbitMQReceiver();

        private void RabbitMQReceiver_OnDataReceived(object sender, EventArgs<byte[]> e)
        {
			if (OnNewData != null) {
                OnNewData (this, e);
			}
        }

        private void Awake()
        {
            m_RabbitMQReceiver.OnDataReceived += RabbitMQReceiver_OnDataReceived;
        }

        private void Start()
        {
            config = Config.Instance;
            tvm = config.TVMs;
            transform.localPosition = tvm.offsetPosition;
            transform.localRotation = Quaternion.Euler(tvm.offsetRotation);


            //m_RabbitMQReceiver.ConnectionProperties.ConnectionURI = config.remote_tvm_address;
            //m_RabbitMQReceiver.ConnectionProperties.ExchangeName = config.remote_tvm_exchange_name;
            m_RabbitMQReceiver.ConnectionProperties.ConnectionURI = connectionURI;
            m_RabbitMQReceiver.ConnectionProperties.ExchangeName = exchangeName;
            m_RabbitMQReceiver.Enabled = true;
        }

        private void OnEnable()
        {
            m_RabbitMQReceiver.Enabled = true;
        }

		private void Update()
		{
			if (this.isReceiverConnected != this.m_RabbitMQReceiver.IsConnected)
			{
				this.isReceiverConnected = this.m_RabbitMQReceiver.IsConnected;
				
			}
		}

        private void OnDisable()
        {
            m_RabbitMQReceiver.Enabled = false;
        }

        private void OnDestroy()
        {
			
			m_RabbitMQReceiver.OnDataReceived -= RabbitMQReceiver_OnDataReceived;
            m_RabbitMQReceiver.Enabled = false;
        }

    }
}
