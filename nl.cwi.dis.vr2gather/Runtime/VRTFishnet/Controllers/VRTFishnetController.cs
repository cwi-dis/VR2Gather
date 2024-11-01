using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;
using VRT.Orchestrator.Wrapping;
using VRT.Pilots.Common;
using System;
using VRT.Orchestrator.Elements;

namespace VRT.Fishnet {
    public class VRTFishnetController : NetworkIdBehaviour
    {
        [Tooltip("How long to wait after start of session to startup Fishnet")]
        [SerializeField] private float startUpTimeDelayInSeconds = 3.0f;

        [Tooltip("Introspection: the Fishnet network manager")]
        [SerializeField]
        private NetworkManager _networkManager;

        [Tooltip("Introspection: clientState as seen by Fishnet")]
        [SerializeField]
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
    
        [Tooltip("Introspection: serverState as seen by Fishnet")]
        [SerializeField]
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;

        

       
        [Tooltip("Introspection: enable debug messages")]
        [SerializeField] bool debug;

        [Tooltip("Introspection: client connections have been forwarded to Fishnet")]
        [SerializeField] bool didForwardConnectionRequests = false;
    public class FishnetStartupData : BaseMessage
        {
            public byte dummy;
        };

        public class FishnetMessage : BaseMessage
        {
            public bool toServer;
            public int connectionId;
            public byte channelId;
            public byte[] fishnetPayload;
        };

        Queue<FishnetMessage> incomingMessages = new();

        protected override void Awake()
        {
            base.Awake();
            OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_FishnetStartupData, typeof(FishnetStartupData));
            OrchestratorController.Instance.RegisterEventType(MessageTypeID.TID_FishnetMessage, typeof(FishnetMessage));
            if (_networkManager == null) {
                _networkManager = FindObjectOfType<NetworkManager>();
            }
            if (_networkManager == null)
            {
                Debug.LogError($"{Name()}: Fishnet NetworkManager not found");
            }
            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
        
        }

        new void OnDestroy()
        {
            if (_networkManager == null)
                return;

            _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
            base.OnDestroy();
        }

        public virtual void OnEnable()
        {
            OrchestratorController.Instance.Subscribe<FishnetStartupData>(StartFishnetClient);
            OrchestratorController.Instance.Subscribe<FishnetMessage>(FishnetMessageReceived);
        }


        public virtual void OnDisable()
        {
            OrchestratorController.Instance.Unsubscribe<FishnetStartupData>(StartFishnetClient);
            if (_clientState != LocalConnectionState.Stopped) {
                if (debug) Debug.Log($"{Name()}: Stopping client");
                _networkManager.ClientManager.StopConnection();
            }
            if (_serverState != LocalConnectionState.Stopped) {
                if (debug) Debug.Log($"{Name()}: Stopping server");
                _networkManager.ServerManager.StopConnection(true);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            if (debug) Debug.Log($"{Name()}: Starting VRTFishnetController");

            if (OrchestratorController.Instance.UserIsMaster) {
                if (debug) Debug.Log($"{Name()}: Firing Startup Coroutine");

                StartCoroutine("FishnetStartup");
            }
        }

        
        public string Name()
        {
            return GetType().Name;
        }
        private IEnumerator FishnetStartup()
        {
            yield return new WaitForSecondsRealtime(startUpTimeDelayInSeconds);
            
            StartFishnetServer();

            yield return new WaitForSecondsRealtime(2.0f);
            
            BroadcastFishnetServerAddress();  
        }

        void StartFishnetServer() {
            if (_serverState != LocalConnectionState.Started) {
                
                if (debug) Debug.Log($"{Name()}: Starting Fishnet server on VR2Gather master.");
                _networkManager.ServerManager.StartConnection();
            }
        
        }

        void BroadcastFishnetServerAddress() {
            // xxxjack This will only be run on the master. Use a VR2Gather Orchestrator message to have all session participants call StartFishnetClient.
            // xxxjack maybe we ourselves (the master) have to call it also, need to check.
            FishnetStartupData serverData = new();
            OrchestratorController.Instance.SendTypeEventToAll(serverData);
            // We don't start the fishnet client yet, we do that later, after we have told
            // the fishnet server about this connection.
            // StartFishnetClient(serverData);
        }

        void StartFishnetClient(FishnetStartupData server) {
            // xxxjack this is going to need at least one argument (the address of the Fishnet server)
            if (debug) Debug.Log($"{Name()}: Starting Fishnet client");
            if (_clientState != LocalConnectionState.Stopped) {
                Debug.LogWarning($"{Name()}: StartFishnetClient called, but clientState=={_clientState}");
            }
            if (_clientState == LocalConnectionState.Stopped) {
                _networkManager.ClientManager.StartConnection();
            }
        }
        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj)
        {
            if (debug) Debug.Log($"{Name()}: ClientManager_OnClientConnectionState: state={obj.ConnectionState}");
            _clientState = obj.ConnectionState;
        }


        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj)
        {
            if(obj.ConnectionState == LocalConnectionState.Stopped)
            {
                Debug.LogError($"{Name()}: ServerManager_OnServerConnectionState: state={obj.ConnectionState}");
            }
            else
            {
                if (debug) Debug.Log($"{Name()}: ServerManager_OnServerConnectionState: state={obj.ConnectionState}");
            }
            
            _serverState = obj.ConnectionState;
        }
        
        void FishnetMessageReceived(FishnetMessage message) 
        {
            string senderId = message.SenderId;
            string connectionOwnerId = OrchestratorController.Instance.CurrentSession.GetUserByIndex(message.connectionId).userId;
            if (senderId != connectionOwnerId) {
                Debug.LogWarning($"{Name()}: FishnetMessageReceived: connectionId {message.connectionId} is owned by {connectionOwnerId} but got message from {senderId}");
            }
            if (senderId != connectionOwnerId ) {

            }
            if (debug) Debug.Log($"{Name()}: FishnetMessageReceived(connectionId={message.connectionId}, toServer={message.toServer}, {message.channelId}, {message.fishnetPayload.Length} bytes)");
            incomingMessages.Enqueue(message);
        }

        public void SendToServer(byte channelId, ArraySegment<byte> segment)
        {
            string userId = OrchestratorController.Instance.SelfUser.userId;
            int connectionId = OrchestratorController.Instance.CurrentSession.GetUserIndex(userId);
            FishnetMessage message = new() {
                toServer = true,
                connectionId = connectionId,
                channelId = channelId,
                fishnetPayload = segment.ToArray()
            };
            if (debug) Debug.Log($"{Name()}: SendToServer(connectionId={connectionId}, channelId={channelId}, {message.fishnetPayload.Length} bytes)");
            // The orchestrator receiver code filters out messages coming from self.
            // So we short-circuit that here.
            if (OrchestratorController.Instance.UserIsMaster) {
                if (debug) Debug.Log($"{Name()}: SendToServer: Short-circuit message to self, we are master.");
                message.SenderId = userId;
                FishnetMessageReceived(message);
            }
            else
            {
                OrchestratorController.Instance.SendTypeEventToMaster<FishnetMessage>(message);
            }        
        }
        
        public void SendToClient(byte channelId, ArraySegment<byte> segment, int connectionId)
        {
            User user = OrchestratorController.Instance.CurrentSession.GetUserByIndex(connectionId);
            string userId = user.userId;
            FishnetMessage message = new() {
                toServer = false,
                connectionId = connectionId,
                channelId = channelId,
                fishnetPayload = segment.ToArray()
            };
            if (debug) Debug.Log($"{Name()}: SendToClient(channelId={channelId}, {message.fishnetPayload.Length} bytes, connectionId={connectionId}) -> {userId}");
            // The orchestrator receiver code filters out messages coming from self.
            // So we short-circuit that here.
            if (userId == OrchestratorController.Instance.SelfUser.userId) {
                if (debug) Debug.Log($"{Name()}: SendToClient: Short-circuit message to self.");
                message.SenderId = userId;
                FishnetMessageReceived(message);
            }
            else
            {
                OrchestratorController.Instance.SendTypeEventToUser<FishnetMessage>(userId, message);
            }
        }

        public bool IterateIncoming(VRTFishnetTransport transport) {
            // process all connection requests, if not done yet.
            if (!didForwardConnectionRequests && transport.VRTIsConnected(true)) {
                if (debug) Debug.Log($"{Name()}: IterateIncoming: forward new connections to {transport.Name()}");
                
                for (int connectionId = 0; connectionId < OrchestratorController.Instance.CurrentSession.GetUserCount(); connectionId++) {
                    transport.VRTHandleConnectedViaOrchestrator(connectionId);
                }
                
                // For the instance hosting the server (the VRT session master) we also need to start the
                // fishnet client
                FishnetStartupData serverData = new();
                StartFishnetClient(serverData);

                didForwardConnectionRequests = true;
            }
            // xxxjack process all messages in the queue
            FishnetMessage message;
            while (incomingMessages.TryDequeue(out message)) {
                if (debug) Debug.Log($"{Name()}: IterateIncoming: forward message to {transport.Name()}");
                transport.VRTHandleDataReceivedViaOrchestrator(message.toServer, message.connectionId, message.channelId, message.fishnetPayload);     
            }
            return true;
        }

        public string GetConnectionAddress(int connectionId) {
            User user = OrchestratorController.Instance.CurrentSession.GetUserByIndex(connectionId);
            return user.userId;
        }
    }

}