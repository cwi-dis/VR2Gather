﻿using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using VRT.Core;
using VRT.Transport.RabbitMQ;
using VRT.Transport.RabbitMQ.Utils;
using VRT.UserRepresentation.TVM;

namespace VRT.UserRepresentation.TVM.DataProviders
{

    public class NetworkDataProvider : MonoBehaviour, IDataProvider, ITVMHookUp
    {
        public bool isMaster;
        public string connectionURI;
        public string exchangeName;
        private Config cfg;
        private Config._TVMs tvm;
        private bool isReceiverConnected = false;
        public event EventHandler<EventArgs<byte[]>> OnNewData;
        private RabbitMQReceiver m_RabbitMQReceiver = new RabbitMQReceiver();

        private void RabbitMQReceiver_OnDataReceived(object sender, EventArgs<byte[]> e)
        {
            if (OnNewData != null)
            {
                OnNewData(this, e);
            }
        }

        public void HookUp(bool _firstTVM, string _connectionURI, string _exchangeName)
        {
            isMaster = _firstTVM;
            connectionURI = _connectionURI;
            exchangeName = _exchangeName;
            gameObject.SetActive(true);

        }
        private void Awake()
        {
            m_RabbitMQReceiver.OnDataReceived += RabbitMQReceiver_OnDataReceived;

            cfg = Config.Instance;
            tvm = cfg.TVMs;
        }

        private void Start()
        {
            if (isMaster) DllFunctions.set_number_TVMS(8);
            m_RabbitMQReceiver.ConnectionProperties.ConnectionURI = connectionURI;
            m_RabbitMQReceiver.ConnectionProperties.ExchangeName = exchangeName;
            m_RabbitMQReceiver.Enabled = true;
        }

        //private void OnEnable() {
        //    m_RabbitMQReceiver.Enabled = true;
        //}

        private void Update()
        {
            if (isReceiverConnected != m_RabbitMQReceiver.IsConnected)
            {
                isReceiverConnected = m_RabbitMQReceiver.IsConnected;
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
