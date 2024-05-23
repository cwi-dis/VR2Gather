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
                    var data = response.GetValue<OrchestratorResponse<VersionResponse>>();
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

        #region login/logout

        public void Login(string username, string password) {
            lock (this) {
                Socket.Emit("Login", (response) => {
                    var data = response.GetValue<OrchestratorResponse<LoginResponse>>();
                    ResponsesListener?.OnLoginResponse(data.ResponseStatus, data.body.userId);
                }, new {
                    userName = username
                });
            }
        }

        public void Logout() {
            lock (this) {
                Socket.Emit("Logout", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    ResponsesListener?.OnLogoutResponse(data.ResponseStatus);
                }, new { });
            }
        }

        #endregion

        #region session management

        public void AddSession(string scenarioId, Scenario scenario, string sessionName, string sessionDescription, string sessionProtocol) {
            lock (this) {
                Socket.Emit("AddSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();
                    ResponsesListener?.OnAddSessionResponse(data.ResponseStatus, data.body);
                }, new {
                    sessionName,
                    sessionDescription,
                    sessionProtocol,
                    scenarioDefinition = new {
                        scenarioId,
                        scenario.scenarioName,
                        scenario.scenarioDescription
                    }
                });
            }
        }

        public void GetSessions() {
            lock (this) {
                Socket.Emit("GetSessions", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Dictionary<string, Session>>>();

                    var sessions = new List<Session>();
                    foreach (var item in data.body) {
                        sessions.Add(item.Value);
                    }

                    ResponsesListener?.OnGetSessionsResponse(data.ResponseStatus, sessions);
                }, new { });
            }
        }

        public void GetSessionInfo() {
            lock (this) {
                Socket.Emit("GetSessionInfo", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();
                    ResponsesListener?.OnGetSessionInfoResponse(data.ResponseStatus, data.body);
                }, new { });
            }
        }

        public void DeleteSession(string sessionId) {
            lock (this) {
                Socket.Emit("DeleteSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    ResponsesListener?.OnDeleteSessionResponse(data.ResponseStatus);
                }, new {
                    sessionId
                });
            }
        }

        public void JoinSession(string sessionId) {
            lock (this) {
                Socket.Emit("JoinSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();
                    ResponsesListener?.OnJoinSessionResponse(data.ResponseStatus, data.body);
                }, new {
                    sessionId
                });
            }
        }

        public void LeaveSession() {
            lock (this) {
                Socket.Emit("LeaveSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    ResponsesListener?.OnLeaveSessionResponse(data.ResponseStatus);
                }, new { });
            }
        }

        public void SendMessage(string message, string userId) {
            lock (this) {
                Socket.Emit("SendMessage", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    ResponsesListener?.OnSendMessageResponse(data.ResponseStatus);
                }, new {
                    message,
                    userId
                });
            }
        }

        public void SendMessageToAll(string message) {
            lock (this) {
                Socket.Emit("SendMessageToAll", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    ResponsesListener?.OnSendMessageResponse(data.ResponseStatus);
                }, new {
                    message
                });
            }
        }

        public void UpdateUserDataJson(UserData userData) {
            lock (this) {
                Socket.Emit("UpdateUserDataJson", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    ResponsesListener?.OnUpdateUserDataJsonResponse(data.ResponseStatus);
                }, new {
                    userDataJson = userData.AsJsonString()
                });
            }
        }

        #endregion

        #region scene events

        public void SendSceneEventPacketToMaster(byte[] pByteArray) {
            lock (this) {
                Socket.Emit("SendSceneEventToMaster", 
                    pByteArray
                );
            }
        }

        public void SendSceneEventPacketToUser(string pUserID, byte[] pByteArray) {
            lock (this) {
                Socket.Emit("SendSceneEventToUser", new {
                    pUserID, pByteArray
                });
            }
        }

        public void SendSceneEventPacketToAllUsers(byte[] pByteArray) {
            lock (this) {
                Socket.Emit("SendSceneEventToAllUsers", new {
                    pByteArray
                });
            }
        }

        #endregion
    }
}
