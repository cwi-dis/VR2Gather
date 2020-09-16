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
using System.Text;

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
        public readonly string fromId;
        public readonly string fromName;
        public readonly string message;

        public UserMessage(string pFromID, string pFromName, string pMessage)
        {
            fromId = pFromID;
            fromName = pFromName;
            message = pMessage;
        }
    }

    // class that stores a user event incoming from the orchestrator
    // necessary new parameters welcomed
    public class UserEvent
    {
        public readonly string fromId;
        public readonly string message;

        public UserEvent(string pFromID, string pMessage)
        {
            fromId = pFromID;
            message = pMessage;
        }
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

    // class that stores a user data-stream packet incoming from the orchestrator
    public class UserDataStreamPacket
    {
        public string dataStreamUserID;
        public string dataStreamType;
        public string dataStreamDesc;
        public byte[] dataStreamPacket;

        public UserDataStreamPacket(string pDataStreamUserID, string pDataStreamType, string pDataStreamDesc, byte[] pDataStreamPacket)
        {
            if (pDataStreamPacket != null)
            {
                dataStreamUserID = pDataStreamUserID;
                dataStreamType = pDataStreamType;
                dataStreamDesc = pDataStreamDesc;
                dataStreamPacket = pDataStreamPacket;
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
            if(instance is null)
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
        public Action<UserDataStreamPacket> OnDataStreamReceived;

        private string myUserID = "";

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

        #region commands with Acks and responses
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

        public void OnSocketConnecting()
        {
            if (ResponsesListener != null) ResponsesListener.OnConnecting();
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

        public void OnSocketError(ResponseStatus status)
        {
            if (ResponsesListener != null) ResponsesListener.OnError(status);
        }

        public bool GetOrchestratorVersion()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetOrchestratorVersion");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }

        public void OnGetOrchestratorVersionResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            string version = response.body["orchestratorVersion"].ToString();
            ResponsesListener?.OnGetOrchestratorVersionResponse(new ResponseStatus(response.error, response.message), version);
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
            try { myUserID = response.body["userId"].ToString(); }
            catch { myUserID = "";  }
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
            if (ResponsesListener != null) ResponsesListener.OnGetNTPTimeResponse(status, ntpTime);
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
            // By default canBeMaster is set to false, it needs to be overrided to be sure that a master is affected.
            command.GetParameter("canBeMaster").ParamValue = true;
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

        public bool UpdateUserDataJson(UserData userData)
        {
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

        public bool ClearUserData()
        {
            OrchestratorCommand command = GetOrchestratorCommand("ClearUserData");
            return OrchestrationSocketIoManager.EmitCommand(command);
        }
        
        private void OnClearUserDataResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener != null) ResponsesListener.OnClearUserDataResponse(status);
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

        public void GetAvailableDataStreams(string pDataStreamUserId)
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetAvailableDataStreams");
            command.GetParameter("dataStreamUserId").ParamValue = pDataStreamUserId;
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetAvailableDataStreams(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            List<DataStream> lDataStreams = Helper.ParseElementsList<DataStream>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnGetAvailableDataStreams(status, lDataStreams);
        }

        public void GetRegisteredDataStreams()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetRegisteredDataStreams");
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetRegisteredDataStreams(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            List<DataStream> lDataStreams = Helper.ParseElementsList<DataStream>(response.body);
            if (ResponsesListener != null) ResponsesListener.OnGetRegisteredDataStreams(status, lDataStreams);
        }

        #endregion

        #region commands - no Acks

        public void PushAudioPacket(byte[] pByteArray)
        {
            OrchestratorCommand command = GetOrchestratorCommand("PushAudio");
            command.GetParameter("audiodata").ParamValue = pByteArray;
            OrchestrationSocketIoManager.EmitPacket(command);
        }

        public void SendSceneEventPacketToMaster(byte[] pByteArray)
        {
            OrchestratorCommand command = GetOrchestratorCommand("SendSceneEventToMaster");
            command.GetParameter("sceneEventData").ParamValue = pByteArray;
            OrchestrationSocketIoManager.EmitPacket(command);
        }

        public void SendSceneEventPacketToUser(string pUserID, byte[] pByteArray)
        {
            OrchestratorCommand command = GetOrchestratorCommand("SendSceneEventToUser");
            command.GetParameter("userId").ParamValue = pUserID;
            command.GetParameter("sceneEventData").ParamValue = pByteArray;
            OrchestrationSocketIoManager.EmitPacket(command);
        }

        public void SendSceneEventPacketToAllUsers(byte[] pByteArray)
        {
            OrchestratorCommand command = GetOrchestratorCommand("SendSceneEventToAllUsers");
            command.GetParameter("sceneEventData").ParamValue = pByteArray;
            OrchestrationSocketIoManager.EmitPacket(command);
        }

        public void DeclareDataStream(string pDataStreamType)
        {
            OrchestratorCommand command = GetOrchestratorCommand("DeclareDataStream");
            command.GetParameter("dataStreamKind").ParamValue = pDataStreamType;
            command.GetParameter("dataStreamDescription").ParamValue = "";
            OrchestrationSocketIoManager.EmitPacket(command);
        }

        public void RemoveDataStream(string pDataStreamType)
        {
            OrchestratorCommand command = GetOrchestratorCommand("RemoveDataStream");
            command.GetParameter("dataStreamKind").ParamValue = pDataStreamType;
            OrchestrationSocketIoManager.EmitPacket(command);
        }

        public void RemoveAllDataStreams()
        {
            OrchestratorCommand command = GetOrchestratorCommand("RemoveAllDataStreams");
            OrchestrationSocketIoManager.EmitPacket(command);
        }

        public void RegisterForDataStream(string pDataStreamUserId, string pDataStreamType)
        {
            OrchestratorCommand command = GetOrchestratorCommand("RegisterForDataStream");
            command.GetParameter("dataStreamUserId").ParamValue = pDataStreamUserId;
            command.GetParameter("dataStreamKind").ParamValue = pDataStreamType;
            OrchestrationSocketIoManager.EmitPacket(command);
        }

        public void UnregisterFromDataStream(string pDataStreamUserId, string pDataStreamKind)
        {
            OrchestratorCommand command = GetOrchestratorCommand("UnregisterFromDataStream");
            command.GetParameter("dataStreamUserId").ParamValue = pDataStreamUserId;
            command.GetParameter("dataStreamKind").ParamValue = pDataStreamKind;
            OrchestrationSocketIoManager.EmitPacket(command);
        }

        public void UnregisterFromAllDataStreams()
        {
            OrchestratorCommand command = GetOrchestratorCommand("UnregisterFromAllDataStreams");
            OrchestrationSocketIoManager.EmitPacket(command);
        }

        public void SendData(string pDataStreamType, byte[] pDataStreamBytes)
        {
            OrchestratorCommand command = GetOrchestratorCommand("SendData");
            command.GetParameter("dataStreamKind").ParamValue = pDataStreamType;
            command.GetParameter("dataStreamBytes").ParamValue = pDataStreamBytes;
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

            UserMessage messageReceived = new UserMessage(jsonResponse[1]["messageFrom"].ToString(), jsonResponse[1]["messageFromName"].ToString(), jsonResponse[1]["message"].ToString());
            
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

            if (myUserID != lUserID)
            {
                UserAudioPacket packetReceived = new UserAudioPacket(packet.Attachments[0], lUserID);
                OnAudioSent?.Invoke(packetReceived);
            }
        }

        // bit-stream packets from the orchestrator
        private void OnUserDataReceived(Socket socket, Packet packet, params object[] args)
        {
            JsonData jsonResponse = JsonMapper.ToObject(packet.Payload);
            string lUserID = jsonResponse[1].ToString();
            string lType = jsonResponse[2].ToString();
            string lDescription = jsonResponse[3].ToString();

            UserDataStreamPacket packetReceived = new UserDataStreamPacket(lUserID, lType, lDescription, packet.Attachments[0]);
            OnDataStreamReceived?.Invoke(packetReceived);
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
            string lUserID = jsonResponse[1]["eventData"][0].ToString();

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

        // events packets from master user through the orchestrator
        private void OnMasterEventReceived(Socket socket, Packet packet, params object[] args)
        {
            if (MessagesFromOrchestratorListener != null)
            {
                JsonData jsonResponse = JsonMapper.ToObject(packet.Payload);
                string lUserID = jsonResponse[1]["sceneEventFrom"].ToString();
                string lData = Encoding.ASCII.GetString(packet.Attachments[0]);
                UserEvent lUserEvent = new UserEvent(lUserID, lData);

                MessagesFromOrchestratorListener.OnMasterEventReceived(lUserEvent);
            }
        }

        // events packets from users through the orchestrator
        private void OnUserEventReceived(Socket socket, Packet packet, params object[] args)
        {
            if (MessagesFromOrchestratorListener != null)
            {
                JsonData jsonResponse = JsonMapper.ToObject(packet.Payload);
                string lUserID = jsonResponse[1]["sceneEventFrom"].ToString();
                string lData = Encoding.ASCII.GetString(packet.Attachments[0]);
                UserEvent lUserEvent = new UserEvent(lUserID, lData);

                MessagesFromOrchestratorListener.OnUserEventReceived(lUserEvent);
            }
        }

        #endregion

        #region grammar definition
        // declare te available commands, their parameters and the callbacks that should be used for the response of each command
        public void InitGrammar()
        {
            orchestratorCommands = new List<OrchestratorCommand>
            {              
                //login & logout
                new OrchestratorCommand("Login", new List<Parameter>
                {
                    new Parameter("userName", typeof(string)),
                    new Parameter("userPassword", typeof(string))
                },
                OnLoginResponse),
                new OrchestratorCommand("Logout", null, OnLogoutResponse),
                new OrchestratorCommand("GetOrchestratorVersion", null, OnGetOrchestratorVersionResponse),

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
                    new Parameter("canBeMaster", typeof(bool))
                },
                OnJoinSessionResponse),
                new OrchestratorCommand("LeaveSession", null, OnLeaveSessionResponse),

                //live stream
                new OrchestratorCommand("GetLivePresenterData", null, GetLivePresenterDataResponse),

                //scenarios
                new OrchestratorCommand("GetScenarios", null, OnGetScenariosResponse),
                new OrchestratorCommand("GetScenarioInstanceInfo", new List<Parameter>
                {
                    new Parameter("scenarioId", typeof(string))
                },
                OnGetScenarioInstanceInfoResponse),

                //users
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
                    new Parameter("userDataJson", typeof(string))
                },
                OnUpdateUserDataJsonResponse),
                new OrchestratorCommand("ClearUserData", null, OnClearUserDataResponse),
                new OrchestratorCommand("DeleteUser", new List<Parameter>
                {
                    new Parameter("userId", typeof(string))
                },
                OnDeleteUserResponse),

                //rooms
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

                //user events
                new OrchestratorCommand("SendSceneEventToMaster", new List<Parameter>
                {
                    new Parameter("sceneEventData", typeof(byte[]))
                }),
                new OrchestratorCommand("SendSceneEventToUser", new List<Parameter>
                {
                    new Parameter("userId", typeof(string)),
                    new Parameter("sceneEventData", typeof(byte[]))
                }),
                new OrchestratorCommand("SendSceneEventToAllUsers", new List<Parameter>
                {
                    new Parameter("sceneEventData", typeof(byte[]))
                }),

                //user bit-streams
                new OrchestratorCommand("DeclareDataStream", new List<Parameter>
                {
                    new Parameter("dataStreamKind", typeof(string)),
                    new Parameter("dataStreamDescription", typeof(string))
                }),
                new OrchestratorCommand("RemoveDataStream", new List<Parameter>
                {
                    new Parameter("dataStreamKind", typeof(string)),
                }),
                new OrchestratorCommand("RemoveAllDataStreams", null),
                new OrchestratorCommand("RegisterForDataStream", new List<Parameter>
                {
                    new Parameter("dataStreamUserId", typeof(string)),
                    new Parameter("dataStreamKind", typeof(string))
                }),
                new OrchestratorCommand("UnregisterFromDataStream", new List<Parameter>
                {
                    new Parameter("dataStreamUserId", typeof(string)),
                    new Parameter("dataStreamKind", typeof(string))
                }),
                new OrchestratorCommand("UnregisterFromAllDataStreams", null),
                new OrchestratorCommand("GetAvailableDataStreams", new List<Parameter>
                {
                    new Parameter("dataStreamUserId", typeof(string))
                },
                OnGetAvailableDataStreams),
                new OrchestratorCommand("GetRegisteredDataStreams", null, OnGetRegisteredDataStreams),
                new OrchestratorCommand("SendData", new List<Parameter>
                {
                    new Parameter("dataStreamKind", typeof(string)),
                    new Parameter("dataStreamBytes", typeof(byte[]))
                })
            };

            orchestratorMessages = new List<OrchestratorMessageReceiver>
            {              
                //messages
                new OrchestratorMessageReceiver("MessageSent", OnMessageSentFromOrchestrator),
                //audio packets
                new OrchestratorMessageReceiver("AudioSent", OnAudioSentFromOrchestrator),
                //session update events
                new OrchestratorMessageReceiver("SessionUpdated", OnSessionUpdated),
                //user events
                new OrchestratorMessageReceiver("SceneEventToMaster", OnMasterEventReceived),
                new OrchestratorMessageReceiver("SceneEventToUser", OnUserEventReceived),
                //user bit-stream
                new OrchestratorMessageReceiver("DataReceived", OnUserDataReceived)
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