using System;
using System.Collections.Generic;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using UnityEngine;
using System.Text;

using VRT.Orchestrator.Responses;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace VRT.Orchestrator.Wrapping {
    public class TaskQueue
    {
        private ConcurrentQueue<Task> _tasks = new ConcurrentQueue<Task>();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);
        private Task _processor;
        private bool _queueActive;

        public TaskQueue()
        {
            _processor = Task.Run(async () => await ProcessQueueAsync());
        }

        public void Enqueue(Task asyncAction)
        {
            _tasks.Enqueue(asyncAction);
            _signal.Release();
        }

        private async Task ProcessQueueAsync()
        {
            while (_queueActive)
            {
                await _signal.WaitAsync();
                if (_tasks.TryDequeue(out var task))
                {
                    await task;
                }
            }

            Debug.Log("TaskQueue terminated");
        }

        public void CloseQueue() {
            _queueActive = false;
        }
    }

    public class OSSOrchestratorWrapper : IOrchestratorConnectionListener, IMessagesListener
    {
        private SocketIOUnity Socket;
        private readonly object sendLock = new();
        private TaskQueue _taskQueue = new TaskQueue();

        public static OSSOrchestratorWrapper instance;
        // Listener for the responses of the orchestrator
        private IOrchestratorResponsesListener ResponsesListener;

        // Listener for the messages of the orchestrator
        private IOrchestratorMessagesListener MessagesListener;

        // Listener for the messages emitted spontaneously by the orchestrator
        private IUserMessagesListener UserMessagesListener;

        // Listeners for the user events emitted when a session is updated by the orchestrator
        private List<IUserSessionEventsListener> UserSessionEventslisteners;

        public Action<UserDataStreamPacket> OnDataStreamReceived;
        private string myUserID = "";

        public OSSOrchestratorWrapper(string orchestratorSocketUrl, IOrchestratorResponsesListener responsesListener, IOrchestratorMessagesListener messagesListener, IUserMessagesListener userMessagesListener, IUserSessionEventsListener userSessionEventsListener)
        {
            if (instance is null)
            {
                instance = this;
            }

            ResponsesListener = responsesListener;
            MessagesListener = messagesListener;
            UserMessagesListener = userMessagesListener;

            UserSessionEventslisteners = new List<IUserSessionEventsListener> {
                userSessionEventsListener
            };

            Socket = new SocketIOUnity(new Uri(orchestratorSocketUrl), new SocketIOOptions {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                Reconnection = false,
                EIO = EngineIO.V4
            });
            Socket.JsonSerializer = new NewtonsoftJsonSerializer();

            Socket.OnConnected += (sender, e) => OnSocketConnect();
            Socket.OnDisconnected += (sender, e) =>
            {
                OnSocketDisconnect();
            };
            Socket.OnError += (sender, e) =>
            {
                Debug.LogError($"ERROR: {e}");
                OnSocketError(null);
            };

            Socket.OnPing += (sender, e) => {
                Debug.Log("PING");
            };

            Socket.OnPong += (sender, e) => {
                Debug.Log("PoNG");
            };

            Socket.On("MessageSent", OnMessageSentFromOrchestrator);
            Socket.On("DataReceived", OnUserDataReceived);
            Socket.On("SceneEventToMaster", OnMasterEventReceived);
            Socket.On("SceneEventToUser", OnUserEventReceived);
            Socket.On("SessionUpdated", OnSessionUpdated);
        }

        public void Connect() {
            Socket.Connect();
            OnSocketConnecting();
        }

        public void EnableSocketioLogging() { }

        public void OnSocketConnect()
        {
            if (ResponsesListener == null)
            {
                Debug.LogWarning($"OrchestratorWrapper: OnSocketConnect: no ResponsesListener");
            }
            else
            {
                Debug.Log("Calling OnConnect");
                UnityThread.executeInUpdate(() => {
                  ResponsesListener.OnConnect();
                });
            }
        }

        public void Disconnect() {
            Debug.Log("DISCONNECT called");
            _taskQueue.CloseQueue();
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
                UnityThread.executeInUpdate(() => {
                    ResponsesListener.OnDisconnect();
                });
            }
        }

        public void OnOrchestratorRequest(string request)
        {
            Debug.LogWarning($"OnOrchestratorRequest called: {request}");
            throw new NotImplementedException();
        }

        public void OnOrchestratorResponse(int commandID, int status, string response)
        {
            Debug.LogWarning($"OnOrchestratorResponse called: {commandID} {status} {response}");
            throw new NotImplementedException();
        }

        public void OnSocketConnecting()
        {
            UnityThread.executeInUpdate(() =>
            {
                ResponsesListener?.OnConnecting();
            });
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
                    
                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnGetOrchestratorVersionResponse(data.ResponseStatus, data.body.orchestratorVersion);
                    });
                }, new { });
            }
        }

        public void GetNTPTime() {
            lock (this) {
                Socket.Emit("GetNTPTime", (response) => {
                    var data = response.GetValue<OrchestratorResponse<NtpClock>>();

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnGetNTPTimeResponse(data.ResponseStatus, data.body);
                    });
                }, new { });
            }
        }

        #endregion

        #region login/logout

        public void Login(string username, string password) {
            lock (this) {
                Socket.Emit("Login", (response) => {
                    var data = response.GetValue<OrchestratorResponse<LoginResponse>>();
                    myUserID = data.body.userId;

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnLoginResponse(data.ResponseStatus, data.body.userId);
                    });
                }, new {
                    userName = username
                });
            }
        }

        public void Logout() {
            lock (this) {
                Socket.Emit("Logout", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();
                    myUserID = "";

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnLogoutResponse(data.ResponseStatus);
                    });
                }, new { });
            }
        }

        #endregion

        #region session management

        public void AddSession(string scenarioId, Scenario scenario, string sessionName, string sessionDescription, string sessionProtocol) {
            lock (this) {
                Socket.Emit("AddSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnAddSessionResponse(data.ResponseStatus, data.body);
                    });
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

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnGetSessionsResponse(data.ResponseStatus, sessions);
                    });
                }, new { });
            }
        }

        public void GetSessionInfo() {
            lock (this) {
                Socket.Emit("GetSessionInfo", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnGetSessionInfoResponse(data.ResponseStatus, data.body);
                    });
                }, new { });
            }
        }

        public void DeleteSession(string sessionId) {
            lock (this) {
                Socket.Emit("DeleteSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnDeleteSessionResponse(data.ResponseStatus);
                    });
                }, new {
                    sessionId
                });
            }
        }

        public void JoinSession(string sessionId) {
            lock (this) {
                Socket.Emit("JoinSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<Session>>();

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnJoinSessionResponse(data.ResponseStatus, data.body);
                    });
                }, new {
                    sessionId
                });
            }
        }

        public void LeaveSession() {
            lock (this) {
                Socket.Emit("LeaveSession", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnLeaveSessionResponse(data.ResponseStatus);
                    });
                }, new { });
            }
        }

        public void SendMessage(string message, string userId) {
            lock (this) {
                Socket.Emit("SendMessage", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnSendMessageResponse(data.ResponseStatus);
                    });
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

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnSendMessageResponse(data.ResponseStatus);
                    });
                }, new {
                    message
                });
            }
        }

        public void UpdateUserDataJson(UserData userData) {
            lock (this) {
                Socket.Emit("UpdateUserDataJson", (response) => {
                    var data = response.GetValue<OrchestratorResponse<EmptyResponse>>();

                    UnityThread.executeInUpdate(() => {
                        ResponsesListener?.OnUpdateUserDataJsonResponse(data.ResponseStatus);
                    });
                }, new {
                    userDataJson = userData.AsJsonString()
                });
            }
        }

        #endregion

        #region scene events

        public void SendSceneEventPacketToMaster(byte[] pByteArray) {
            lock (sendLock) {
                _taskQueue.Enqueue(Socket.EmitAsync("SendSceneEventToMaster",
                    pByteArray
                ));
            }
        }

        public void SendSceneEventPacketToUser(string pUserID, byte[] pByteArray) {
            lock (sendLock) {
                _taskQueue.Enqueue(Socket.EmitAsync("SendSceneEventToUser",
                    pUserID, pByteArray
                ));
            }
        }

        public void SendSceneEventPacketToAllUsers(byte[] pByteArray) {
            lock (sendLock) {
                _taskQueue.Enqueue(Socket.EmitAsync("SendSceneEventToAllUsers",
                    pByteArray
                ));
            }
        }

        #endregion

        #region data streams

        public void DeclareDataStream(string pDataStreamType) {
            lock (this) {
                Socket.Emit("DeclareDataStream", pDataStreamType);
            }
        }

        public void RemoveDataStream(string pDataStreamType) {
            lock (this) {
                Socket.Emit("RemoveDataStream", pDataStreamType);
            }
        }

        public void RegisterForDataStream(string pDataStreamUserId, string pDataStreamType) {
            lock (this) {
                Socket.Emit("RegisterForDataStream", pDataStreamUserId, pDataStreamType);
            }
        }

        public void UnregisterFromDataStream(string pDataStreamUserId, string pDataStreamType) {
            lock (this) {
                Socket.Emit("UnregisterFromDataStream", pDataStreamUserId, pDataStreamType);
            }
        }

        public void SendData(string pDataStreamType, byte[] pDataStreamBytes) {
            lock (this) {
                Socket.Emit("SendData", pDataStreamType, pDataStreamBytes);
            }
        }

        #endregion

        #region events

        private void OnMessageSentFromOrchestrator(SocketIOResponse response) {
            lock (this) {
                var message = response.GetValue<UserMessage>();
                UnityThread.executeInUpdate(() => {
                    UserMessagesListener?.OnUserMessageReceived(message);
                });
            }
        }

        private void OnUserDataReceived(SocketIOResponse response) {
            lock (this) {
                var userId = response.GetValue<string>(0);
                var type = response.GetValue<string>(1);
                var data = response.GetValue<byte[]>(2);

                var packet = new UserDataStreamPacket(userId, type, "", data);
                Debug.Log($"DATA: {userId} {type} {data}");

                UnityThread.executeInUpdate(() =>
                {
                    OnDataStreamReceived?.Invoke(packet);
                });
            }
        }

        private void OnMasterEventReceived(SocketIOResponse response) {
            lock (this) {
                if (UserMessagesListener != null)
                {
                    var sceneEvent = response.GetValue<SceneEvent>();
                    string data = Encoding.ASCII.GetString(response.InComingBytes[0], 0, response.InComingBytes[0].Length);

                    UnityThread.executeInUpdate(() =>
                    {
                        UserMessagesListener.OnMasterEventReceived(new UserEvent(sceneEvent.sceneEventFrom, data));
                    });
                }
                else {
                    Debug.LogWarning("No UserMessagesListener");
                }
            }
        }

        private void OnUserEventReceived(SocketIOResponse response) {
            lock (this) {
                if (UserMessagesListener != null) {
                    var sceneEvent = response.GetValue<SceneEvent>();
                    string data = Encoding.ASCII.GetString(response.InComingBytes[0], 0, response.InComingBytes[0].Length);


                    UnityThread.executeInUpdate(() =>
                    {
                        UserMessagesListener.OnUserEventReceived(new UserEvent(sceneEvent.sceneEventFrom, data));
                    });
                } else {
                    Debug.LogWarning("No UserMessagesListener");
                }
            }
        }

        private void OnSessionUpdated(SocketIOResponse response) {
            lock (this) {
                var data = response.GetValue<SessionUpdate>();

                if (data.eventData.userId == myUserID) {
                    return;
                }

                switch (data.eventId) {
                    case "USER_JOINED_SESSION":
                        foreach (IUserSessionEventsListener e in UserSessionEventslisteners)
                        {
                            UnityThread.executeInUpdate(() => {
                                e?.OnUserJoinedSession(data.eventData.userId, data.eventData.userData);
                            });
                        }
                        break;
                    case "USER_LEFT_SESSION":
                        foreach (IUserSessionEventsListener e in UserSessionEventslisteners)
                        {
                            UnityThread.executeInUpdate(() => {
                                e?.OnUserLeftSession(data.eventData.userId);
                            });
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion
    }
}
