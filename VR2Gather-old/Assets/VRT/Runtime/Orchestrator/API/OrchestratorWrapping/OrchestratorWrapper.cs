//  © - 2020 – viaccess orca 
//  
//  Copyright
//  This code is strictly confidential and the receiver is obliged to use it 
//  exclusively for his or her own purposes. No part of Viaccess-Orca code may
//  be reproduced or transmitted in any form or by any means, electronic or 
//  mechanical, including photocopying, recording, or by any information 
//  storage and retrieval system, without permission in writing from 
//  Viaccess S.A. The information in this code is subject to change without 
//  notice. Viaccess S.A. does not warrant that this code is error-free. If 
//  you find any problems with this code or wish to make comments, please 
//  report them to Viaccess-Orca.
//  
//  Trademarks
//  Viaccess-Orca is a registered trademark of Viaccess S.A in France and/or
//  other countries. All other product and company names mentioned herein are
//  the trademarks of their respective owners. Viaccess S.A may hold patents,
//  patent applications, trademarks, copyrights or other intellectual property
//  rights over the code hereafter. Unless expressly specified otherwise in a 
//  written license agreement, the delivery of this code does not imply the 
//  concession of any license over these patents, trademarks, copyrights or 
//  other intellectual property.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Best.SocketIO;
using Best.HTTP.JSON.LitJson;
using System.Text;
using VRT.Orchestrator.WSManagement;

namespace VRT.Orchestrator.Wrapping
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


    // class that encapsulates the connection with the orchestrator, emitting and receiving the events
    // and converting and parsing the camands and the responses
    public class OrchestratorWrapper : IOrchestratorConnectionListener, IMessagesListener
    {
        public static OrchestratorWrapper instance;

        // manager for the socketIO connection to the orchestrator 
        private OrchestratorWSManager OrchestrationSocketIoManager;

        // Listener for the responses of the orchestrator
        private IOrchestratorResponsesListener ResponsesListener;

        // Listener for the messages of the orchestrator
        private IOrchestratorMessagesListener MessagesListener;

        // Listener for the messages emitted spontaneously by the orchestrator
        private IUserMessagesListener UserMessagesListener;

        // Listeners for the user events emitted when a session is updated by the orchestrator
        private List<IUserSessionEventsListener> UserSessionEventslisteners = new List<IUserSessionEventsListener>();

        // List of available commands (grammar description)
        public List<OrchestratorCommand> orchestratorCommands { get; private set; }

        // List of messages that can be received from the orchestrator
        public List<OrchestratorMessageReceiver> orchestratorMessages { get; private set; }

        public OrchestratorWrapper(string orchestratorSocketUrl, IOrchestratorResponsesListener responsesListener, IOrchestratorMessagesListener messagesListener, IUserMessagesListener userMessagesListener, IUserSessionEventsListener userSessionEventsListener)
        {
            if(instance is null)
            {
                instance = this;
            }

            OrchestrationSocketIoManager = new OrchestratorWSManager(orchestratorSocketUrl, this, this);

            ResponsesListener = responsesListener;
            MessagesListener = messagesListener;
            UserMessagesListener = userMessagesListener;

            UserSessionEventslisteners = new List<IUserSessionEventsListener>();
            UserSessionEventslisteners.Add(userSessionEventsListener);

            InitGrammar();
        }
        public Action<UserDataStreamPacket> OnDataStreamReceived;

        private string myUserID = "";

        #region messages listening interface implementation
        public void OnOrchestratorResponse(int commandID, int status, string response)
        {
            if (MessagesListener != null) MessagesListener.OnOrchestratorResponse(commandID, status, response);
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
                Debug.LogWarning("OrchestratorWrapper: Connect: already connected, Disconnect() and reconnect");
                OrchestrationSocketIoManager.SocketDisconnect();
            }
            OrchestrationSocketIoManager.SocketConnect(orchestratorMessages);
        }

        public void OnSocketConnect()
        {
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnSocketConnect: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnConnect();
        }

        public void OnSocketConnecting()
        {
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnSocketConnecting: no ResponsesListener");
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
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnSocketDisconnect: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnDisconnect();
        }

        public void OnSocketError(ResponseStatus status)
        {
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnSocketError: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnError(status);
        }

        public void GetOrchestratorVersion()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetOrchestratorVersion");
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        public void OnGetOrchestratorVersionResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            string version = response.body["orchestratorVersion"].ToString();
            ResponsesListener?.OnGetOrchestratorVersionResponse(new ResponseStatus(response.error, response.message), version);
        }

        public void Login(string userName, string userPassword)
        {
            OrchestratorCommand command = GetOrchestratorCommand("Login");
            command.GetParameter("userName").ParamValue = userName;
            command.GetParameter("userPassword").ParamValue = userPassword;
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnLoginResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            try {
                myUserID = response.body["userId"].ToString();
            }
            catch {
                myUserID = "";
            }
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnLoginResponse: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnLoginResponse(new ResponseStatus(response.error, response.message), myUserID);
        }

        public void Logout()
        {
            OrchestratorCommand command = GetOrchestratorCommand("Logout");
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnLogoutResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            myUserID = "";
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnLogoutResponse: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnLogoutResponse(new ResponseStatus(response.error, response.message));
        }

        public void GetNTPTime()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetNTPTime");
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetNTPTimeResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            NtpClock ntpTime = NtpClock.ParseJsonData<NtpClock>(response.body);
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnSocketConnect: no ResponsesListener");
            if (ResponsesListener != null)ResponsesListener.OnGetNTPTimeResponse(status, ntpTime);
        }


        internal void _AddScenario(Scenario scOrch)
        {
            OrchestratorCommand command = GetOrchestratorCommand("AddScenario");
            command.GetParameter("scenarioId").ParamValue = scOrch.scenarioId;
            command.GetParameter("scenarioName").ParamValue = scOrch.scenarioName;
            command.GetParameter("scenarioDescription").ParamValue = scOrch.scenarioDescription;
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void _OnAddScenarioResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            // Just ignore the response. We'll get another error later when we create a session.
        }

        public void AddSession(string scenarioId, Scenario scenario, string sessionName, string sessionDescription, string sessionProtocol)
        {
            
            OrchestratorCommand command = GetOrchestratorCommand("AddSession");
            command.GetParameter("scenarioId").ParamValue = scenarioId;
            if (scenario != null)
            {
                // This is gross. Have to go via string to get a JsonData.
                string jsonString = JsonUtility.ToJson(scenario);
                JsonData json = JsonMapper.ToObject(jsonString);
                command.GetParameter("scenarioDefinition").ParamValue = json;
            }
            command.GetParameter("sessionName").ParamValue = sessionName;
            command.GetParameter("sessionDescription").ParamValue = sessionDescription;
            command.GetParameter("sessionProtocol").ParamValue = sessionProtocol;
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnAddSessionResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            Session session = Session.ParseJsonData<Session>(response.body);
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnAddSessionResponse: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnAddSessionResponse(status, session);
        }

        public void GetSessions()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetSessions");
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetSessionsResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            List<Session> list;

            if (response.body.Keys.Count != 0) {
                list = Helper.ParseElementsList<Session>(response.body);
            } else {
                list = new List<Session>();
            }

            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnGetSessionsResponse: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnGetSessionsResponse(status, list);
        }

        public void GetSessionInfo()
        {
            OrchestratorCommand command = GetOrchestratorCommand("GetSessionInfo");
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnGetSessionInfoResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            Session session = Session.ParseJsonData<Session>(response.body);
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnGetSessionsInfoResponse: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnGetSessionInfoResponse(status, session);
        }

        public void DeleteSession(string sessionId)
        {
            OrchestratorCommand command = GetOrchestratorCommand("DeleteSession");
            command.GetParameter("sessionId").ParamValue = sessionId;
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnDeleteSessionResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnDeleteSessionResponse: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnDeleteSessionResponse(status);
        }

        public void JoinSession(string sessionId)
        {
            OrchestratorCommand command = GetOrchestratorCommand("JoinSession");
            command.GetParameter("sessionId").ParamValue = sessionId;
            // By default canBeMaster is set to false, it needs to be overrided to be sure that a master is affected.
            command.GetParameter("canBeMaster").ParamValue = true;
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnJoinSessionResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            Session session = Session.ParseJsonData<Session>(response.body);
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnJoinSessionResponse: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnJoinSessionResponse(status, session);
        }

        public void LeaveSession()
        {
            OrchestratorCommand command = GetOrchestratorCommand("LeaveSession");
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnLeaveSessionResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnLeaveSessionResponse: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnLeaveSessionResponse(status);
        }
        public void UpdateUserData(string userDataKey, string userDataValue)
        {
            OrchestratorCommand command = GetOrchestratorCommand("UpdateUserData");
            command.GetParameter("userDataKey").ParamValue = userDataKey;
            command.GetParameter("userDataValue").ParamValue = userDataValue;
            OrchestrationSocketIoManager.EmitCommand(command);
        }
        public void UpdateUserDataJson(UserData userData)
        {
            JsonData json = JsonUtility.ToJson(userData);
            OrchestratorCommand command = GetOrchestratorCommand("UpdateUserDataJson");
            command.GetParameter("userDataJson").ParamValue = json;
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnUpdateUserDataJsonResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnUpdateUserDataJsonResponse: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnUpdateUserDataJsonResponse(status);
        }

        public void SendMessage(string message, string userId)
        {
            OrchestratorCommand command = GetOrchestratorCommand("SendMessage");
            command.GetParameter("message").ParamValue = message;
            command.GetParameter("userId").ParamValue = userId;
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnSendMessageResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnSendMessageResponse: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnSendMessageResponse(status);
        }

        public void SendMessageToAll(string message)
        {
            OrchestratorCommand command = GetOrchestratorCommand("SendMessageToAll");
            command.GetParameter("message").ParamValue = message;
            OrchestrationSocketIoManager.EmitCommand(command);
        }

        private void OnSendMessageToAllResponse(OrchestratorCommand command, OrchestratorResponse response)
        {
            ResponseStatus status = new ResponseStatus(response.error, response.message);
            if (ResponsesListener == null) Debug.LogWarning($"OrchestratorWrapper: OnSendMessageToAllResponse: no ResponsesListener");
            if (ResponsesListener != null) ResponsesListener.OnSendMessageToAllResponse(status);
        }
#endregion

#region commands - no Acks

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
        private void OnMessageSentFromOrchestrator(Socket socket)
        {
            var packet = socket.CurrentPacket;
            MessagesListener?.OnOrchestratorResponse(-1, 0, packet.Payload);

            JsonData jsonResponse = JsonMapper.ToObject(packet.Payload);
            UserMessage messageReceived = new UserMessage(jsonResponse[1]["messageFrom"].ToString(), jsonResponse[1]["messageFromName"].ToString(), jsonResponse[1]["message"].ToString());

            UserMessagesListener?.OnUserMessageReceived(messageReceived);
        }

        // bit-stream packets from the orchestrator
        private void OnUserDataReceived(Socket socket)
        {
            var packet = socket.CurrentPacket;
            JsonData jsonResponse = JsonMapper.ToObject(packet.Payload);
            string lUserID = jsonResponse[1].ToString();
            string lType = jsonResponse[2].ToString();
            string lDescription = jsonResponse[3].ToString();

            var attachment = packet.Attachements[0];
            byte[] buffer = new byte[attachment.Count];
            attachment.CopyTo(buffer);

            UserDataStreamPacket packetReceived = new UserDataStreamPacket(lUserID, lType, lDescription, buffer);
            OnDataStreamReceived?.Invoke(packetReceived);
        }

        // sessions update events from the orchestrator
        private void OnSessionUpdated(Socket socket)
        {
            var packet = socket.CurrentPacket;
            MessagesListener?.OnOrchestratorResponse(-1, 0, packet.Payload);

            Debug.Log("SessionUpdated:" + packet.ToString());
            JsonData jsonResponse = JsonMapper.ToObject(packet.Payload);

            string lEventID = jsonResponse[1]["eventId"].ToString();
            string lUserID = jsonResponse[1]["eventData"]["userId"].ToString();
            
            if (lUserID == myUserID)
            {
                //I just joined a session, so I need to get all connected users IDs to get their audio, provided by the OnGetSessionInfoResponse callback.
                return;
            }

            switch (lEventID)
            {
                case "USER_JOINED_SESSION":
                    User lUser = User.ParseJsonData(jsonResponse[1]["eventData"]["userData"]);

                    foreach (IUserSessionEventsListener e in UserSessionEventslisteners)
                    {
                        e?.OnUserJoinedSession(lUserID, lUser);
                    }

                    break;
                case "USER_LEFT_SESSION":

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
        private void OnMasterEventReceived(Socket socket)
        {
            if (UserMessagesListener != null)
            {
                var packet = socket.CurrentPacket;
                JsonData jsonResponse = JsonMapper.ToObject(packet.Payload);

                string lUserID = jsonResponse[1]["sceneEventFrom"].ToString();
                var attachment = packet.Attachements[0];
                string lData = Encoding.ASCII.GetString(attachment, 0, attachment.Count);

                UserEvent lUserEvent = new UserEvent(lUserID, lData);
                UserMessagesListener.OnMasterEventReceived(lUserEvent);
            }
        }

        // events packets from users through the orchestrator
        private void OnUserEventReceived(Socket socket)
        {
            if (UserMessagesListener != null)
            {
                var packet = socket.CurrentPacket;
                JsonData jsonResponse = JsonMapper.ToObject(packet.Payload);

                string lUserID = jsonResponse[1]["sceneEventFrom"].ToString();
                var attachment = packet.Attachements[0];
                string lData = Encoding.ASCII.GetString(attachment, 0, attachment.Count);

                UserEvent lUserEvent = new UserEvent(lUserID, lData);
                UserMessagesListener.OnUserEventReceived(lUserEvent);
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
                    new Parameter("sessionDescription", typeof(string)),
                    new Parameter("sessionProtocol", typeof(string)),
                    new Parameter("scenarioDefinition", typeof(string))
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

                new OrchestratorCommand("UpdateUserDataJson", new List<Parameter>
                {
                    new Parameter("userDataJson", typeof(string))
                },
                OnUpdateUserDataJsonResponse),
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
                    return new OrchestratorCommand(orchestratorCommands[i]);
                }
            }
            return null;
        }

#endregion
    }
}