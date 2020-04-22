using System;
using UnityEngine;
using Utils;
using System.Runtime.InteropServices;
using System.IO;

namespace DataProviders
{
    [System.Serializable]
    public class Config
    {
        public bool is_player_1 = false;
        public string player2_ip = "127.0.0.1";
        public int port = 55556;
        public bool use_sprites = false;

        public bool single_player = false;
        public string microphone_device = "";

        public string local_tvm_position = string.Empty;
        public string local_tvm_rotation = string.Empty;
        public string local_tvm_scale = string.Empty;
        public string local_tvm_address = string.Empty; // amqp://tofis:tofis@195.521.117.145:5672
        public string local_tvm_exchange_name = string.Empty; // player1

        public string remote_tvm_position = string.Empty;
        public string remote_tvm_rotation = string.Empty;
        public string remote_tvm_scale = string.Empty;
        public string remote_tvm_address = string.Empty; // amqp://tofis:tofis@195.521.117.145:5672
        public string remote_tvm_exchange_name = string.Empty; // player2

        public string local_pointcloud_url = string.Empty;
        public string local_pointcloud_position = string.Empty;
        public string local_pointcloud_rotation = string.Empty;
        public string local_pointcloud_scale = string.Empty;
        public string local_pointcloud_size = string.Empty;

        public string remote_pointcloud_url = string.Empty;
        public string remote_pointcloud_position = string.Empty;
        public string remote_pointcloud_rotation = string.Empty;
        public string remote_pointcloud_scale = string.Empty;
        public string remote_pointcloud_size = string.Empty;
        public string camera_height = string.Empty;

    }

    public class PCLDataProvider : MonoBehaviour, PCLIdataProvider
    {
        public string PCLDataExchangeName = "";
        private bool isReceiverConnected = false;
        public Config config;
        public event EventHandler<EventArgs<byte[]>> OnNewPCLData;
        public event EventHandler<EventArgs<byte[]>> OnNewMetaData;

        private RabbitMQReceiver m_RabbitMQReceiver = new RabbitMQReceiver();

        private void RabbitMQReceiver_OnPCLDataReceived(object sender, EventArgs<byte[]> e)
        {
            if (OnNewPCLData != null)
                OnNewPCLData(this, e);
        }

        private void Awake()
        {
            m_RabbitMQReceiver.OnDataReceived += RabbitMQReceiver_OnPCLDataReceived;
        }

        private void Start()
        {
            if (PCLDataExchangeName == "pclData.json")
                native_pointcloud_receiver_pinvoke.set_number_wrappers(1);

            config = JsonUtility.FromJson<Config>(System.IO.File.ReadAllText(Application.streamingAssetsPath + "/" + PCLDataExchangeName));
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
			m_RabbitMQReceiver.OnDataReceived -= RabbitMQReceiver_OnPCLDataReceived;
            m_RabbitMQReceiver.Enabled = false;
        }
    }
}
