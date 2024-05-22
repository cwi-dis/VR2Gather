using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;

using VRT.Orchestrator.WSManagement;
using VRT.Orchestrator.Responses;

namespace VRT.Orchestrator.Wrapping {
    public class OSSOrchestratorWrapper : IOrchestratorConnectionListener, IMessagesListener
    {
        private SocketIOUnity Socket;

        public OSSOrchestratorWrapper instance;
        // Listener for the responses of the orchestrator
        private IOrchestratorResponsesListener ResponsesListener;

        // Listener for the messages of the orchestrator
        private IOrchestratorMessagesListener MessagesListener;

        // Listener for the messages emitted spontaneously by the orchestrator
        private IUserMessagesListener UserMessagesListener;

        // Listeners for the user events emitted when a session is updated by the orchestrator
        private List<IUserSessionEventsListener> UserSessionEventslisteners;

        public OSSOrchestratorWrapper(string orchestratorSocketUrl, IOrchestratorResponsesListener responsesListener, IOrchestratorMessagesListener messagesListener, IUserMessagesListener userMessagesListener, IUserSessionEventsListener userSessionEventsListener)
        {
            ResponsesListener = responsesListener;
            MessagesListener = messagesListener;
            UserMessagesListener = userMessagesListener;

            UserSessionEventslisteners = new List<IUserSessionEventsListener> {
                userSessionEventsListener
            };

            Socket = new SocketIOUnity(new Uri(orchestratorSocketUrl), new SocketIOOptions {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                EIO = 4
            });
            Socket.JsonSerializer = new NewtonsoftJsonSerializer();

            Socket.OnConnected += (sender, e) => OnSocketConnect();
            Socket.OnDisconnected += (sender, e) => OnSocketDisconnect();
            Socket.OnError += (sender, e) => OnSocketError(null);
        }

        public void Connect() {
            Socket.Connect();
        }

        public void OnSocketConnect()
        {
            if (ResponsesListener == null)
            {
                Debug.LogWarning($"OrchestratorWrapper: OnSocketConnect: no ResponsesListener");
            }
            else
            {
                ResponsesListener.OnConnect();
            }
        }

        public void Disconnect() {
            Socket.Disconnect();        
        }

        public void OnSocketDisconnect()
        {
            if (ResponsesListener == null)
            {
                Debug.LogWarning($"OrchestratorWrapper: OnSocketDisconnect: no ResponsesListener");
            }
            else
            { 
              ResponsesListener.OnDisconnect();
            }
        }

        public void OnOrchestratorRequest(string request)
        {
            throw new NotImplementedException();
        }

        public void OnOrchestratorResponse(int commandID, int status, string response)
        {
            throw new NotImplementedException();
        }

        public void OnSocketConnecting()
        {
            throw new NotImplementedException();
        }

        public void OnSocketError(ResponseStatus message)
        {
            throw new NotImplementedException();
        }

        #region utility requests

        public void GetOrchestratorVersion() {
            lock (this) {
                Socket.Emit("GetOrchestratorVersion", (response) => {
                    var data = response.GetValue<OrchestratorResponse<OrchestratorVersionResponse>>();
                    ResponsesListener?.OnGetOrchestratorVersionResponse(data.ResponseStatus, data.body.orchestratorVersion);
                }, new { });
            }
        }

        public void GetNTPTime() {
            lock (this) {
                Socket.Emit("GetNTPTime", (response) => {
                    var data = response.GetValue<OrchestratorResponse<NtpClock>>();
                    ResponsesListener?.OnGetNTPTimeResponse(data.ResponseStatus, data.body);
                }, new { });
            }
        }

        #endregion
    }
}
