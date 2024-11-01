using FishNet.Managing;
using FishNet.Managing.Transporting;
using FishNet.Transporting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace VRT.Fishnet {
    public class VRTFishnetTransport : Transport
    {
        const int _mtu = 1000;

        LocalConnectionState _clientConnectionState = LocalConnectionState.Stopped;
        LocalConnectionState _serverConnectionState = LocalConnectionState.Stopped;

        public VRTFishnetController controller = null;
        
        public bool debug = false;

        protected void OnDestroy()
        {
            Shutdown();
        }
      
        public string Name()
        {
            return GetType().Name;
        }

        /// <summary>
        /// Gets the address of a remote connection Id.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public override string GetConnectionAddress(int connectionId)
        {
            if (debug) Debug.Log($"{Name()}: GetConnectionAddress({connectionId})");
            return controller.GetConnectionAddress(connectionId);
        }
        /// <summary>
        /// Called when a connection state changes for the local client.
        /// </summary>
        public override event Action<ClientConnectionStateArgs> OnClientConnectionState;
        /// <summary>
        /// Called when a connection state changes for the local server.
        /// </summary>
        public override event Action<ServerConnectionStateArgs> OnServerConnectionState;
        /// <summary>
        /// Called when a connection state changes for a remote client.
        /// </summary>
        public override event Action<RemoteConnectionStateArgs> OnRemoteConnectionState;
        /// <summary>
        /// Gets the current local ConnectionState.
        /// </summary>
        /// <param name="server">True if getting ConnectionState for the server.</param>
        public override LocalConnectionState GetConnectionState(bool server)
        {
            if (debug) Debug.Log($"{Name()}: GetConnectionState({server})");
            return server ? _serverConnectionState : _clientConnectionState;
        }
        /// <summary>
        /// Gets the current ConnectionState of a remote client on the server.
        /// </summary>
        /// <param name="connectionId">ConnectionId to get ConnectionState for.</param>
        public override RemoteConnectionState GetConnectionState(int connectionId)
        {
            if (debug) Debug.Log($"{Name()}: GetConnectionState({connectionId})");
            return RemoteConnectionState.Started;
        }
        /// <summary>
        /// Handles a ConnectionStateArgs for the local client.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleClientConnectionState(ClientConnectionStateArgs connectionStateArgs)
        {
            if (debug) Debug.Log($"{Name()}: HandleClientConnectionState()");
            OnClientConnectionState?.Invoke(connectionStateArgs);
        }
        /// <summary>
        /// Handles a ConnectionStateArgs for the local server.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleServerConnectionState(ServerConnectionStateArgs connectionStateArgs)
        {
            if (debug) Debug.Log($"{Name()}: HandleServerConnectionState()");
            OnServerConnectionState?.Invoke(connectionStateArgs);
        }
        /// <summary>
        /// Handles a ConnectionStateArgs for a remote client.
        /// </summary>
        /// <param name="connectionStateArgs"></param>
        public override void HandleRemoteConnectionState(RemoteConnectionStateArgs connectionStateArgs)
        {
            if (debug) Debug.Log($"{Name()}: HandleRemoteConnectionState({connectionStateArgs.ConnectionId})");
            OnRemoteConnectionState?.Invoke(connectionStateArgs);
        }
       
        /// <summary>
        /// Processes data received by the socket.
        /// </summary>
        /// <param name="server">True to process data received on the server.</param>
        public override void IterateIncoming(bool server)
        {
            // if (debug) Debug.Log($"{Name()}: IterateIncoming({server})");
            controller.IterateIncoming(this);
        }

        /// <summary>
        /// Processes data to be sent by the socket.
        /// </summary>
        /// <param name="server">True to process data received on the server.</param>
        public override void IterateOutgoing(bool server)
        {
            // if (debug) Debug.Log($"{Name()}: IterateOutgoing({server})");
            // Nothing to do, everything has been sent already.
        }
     
        /// <summary>
        /// Called when client receives data.
        /// </summary>
        public override event Action<ClientReceivedDataArgs> OnClientReceivedData;
        /// <summary>
        /// Handles a ClientReceivedDataArgs.
        /// </summary>
        /// <param name="receivedDataArgs"></param>
        public override void HandleClientReceivedDataArgs(ClientReceivedDataArgs receivedDataArgs)
        {
            if (debug) Debug.Log($"{Name()}: HandleClientReceivedDataArgs()");
            OnClientReceivedData?.Invoke(receivedDataArgs);
        }
        /// <summary>
        /// Called when server receives data.
        /// </summary>
        public override event Action<ServerReceivedDataArgs> OnServerReceivedData;
        /// <summary>
        /// Handles a ClientReceivedDataArgs.
        /// </summary>
        /// <param name="receivedDataArgs"></param>
        public override void HandleServerReceivedDataArgs(ServerReceivedDataArgs receivedDataArgs)
        {
            if (debug) Debug.Log($"{Name()}: HandleServerReceivedDataArgs()");
            OnServerReceivedData?.Invoke(receivedDataArgs);
        }
       
        /// <summary>
        /// Sends to the server or all clients.
        /// </summary>
        /// <param name="channelId">Channel to use.</param>
        /// <param name="segment">Data to send.</param>
        // xxxjack [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
            if (debug) Debug.Log($"{Name()}: SendToServer()");
            SanitizeChannel(ref channelId);
            controller.SendToServer(channelId, segment);
        }
        /// <summary>
        /// Sends data to a client.
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="segment"></param>
        /// <param name="connectionId"></param>
        // xxxjack [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SendToClient(byte channelId, ArraySegment<byte> segment, int connectionId)
        {
            if (debug) Debug.Log($"{Name()}: SendToClient()");
            SanitizeChannel(ref channelId);
            controller.SendToClient(channelId, segment, connectionId);
        }
  
        /// <summary>
        /// How long in seconds until either the server or client socket must go without data before being timed out.
        /// </summary>
        /// <param name="asServer">True to get the timeout for the server socket, false for the client socket.</param>
        /// <returns></returns>
        public override float GetTimeout(bool asServer)
        {
            if (debug) Debug.Log($"{Name()}: GetTimeout()");
            return -1f;
        }
        /// <summary>
        /// Returns the maximum number of clients allowed to connect to the server. If the transport does not support this method the value -1 is returned.
        /// </summary>
        /// <returns></returns>
        public override int GetMaximumClients()
        {
            if (debug) Debug.Log($"{Name()}: GetMaximumClients()");
            return -1;
        }
        /// <summary>
        /// Sets maximum number of clients allowed to connect to the server. If applied at runtime and clients exceed this value existing clients will stay connected but new clients may not connect.
        /// </summary>
        /// <param name="value"></param>
        public override void SetMaximumClients(int value)
        {
            if (debug) Debug.Log($"{Name()}: SetMaximumClients({value})");
            
        }
        /// <summary>
        /// Sets which address the client will connect to.
        /// </summary>
        /// <param name="address"></param>
        public override void SetClientAddress(string address)
        {
            if (debug) Debug.Log($"{Name()}: SetClientAddress({address})");
#if xxxjack
            _clientAddress = address;
#endif
        }


        /// <summary>
        /// Starts the local server or client using configured settings.
        /// </summary>
        /// <param name="server">True to start server.</param>
        public override bool StartConnection(bool server)
        {
            if (debug) Debug.Log($"{Name()}: StartConnection({server})");
            if (server) {
                _serverConnectionState = LocalConnectionState.Started;
                ServerConnectionStateArgs args = new(_serverConnectionState, base.Index);
                HandleServerConnectionState(args);
            }
            else
            {
                _clientConnectionState = LocalConnectionState.Started;
                ClientConnectionStateArgs args = new(_clientConnectionState, base.Index);
                HandleClientConnectionState(args);
            }
            return true;
        }

        /// <summary>
        /// Stops the local server or client.
        /// </summary>
        /// <param name="server">True to stop server.</param>
        public override bool StopConnection(bool server)
        {
            if (debug) Debug.Log($"{Name()}: StopConnection({server})");
            if (server)
                _serverConnectionState = LocalConnectionState.Stopped;
            else
                _clientConnectionState = LocalConnectionState.Stopped;
            return true;
        }

        /// <summary>
        /// Stops a remote client from the server, disconnecting the client.
        /// </summary>
        /// <param name="connectionId">ConnectionId of the client to disconnect.</param>
        /// <param name="immediately">True to abrutly stp the client socket without waiting socket thread.</param>
        public override bool StopConnection(int connectionId, bool immediately)
        {
            if (debug) Debug.Log($"{Name()}: StopConnection(...)");
            return StopClient(connectionId, immediately);
        }

        /// <summary>
        /// Stops both client and server.
        /// </summary>
        public override void Shutdown()
        {
            //Stops client then server connections.
            StopConnection(false);
            StopConnection(true);
        }

        /// <summary>
        /// Starts server.
        /// </summary>
        private bool StartServer()
        {
            if (debug) Debug.Log($"{Name()}: StartServer()");
#if xxxjack
            SslConfiguration config;
#if UNITY_SERVER
            config = _sslConfiguration;
#else
            config = new SslConfiguration();
#endif
            _server.Initialize(this, _mtu, config);
            return _server.StartConnection(_port, _maximumClients);
#else
            return true;
#endif
        }

        /// <summary>
        /// Stops server.
        /// </summary>
        private bool StopServer()
        {
            if (debug) Debug.Log($"{Name()}: StopServer()");
#if xxxjack
            return _server.StopConnection();
#else
            return true;
#endif
        }

        /// <summary>
        /// Starts the client.
        /// </summary>
        /// <param name="address"></param>
        private bool StartClient(string address)
        {
            if (debug) Debug.Log($"{Name()}: StartClient()");
#if xxxjack
            _client.Initialize(this, _mtu);
            return _client.StartConnection(address, _port, _useWss);
#else
            return true;
#endif
        }

        /// <summary>
        /// Stops the client.
        /// </summary>
        private bool StopClient()
        {
            if (debug) Debug.Log($"{Name()}: StopClient()");
#if xxxjack
            return _client.StopConnection();
#else
            return true;
#endif
        }

        /// <summary>
        /// Stops a remote client on the server.
        /// </summary>
        /// <param name="connectionId"></param>
        /// <param name="immediately">True to abrutly stp the client socket without waiting socket thread.</param>
        private bool StopClient(int connectionId, bool immediately)
        {
            if (debug) Debug.Log($"{Name()}: StopClient()");
            return true;
        }
       
        /// <summary>
        /// If channelId is invalid then channelId becomes forced to reliable.
        /// </summary>
        /// <param name="channelId"></param>
        private void SanitizeChannel(ref byte channelId)
        {
            if (channelId < 0 || channelId >= TransportManager.CHANNEL_COUNT)
            {
                base.NetworkManager.LogWarning($"Channel of {channelId} is out of range of supported channels. Channel will be defaulted to reliable.");
                channelId = 0;
            }
        }
        /// <summary>
        /// Gets the MTU for a channel. This should take header size into consideration.
        /// For example, if MTU is 1200 and a packet header for this channel is 10 in size, this method should return 1190.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public override int GetMTU(byte channel)
        {
            if (debug) Debug.Log($"{Name()}: GetMTU()");
            return _mtu;
        }
        public void VRTHandleDataReceivedViaOrchestrator(bool toServer, int connectionId, byte channelId, byte[] payload) {
            if (debug) Debug.Log($"{Name()}: HandleDataReceivedViaOrchestrator()");
            if(toServer) {
                if (_serverConnectionState != LocalConnectionState.Started) {
                    Debug.LogWarning($"{Name()}: VRTHandleDataReceivedViaOrchestrator: incoming message for server, but it is not started");
                }
                ServerReceivedDataArgs args = new() {
                    Channel=(Channel)channelId,
                    Data=payload,
                    ConnectionId=connectionId,
                    TransportIndex=base.Index

                };
                HandleServerReceivedDataArgs(args);
            }
            else
            {
                if (_clientConnectionState != LocalConnectionState.Started) {
                    Debug.LogWarning($"{Name()}: VRTHandleDataReceivedViaOrchestrator: incoming message for client, but it is not started");
                }
                ClientReceivedDataArgs args = new() {
                    Channel=(Channel)channelId,
                    Data=payload,
                    TransportIndex=base.Index
                };
                HandleClientReceivedDataArgs(args);
            }
        }

        public void VRTHandleConnectedViaOrchestrator(int connectionId) {
            if (_serverConnectionState != LocalConnectionState.Started) {
                Debug.LogWarning($"{Name()}: VRTHandleConnectedViaOrchestrator: incoming connection for server, but it is not started");
            }
            HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started, connectionId, base.Index));       
        }

        public bool VRTIsConnected(bool server) {
            if (server) 
            {
                return _serverConnectionState == LocalConnectionState.Started;
            }
            else
            {
                return _clientConnectionState == LocalConnectionState.Started;
            }
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (debug) Debug.Log($"{Name()}: OnValidate()");
            
        }
#endif
        void xxx() {
        
        }
   
    }
}
