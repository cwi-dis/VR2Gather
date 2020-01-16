using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using OrchestratorWSManagement;
using LitJson;
using BestHTTP;
using BestHTTP.SocketIO;
using BestHTTP.SocketIO.Events;

namespace OrchestratorWrapping
{
    // class that describes the status for the response from the orchestrator
    public class ResponseStatus
    {
        public int Error;
        public string Message;

        public ResponseStatus(int error, string message)
        {
            this.Error = error;
            this.Message = message;
        }
        public ResponseStatus() : this(0, "OK") { }
    }

    // class that stores a user message incoming from the orchestrator
    public class UserMessage
    {
        public string fromId;
        public string fromName;
        public JsonData message;
    }

    // class that stores a user audio packet incoming from the orchestrator
    public class UserAudioPacket
    {
        public byte[] audioPacket;
        public string userID;

        public UserAudioPacket(byte[] pAudioPacket, string pUserID)
        {
            if(pAudioPacket != null)
            {
                audioPacket = pAudioPacket;
                userID = pUserID;
            }
        }
    }

    //class that overrides generic UnityEvent wwith string argument
    public class UnityStringEvent : UnityEvent<string>
    {
    }

    // class that encapsulates the connection with the orchestrator, emitting and receiving the events
    // and converting and parsing the camands and the responses
    public class OrchestratorWrapper : IOrchestratorConnectionListener, IMessagesListener
    {
        public static OrchestratorWrapper instance;

        // manager for the socketIO connection to the orchestrator 
        private OrchestratorWSManager OrchestrationSocketIoManager;

        // Listener for the responses of the orchestrator
        private IOrchestratorResponsesListener ResponsesListener;

        // Listener for the responses of the orchestrator
        private IOrchestratorMessageIOListener MessagesListener;

        // Listener for the messages emitted spontaneously by the orchestrator
        private IMessagesFromOrchestratorListener MessagesFromOrchestratorListener;

        // Listeners for the user events emitted when a session is updated by the orchestrator
        private List<IUserSessionEventsListener> UserSessionEventslisteners = new List<IUserSessionEventsListener>();

        // List of available commands (grammar description)
        public List<OrchestratorCommand> orchestratorCommands { get; private set; }

        // List of messages that can be received from the orchestrator
        public List<OrchestratorMessageReceiver> orchestratorMessages { get; private set; }

        public OrchestratorWrapper(string orchestratorSocketUrl, IOrchestratorResponsesListener responsesListener, IOrchestratorMessageIOListener messagesListener, IMessagesFromOrchestratorListener messagesFromOrchestratorListener, IUserSessionEventsListener userSessionEventslistener)
        {
            if(instance == null)
            {
                instance = this;
            }

            OrchestrationSocketIoManager = new OrchestratorWSManager(orchestratorSocketUrl, this, this);
            ResponsesListener = responsesListener;
            MessagesListener = messagesListener;
            MessagesFromOrchestratorListener = messagesFromOrchestratorListener;

            UserSessionEventslisteners = new List<IUserSessionEventsListener>();
            UserSessionEventslisteners.Add(userSessionEventslistener);

            InitGrammar();
        }
        public OrchestratorWrapper(string orchestratorSocketUrl, IOrchestratorResponsesListener responsesListener, IMessagesFromOrchestratorListener messagesFromOrchestratorListener) : this(orchestratorSocketUrl, responsesListener, null, messagesFromOrchestratorListener, null) { }
        public OrchestratorWrapper(string orchestratorSocketUrl) : this(orchestratorSocketUrl, null, null, null, null) { }

        public void AddUserSessionEventLister(IUserSessionEventsListener e)
        {
            UserSessionEventslisteners.Add(e);
        }

        public Action<UserAudioPacket> OnAudioSent;

        string myUserID = "";

        #region messages listening interface implementation
        public void OnOrchestratorResponse(int status, string response)
        {
            if (MessagesListener != null) MessagesListener.OnOrchestratorResponse(status, response);
        }

        public void OnOrchestratorRequest(string request)
        {
            if (MessagesListener != null) MessagesListener.OnOrchestratorRequest(request);
        }
        #endregion

        #region commands and responses procession
        public void Connect()
        {
            if ((OrchestrationSocketIoManager != null) && (OrchestrationSocketIoManager.isSocketConnected))
            {
                OrchestrationSocketIoManager.SocketDisconnect();
            }
            OrchestrationSocketIoManager.SocketConnect(orchestratorMessages);
        }

        public void OnSocketConnect()
        {
            if (ResponsesListener != null) ResponsesListener.OnConnect();
        }

        public void Disconnect()
        {
            if (OrchestrationSocketIoManager != null)
            {
                OrchestrationSocketIoManager.SocketDisconnect();
            }
        }
        public void OnSocketDisconnect()
        {
            if (ResponsesListener != null) ResponsesListener.OnDisconnect();
        }


        public bool Login(string userName, string userPassword)
        {
            OrchestratorCommand command = GetOrchestratorCommand("Login");
            command.GetParameter("userName").ParamValue = userName;
            command.GetParameter("userPassword").ParamValue = userPassword;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnLoginResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            myUserID = response.body["userId"].ToString();
            if (ResponsesListener != null) ResponsesListener.OnLoginResponse(new ResponseStatus(response.error, response.message), myUserID);
        }

        public bool Logout()
        {
            OrchestratorCommand command = GetOrchestratorCommand("Logout");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnLogoutResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            myUserID = "";
            if (ResponsesListener != null) ResponsesListener.OnLogoutResponse(new ResponseStatus(response.error, response.message));
        }

        public bool GetNTPTime()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetNTPTime");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetNTPTimeResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            NtpClock ntpTime = NtpClock.ParseJsonData<NtpClock>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnGetNTPTimeResponse(status, ntpTime.ntpTime);
        }

        public bool AddSession(string scenarioId, string sessionName, string sessionDescription)
        {
            OrchestratorCommand command = GetOrchestratorCommand("AddSession");
            command.GetParameter("scenarioId").ParamValue = scenarioId;
            command.GetParameter("sessionName").ParamValue = sessionName;
            command.GetParameter("sessionDescription").ParamValue = sessionDescription;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnAddSessionResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            Session session = Session.ParseJsonData<Session>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnAddSessionResponse(status, session);
        }

        public bool GetSessions()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetSessions");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetSessionsResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            List<Session> list = Helper.ParseElementsList<Session>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnGetSessionsResponse(status, list);
        }

        public bool GetSessionInfo()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetSessionInfo");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetSessionInfoResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            Session session = Session.ParseJsonData<Session>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnGetSessionInfoResponse(status, session);
        }

        public bool DeleteSession(string sessionId)
        {
            OrchestratorCommand command = GetOrchestratorCommand("DeleteSession");
            command.GetParameter("sessionId").ParamValue = sessionId;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnDeleteSessionResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener != null) ResponsesListener.OnDeleteSessionResponse(status);
        }

        public bool JoinSession(string sessionId)
        {
            OrchestratorCommand command = GetOrchestratorCommand("JoinSession");
            command.GetParameter("sessionId").ParamValue = sessionId;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnJoinSessionResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener != null) ResponsesListener.OnJoinSessionResponse(status);
        }

        public bool LeaveSession()
        {
            OrchestratorCommand command = GetOrchestratorCommand("LeaveSession");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnLeaveSessionResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener != null) ResponsesListener.OnLeaveSessionResponse(status);
        }

        public bool GetLivePresenterData()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetLivePresenterData");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void GetLivePresenterDataResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            LivePresenterData liveData = LivePresenterData.ParseJsonData<LivePresenterData>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnGetLivePresenterDataResponse(status, liveData);
        }

        public bool GetScenarios()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetScenarios");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetScenariosResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            List<Scenario> list = Helper.ParseElementsList<Scenario>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnGetScenariosResponse(status, list);
        }

        public bool GetScenarioInstanceInfo(string scenarioId)
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetScenarioInstanceInfo");
            command.GetParameter("scenarioId").ParamValue = scenarioId;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetScenarioInstanceInfoResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            ScenarioInstance scenario = ScenarioInstance.ParseJsonData<ScenarioInstance>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnGetScenarioInstanceInfoResponse(status, scenario);
        }

        public bool GetUsers()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetUsers");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetUsersResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            List<User> list = Helper.ParseElementsList<User>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnGetUsersResponse(status, list);
        }

        public bool AddUser(string userName, string userPassword, bool isAdmin)
        {
            OrchestratorCommand command = GetOrchestratorCommand("AddUser");
            command.GetParameter("userName").ParamValue = userName;
            command.GetParameter("userPassword").ParamValue = userPassword;
            command.GetParameter("userAdmin").ParamValue = isAdmin;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnAddUserResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            User user = User.ParseJsonData<User>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnAddUserResponse(status, user);
        }

        public bool GetUserInfo(string userId = "")
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetUserInfo");
            command.GetParameter("userId").ParamValue = userId;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetUserInfoResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            User user = User.ParseJsonData<User>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnGetUserInfoResponse(status, user);
        }

        public bool UpdateUserDataJson(string userMQname = "", string userMQurl = "", string userPCurl = "", string userAudioUrl = "")
        {
            UserData userData = new UserData(userMQname, userMQurl, userPCurl, userAudioUrl);
            JsonData json = JsonUtility.ToJson(userData);

            OrchestratorCommand command = GetOrchestratorCommand("UpdateUserDataJson");
            command.GetParameter("userDataJson").ParamValue = json;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnUpdateUserDataJsonResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener != null) ResponsesListener.OnUpdateUserDataJsonResponse(status);
        }

        public bool DeleteUser(string userId)
        {
            OrchestratorCommand command = GetOrchestratorCommand("DeleteUser");
            command.GetParameter("userId").ParamValue = userId;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnDeleteUserResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener != null) ResponsesListener.OnDeleteUserResponse(status);
        }

        public bool GetRooms()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetRooms");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetRoomsResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            List<RoomInstance> rooms = Helper.ParseElementsList<RoomInstance>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnGetRoomsResponse(status, rooms);
        }

        public bool JoinRoom(string roomId)
        {
            OrchestratorCommand command = GetOrchestratorCommand("JoinRoom");
            command.GetParameter("roomId").ParamValue = roomId;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnJoinRoomResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener != null) ResponsesListener.OnJoinRoomResponse(status);
        }

        public bool LeaveRoom()
        {
            OrchestratorCommand command = GetOrchestratorCommand("LeaveRoom");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnLeaveRoomResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener != null) ResponsesListener.OnLeaveRoomResponse(status);
        }

        public bool SendMessage(string message, string userId)
        {
            OrchestratorCommand command = GetOrchestratorCommand("SendMessage");
            command.GetParameter("message").ParamValue = message;
            command.GetParameter("userId").ParamValue = userId;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnSendMessageResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener != null) ResponsesListener.OnSendMessageResponse(status);
        }

        public bool SendMessageToAll(string message)
        {
            OrchestratorCommand command = GetOrchestratorCommand("SendMessageToAll");
            command.GetParameter("message").ParamValue = message;
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnSendMessageToAllResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener != null) ResponsesListener.OnSendMessageToAllResponse(status);
        }

        public void PushAudioPacket(byte[] pByteArray)
        {
            OrchestratorCommand command = GetOrchestratorCommand("PushAudio");
            command.GetParameter("audiodata").ParamValue = pByteArray;
            OrchestrationSocketIoManager.EmitPacket(command);
        }

        #endregion

        #region remote response

        // messages from the orchestrator
        private void OnMessageSentFromOrchestrator(Socket socket, Packet packet, params object[] args)
        {
            if (MessagesListener != null)
            {
                MessagesListener.OnOrchestratorResponse(0, packet.Payload);
            }
            JsonData jsonResponse = JsonMapper.ToObject(packet.Payload);

            UserMessage messageReceived = new UserMessage();
            messageReceived.fromId = jsonResponse[1]["messageFrom"].ToString();
            messageReceived.fromName = jsonResponse[1]["messageFromName"].ToString();
            messageReceived.message = jsonResponse[1]["message"];

            if (MessagesFromOrchestratorListener != null)
            {
                MessagesFromOrchestratorListener.OnUserMessageReceived(messageReceived);
            }
        }

        // audio packets from the orchestrator
        private void OnAudioSentFromOrchestrator(Socket socket, Packet packet, params object[] args)
        {
            JsonData jsonResponse = JsonMapper.ToObject(packet.Payload);
            string lUserID = jsonResponse[1]["audioFrom"].ToString();

            if (myUserID != lUserID && OnAudioSent != null)
            {
                UserAudioPacket packetReceived = new UserAudioPacket(packet.Attachments[0], lUserID);
                OnAudioSent.Invoke(packetReceived);
            }
        }

        // sessions update events from the orchestrator
        private void OnSessionUpdated(Socket socket, Packet packet, params object[] args)
        {
            JsonData jsonResponse = JsonMapper.ToObject(packet.Payload);

            if (MessagesListener != null)
            {
                MessagesListener.OnOrchestratorResponse(0, packet.Payload);
            }

            string lEventID = jsonResponse[1]["eventId"].ToString();
            string lUserID = jsonResponse[1]["eventData"][0].ToString(); ;

            if (lUserID == myUserID)
            {
                //I just joined a session, so I need to get all connected users IDs to get their audio, provided by the OnGetSessionInfoResponse callback.
                return;
            }

            switch (lEventID)
            {
                case "USER_JOINED_SESSION":

                    foreach (IUserSessionEventsListener e in UserSessionEventslisteners)
                    {
                        e?.OnUserJoinedSession(lUserID);
                    }

                    break;
                case "USER_LEAVED_SESSION":

                    foreach (IUserSessionEventsListener e in UserSessionEventslisteners)
                    {
                        e?.OnUserLeftSession(lUserID);
                    }

                    break;
                default:
                    break;
            }
        }

        #endregion

        #region grammar definition
        // declare te available commands, their parameters and the callbacks that should be used for the response of each command
        public void InitGrammar()
        {
            orchestratorCommands = new List<OrchestratorCommand>
                {              
                    //Login & Logout
                    new OrchestratorCommand("Login", new List<Parameter>
                        {
                            new Parameter("userName", typeof(string)),
                            new Parameter("userPassword", typeof(string))
                        },
                        OnLoginResponse),
                    new OrchestratorCommand("Logout", null, OnLogoutResponse),

                    //NTP
                    new OrchestratorCommand("GetNTPTime", null, OnGetNTPTimeResponse),

                    //sessions
                    new OrchestratorCommand("AddSession", new List<Parameter>
                        {
                            new Parameter("scenarioId", typeof(string)),
                            new Parameter("sessionName", typeof(string)),
                            new Parameter("sessionDescription", typeof(string))
                        },
                        OnAddSessionResponse),
                    new OrchestratorCommand("GetSessions", null, OnGetSessionsResponse),
                    new OrchestratorCommand("GetSessionInfo", null, OnGetSessionInfoResponse),
                    new OrchestratorCommand("DeleteSession", new List<Parameter>
                        {
                            new Parameter("sessionId", typeof(string)),
                        },
                        OnDeleteSessionResponse),
                    new OrchestratorCommand("JoinSession", new List<Parameter>
                        {
                            new Parameter("sessionId", typeof(string)),
                        },
                        OnJoinSessionResponse),
                    new OrchestratorCommand("LeaveSession", null, OnLeaveSessionResponse),

                    // live stream
                    new OrchestratorCommand("GetLivePresenterData", null, GetLivePresenterDataResponse),

                    // scenarios
                    new OrchestratorCommand("GetScenarios", null, OnGetScenariosResponse),
                    new OrchestratorCommand("GetScenarioInstanceInfo", new List<Parameter>
                        {
                            new Parameter("scenarioId", typeof(string))
                        },
                        OnGetScenarioInstanceInfoResponse),

                    // users
                    new OrchestratorCommand("GetUsers", null, OnGetUsersResponse),
                    new OrchestratorCommand("GetUserInfo",
                    new List<Parameter>
                        {
                            new Parameter("userId", typeof(string))
                        }, 
                        OnGetUserInfoResponse),
                    new OrchestratorCommand("AddUser", new List<Parameter>
                        {
                            new Parameter("userName", typeof(string)),
                            new Parameter("userPassword", typeof(string)),
                            new Parameter("userAdmin", typeof(bool))
                        },
                        OnAddUserResponse),
                     new OrchestratorCommand("UpdateUserDataJson", new List<Parameter>
                        {
                            new Parameter("userDataJson", typeof(string)),
                        },
                        OnUpdateUserDataJsonResponse),
                    new OrchestratorCommand("DeleteUser", new List<Parameter>
                        {
                            new Parameter("userId", typeof(string))
                        },
                        OnDeleteUserResponse),

                    // rooms
                    new OrchestratorCommand("GetRooms", null, OnGetRoomsResponse),
                    new OrchestratorCommand("JoinRoom", new List<Parameter>
                        {
                            new Parameter("roomId", typeof(string))
                        },
                        OnJoinRoomResponse),
                    new OrchestratorCommand("LeaveRoom", null, OnLeaveRoomResponse),

                    //messages
                    new OrchestratorCommand("SendMessage", new List<Parameter>
                        {
                            new Parameter("message", typeof(string)),
                            new Parameter("userId", typeof(string))
                        },
                        OnSendMessageResponse),
                    new OrchestratorCommand("SendMessageToAll", new List<Parameter>
                        {
                            new Parameter("message", typeof(string))
                        },
                        OnSendMessageToAllResponse),

                    //audio packets
                    new OrchestratorCommand("PushAudio", new List<Parameter>
                        {
                            new Parameter("audiodata", typeof(byte[]))
                        }),
                };

            orchestratorMessages = new List<OrchestratorMessageReceiver>
                {              
                    //messages
                    new OrchestratorMessageReceiver("MessageSent", OnMessageSentFromOrchestrator),
                    //audio packets
                    new OrchestratorMessageReceiver("AudioSent", OnAudioSentFromOrchestrator),
                    //session update events
                    new OrchestratorMessageReceiver("SessionUpdated", OnSessionUpdated),
                };
        }

        // To retrieve the definition of a command by name
        public OrchestratorCommand GetOrchestratorCommand(string commandName)
        {
            for (int i = 0; i < orchestratorCommands.Count; i++)
            {
                if (orchestratorCommands[i].SocketEventName == commandName)
                {
                    return orchestratorCommands[i];
                }
            }
            return null;
        }
        #endregion
    }
}