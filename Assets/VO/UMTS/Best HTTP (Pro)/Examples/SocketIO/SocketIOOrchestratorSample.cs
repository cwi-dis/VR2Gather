#if !BESTHTTP_DISABLE_SOCKETIO

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using BestHTTP.SocketIO;
using LitJson;

namespace BestHTTP.Examples
{
    public class OrchestratorResponse
    {
        public int error { get; set; }
        public string message { get; set; }
        //public object body { get; set; }
        public BodyResponse body { get; set; }
    }

    public class BodyResponse
    {
        public JsonData value { get; set; }
    }
 

    public class SocketIOOrchestratorSample : MonoBehaviour
    {
        #region Fields

        private SocketManager Manager;

        // orchestrator connection informations
        private string orchestratorUrl = "http://127.0.0.1:8080/socket.io/";
        private string userName = "admin";
        private string password = "password";

        // exchanged messages with orchestrator
        private string orchestratorMessage = string.Empty;
        private string orchestratorResponse = string.Empty;
        //private List<string> messageslog = new List<string>();
        Vector2 scrollMessagesPos;
        private string messagesText = string.Empty;
        private UserState userState = UserState.NotLogged;
        private string userStateRepresentation = string.Empty;
        private string loginId = string.Empty;
        private Boolean ConnectButton = true;
        private Boolean DisconnectButton = false;
        private Boolean LoginButton = false;
        private Boolean LogoutButton = false;
        Dropdown commandsDropdown;
        List<string> m_DropOptions = new List<string> { "Option 1", "Option 2" };

        Vector2 commandsSelectionPos;

        // Les états possibles
        enum UserState
        {
            Unconnected,
            NotLogged,
            WaitingLoginResponse,
            WaitingLogoutResponse,
            Logged,
            SessionJoined,
            RoomJoined
        }

        private static GUIStyle leftAlignedLabel;
        private static GUIStyle titleStyle;
        private static GUIStyle leftRightLabelMargins;
        //private static GUIStyle leftRightButtonMargins;
        #endregion

        #region Unity Events

        void Start()
        {
            commandsDropdown = GetComponent<Dropdown>();
            commandsDropdown.ClearOptions();
            commandsDropdown.AddOptions(m_DropOptions);
            SetState(UserState.Unconnected);
            //userStateRepresentation = userState.ToString("F");
        }

        void OnDestroy()
        {
            // Leaving this sample, close the socket
            if (Manager != null)
            {
                Manager.Close();
            }
        }

        void Update()
        {

            //// Go back to the demo selector
            //if (Input.GetKeyDown(KeyCode.Escape))
            //    SampleSelector.SelectedSample.DestroyUnityObject();

            //// Stop typing if some time passed without typing
            //if (typing)
            //{
            //    var typingTimer = DateTime.UtcNow;
            //    var timeDiff = typingTimer - lastTypingTime;
            //    if (timeDiff >= TYPING_TIMER_LENGTH)
            //    {
            //        Manager.Socket.Emit("stop typing");
            //        typing = false;
            //    }
            //}
        }

        void OnGUI()
        {
            if (leftAlignedLabel == null)
            {
                leftAlignedLabel = new GUIStyle(GUI.skin.textField);
                leftAlignedLabel.alignment = TextAnchor.LowerLeft;
                leftAlignedLabel.fixedWidth = 200;
                leftAlignedLabel.margin = new RectOffset(10, 10, 0, 0);
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.alignment = TextAnchor.MiddleCenter;
                titleStyle.margin = new RectOffset(10, 10, 10, 10);
                titleStyle.fontStyle = FontStyle.Bold;
            }

            if (leftRightLabelMargins == null)
            {
                leftRightLabelMargins = new GUIStyle(GUI.skin.label);
                leftRightLabelMargins.padding = new RectOffset(10, 10, 0, 0);
            }

            //if (leftRightButtonMargins == null)
            //{
            //    leftRightButtonMargins = new GUIStyle(GUI.skin.button);
            //    leftRightButtonMargins.margin = new RectOffset(10, 10, 0, 0);
            //}

            // Le panel de connexion
            GUIHelper.DrawArea(new Rect(5, 5, Screen.width / 3, Screen.height / 3), false, () =>
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Login configuration", titleStyle);

                GUILayout.BeginHorizontal();
                GUILayout.Label("orchestratorUrl", leftRightLabelMargins);
                GUILayout.FlexibleSpace();
                orchestratorUrl = GUILayout.TextField(orchestratorUrl, leftAlignedLabel);
                //GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                //GUILayout.FlexibleSpace();
                GUILayout.Space(10);
                GUI.enabled = ConnectButton;
                if (GUILayout.Button("Connect"))
                {
                    //Connect to socket();
                    ConnectToOrchestrator(orchestratorUrl);
                }
                //GUILayout.FlexibleSpace();
                GUILayout.Space(10);
                GUI.enabled = DisconnectButton;
                if (GUILayout.Button("Disconnect"))
                {
                    //Login();
                    DisconnectFromOrchestrator();
                }
                GUI.enabled = true;
                GUILayout.Space(10);
                //GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.Label("userName", leftRightLabelMargins);
                GUILayout.FlexibleSpace();
                userName = GUILayout.TextField(userName, leftAlignedLabel);
                //GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("password", leftRightLabelMargins);
                GUILayout.FlexibleSpace();
                password = GUILayout.TextField(password, leftAlignedLabel);
                //GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();

                GUILayout.BeginHorizontal();
                //GUILayout.FlexibleSpace();
                GUILayout.Space(10);
                GUI.enabled = LoginButton;
                if (GUILayout.Button("Login"))
                {
                    //Login();
                    LoginOnOrchestrator(userName, password);
                }
                GUILayout.Space(10);
                //GUILayout.FlexibleSpace();
                GUI.enabled = LogoutButton;
                if (GUILayout.Button("Logout"))
                {
                    //Login();
                    LogoutOrchestrator();
                }
                GUI.enabled = true;
                GUILayout.Space(10);
                //GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            });

            // le panel representant les etats
            GUIHelper.DrawArea(new Rect(Screen.width / 3 + 10, 5, Screen.width / 3, Screen.height / 3), false, () =>
            {
                GUILayout.BeginVertical();
                GUILayout.Label("User state", titleStyle);

                GUILayout.BeginHorizontal();
                GUILayout.Label("user state", leftRightLabelMargins);
                GUILayout.FlexibleSpace();
                GUILayout.Label(userStateRepresentation);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("user Id", leftRightLabelMargins);
                GUILayout.FlexibleSpace();
                GUILayout.Label(loginId);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            });

            // le panel pour les commandes possibles
            GUIHelper.DrawArea(new Rect(5, (Screen.height / 3) + 10, Screen.width - 10, (Screen.height*7/15)), false, () =>
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Available commands", titleStyle);
                GUILayout.BeginHorizontal();

                //GUI.Box(new Rect(5, (Screen.height / 3) + 10, Screen.width - 10, (Screen.height * 7 / 15)), string.Empty);
                GUILayout.BeginArea(new Rect(5, 25, (Screen.width - 10)/3-5, (Screen.height * 7 / 15)-30));
                GUILayout.BeginVertical();

                //GUILayout.Label("Sessions");
                //string[] selStrings = { "radio1", "radio2", "radio3" };

                commandsSelectionPos = GUILayout.BeginScrollView(commandsSelectionPos);
                GUILayout.Label("Sessions");
                if (GUILayout.Button("Add Session"))
                {
                    AddSession();
                }
                if (GUILayout.Button("Get sessions"))
                {
                    AddSession();
                }
                if (GUILayout.Button("Get session info"))
                {
                    AddSession();
                }
                if (GUILayout.Button("Delete session"))
                {
                    AddSession();
                }
                GUILayout.Label("Scenarios");
                if (GUILayout.Button("Get Scenarios"))
                {
                    AddSession();
                }
                if (GUILayout.Button("Get scenario info"))
                {
                    AddSession();
                }
                GUILayout.Label("Users");
                if (GUILayout.Button("Get users"))
                {
                    AddSession();
                }
                if (GUILayout.Button("Get user info"))
                {
                    AddSession();
                }
                if (GUILayout.Button("Add user"))
                {
                    AddSession();
                }
                if (GUILayout.Button("Delete user"))
                {
                    AddSession();
                }
                GUILayout.Label("Sessions joining");
                if (GUILayout.Button("JoinSession"))
                {
                    AddSession();
                }
                if (GUILayout.Button("LeaveSession"))
                {
                    AddSession();
                }
                GUILayout.Label("Rooms");
                if (GUILayout.Button("GetRooms"))
                {
                    AddSession();
                }
                if (GUILayout.Button("GetRoomInfo"))
                {
                    AddSession();
                }
                if (GUILayout.Button("JoinRoom"))
                {
                    AddSession();
                }
                if (GUILayout.Button("LeaveRoom"))
                {
                    AddSession();
                }
                GUILayout.Label("Sessions update");
                if (GUILayout.Button("UpdateSession"))
                {
                    AddSession();
                }
                if (GUILayout.Button("SessionClosed"))
                {
                    AddSession();
                }
                GUILayout.Label("Messages");
                if (GUILayout.Button("SendMessage"))
                {
                    AddSession();
                }
                if (GUILayout.Button("SendMessageToAll"))
                {
                    AddSession();
                }
                if (GUILayout.Button("MessageSent"))
                {
                    AddSession();
                }
                GUILayout.EndScrollView();

                GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
                GUILayout.EndArea();

                GUILayout.BeginArea(new Rect((Screen.width - 10) / 3 + 5, 25, (Screen.width - 10) *2 / 3 - 5, (Screen.height * 7 / 15) - 30));
                GUILayout.Label("Selected command description & Params");
                GUILayout.EndArea();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            });

            // Le panel des messages
            GUIHelper.DrawArea(new Rect(5, (4 * Screen.height / 5) + 15, Screen.width - 10, Screen.height / 5 -20), false, () =>
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Messages", titleStyle);

                scrollMessagesPos = GUILayout.BeginScrollView(scrollMessagesPos);
                messagesText = GUILayout.TextArea(messagesText);
                GUILayout.EndScrollView();
                //GUILayout.Label("Out:");
                //GUILayout.Label(orchestratorMessage);

                //GUILayout.FlexibleSpace();

                //GUILayout.Label("Response:");
                //GUILayout.Label(orchestratorResponse);

                //GUILayout.FlexibleSpace();
                GUILayout.EndVertical();
            });
        }

        #endregion

        #region Custom SocketIO Events
        void AddSession()
        {
        }

        void SetState(UserState newState)
        {
            userState = newState;
            userStateRepresentation = userState.ToString("F");
        }

        void ConnectToOrchestrator(string orchestratorUrl)
        {
            // Change an option to show how it should be done
            SocketOptions options = new SocketOptions();
            options.AutoConnect = false;
            options.ConnectWith = BestHTTP.SocketIO.Transports.TransportTypes.WebSocket;

            // Create the Socket.IO manager
            Manager = new SocketManager(new Uri(orchestratorUrl), options);
            Manager.Encoder = new BestHTTP.SocketIO.JsonEncoders.LitJsonEncoder(); //JSON
            Manager.Socket.AutoDecodePayload = false;
            //Debug.LogError("autodecode: " + Manager.Socket.AutoDecodePayload);

            // We set SocketOptions' AutoConnect to false, so we have to call it manually.
            Manager.Open();
            SetState(UserState.NotLogged);
            ConnectButton = false;
            DisconnectButton = true;
            LoginButton = true;
            LogoutButton = false;
        }

        void DisconnectFromOrchestrator()
        {
            Manager.Close();
            SetState(UserState.Unconnected);
            ConnectButton = true;
            DisconnectButton = false;
            LoginButton = false;
            LogoutButton = false;
        }

        void LoginOnOrchestrator(string userName, string password)
        {
            var msgParams = new { userName = userName, userPassword = password };
            orchestratorMessage = "Login " + msgParams;
            messagesText += "\n <<< " + orchestratorMessage;
            SetState(UserState.WaitingLoginResponse);
            Manager.Socket.Emit("Login", OnAckCallback, msgParams);
        }

        void LogoutOrchestrator()
        {
            var msgParams = new {};
            orchestratorMessage = "Logout " + msgParams;
            messagesText += "\n <<< " + orchestratorMessage;
            SetState(UserState.WaitingLogoutResponse);
            Manager.Socket.Emit("Logout", OnAckCallback, msgParams);
            //Manager.Socket.Emit("Logout"); // Pas de reponse au logout
            //SetState(UserState.NotLogged);
            //LoginButton = true;
            //LogoutButton = false;
            //loginId = "";
        }

        public OrchestratorResponse JsonToOrchestratorResponse(string jsonMessage)
        {
            JsonData jsonResponse = JsonMapper.ToObject(jsonMessage);
            OrchestratorResponse response = new OrchestratorResponse();
            response.error = (int)jsonResponse[0]["error"];
            response.message = jsonResponse[0]["message"].ToString();
            response.body = new BodyResponse();
            response.body.value = jsonResponse[0]["body"];
            return response;
        }

        void OnAckCallback(Socket socket, Packet originalPacket, params object[] args)
        {
            //Debug.LogError("OnAckCallback!, paylload= " + originalPacket.Payload);
            orchestratorResponse = originalPacket.Payload;
            messagesText += "\n >>> " + orchestratorResponse;
            JsonToOrchestratorResponse(originalPacket.Payload);
            OrchestratorResponse response = JsonToOrchestratorResponse(originalPacket.Payload);

            switch (userState) {
                case UserState.WaitingLoginResponse:
                    if (response.error == 0)
                    {
                        // reponse Ok
                        loginId = response.body.value["userId"].ToString();
                        SetState(UserState.Logged);
                        LoginButton = false;
                        LogoutButton = true;
                    } else
                    {
                        // reponse KO
                    }
                    break;
                case UserState.WaitingLogoutResponse:
                    if (response.error == 0)
                    {
                        SetState(UserState.NotLogged);
                        LoginButton = true;
                        LogoutButton = false;
                        loginId = "";
                    }
                    else
                    {
                        // reponse KO
                    }
                    break;
                default:
                    break;
            }
        }

        void SetUserName()
        {
            if (string.IsNullOrEmpty(userName))
                return;

            //State = ChatStates.Chat;

            Manager.Socket.Emit("add user", userName);
        }

        #endregion
    }
}

#endif