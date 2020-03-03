using LitJson;
using OrchestratorWrapping;
using System.Collections.Generic;
using UnityEngine;

public class NetController : MonoBehaviour {
    public static NetController Instance { get; private set; }
    public string url = "https://vrt-orch-sandbox.viaccess-orca.com/socket.io/";

    public string userName = "Fernando@THEMO";
    public string userPassword = "THEMO2020";

    public uint uuid { get; set; }

    public static float LocalClock;
    public static float OffsetClock;
    public static float NetClock;

    Scenario scenarioToConnect;

    BinaryConnection connection;
    void Awake() {
        Instance = this;
    }

    void Start() {
        connection = new BinaryConnection();
        connection.RegisterMessage<NetTime>();

        Connect();
    }

    public bool isConnected { get { return connection.isConnected; } }

    public void Connect() {
        OrchestratorController.Instance.OnOrchestratorRequestEvent += OnOrchestratorRequest;
        OrchestratorController.Instance.OnOrchestratorResponseEvent += OnOrchestratorResponse;
        OrchestratorController.Instance.OnUserJoinSessionEvent += OnUserJoinedSessionHandler;
        OrchestratorController.Instance.OnUserLeaveSessionEvent += OnUserLeftSessionHandler;


        OrchestratorController.Instance.OnConnectionEvent += OnConnect;
        OrchestratorController.Instance.OnLoginEvent += OnLogin;
        OrchestratorController.Instance.OnLogoutEvent += OnLogout;
        OrchestratorController.Instance.OnGetSessionsEvent += OnGetSessionsHandler;
        OrchestratorController.Instance.OnAddSessionEvent += OnAddSessionHandler;
        OrchestratorController.Instance.OnGetScenarioEvent += OnGetScenarioHandler;
        OrchestratorController.Instance.OnJoinSessionEvent += OnJoinSessionHandler;
        OrchestratorController.Instance.OnGetUserInfoEvent += OnGetUserInfoHandler;
        OrchestratorController.Instance.OnGetUsersEvent += OnGetUsersHandler;

        OrchestratorController.Instance.OnGetScenariosEvent += OnGetScenariosHandler;
        OrchestratorController.Instance.SocketConnect(url);
        // connection?.Connect("");
    }

    public void OnOrchestratorRequest(string pRequest) {
//        Debug.Log($"OnOrchestratorRequest {pRequest}");
    }

    // Display the received message in the logs
    public void OnOrchestratorResponse(string pResponse) {
//        Debug.Log($"OnOrchestratorResponse {pResponse}");
    }


    private void OnConnect(bool pConnected) {
        Debug.Log($"OnConnect ({pConnected})");
        OrchestratorController.Instance.Login(userName, userPassword);
    }

    private void OnLogin(bool userLoggedSucessfully) {
        Debug.Log($"OnLogin ({userLoggedSucessfully})");
        OrchestratorController.Instance.GetUsers();
    }

    private void OnGetUsersHandler(User[] users) {
        if (users != null && users.Length > 0) {
            for (int i = 0; i < users.Length; ++i) {
                Debug.Log($"OnGetUsersHandler ({users[i].userName})");
                if (users[i].userName == userName) {
                    OrchestratorController.Instance.GetUserInfo(users[i].userId);
                }
            }
        }
    }

    private void OnGetUserInfoHandler(User user) {
        if (user != null) {
            Debug.Log($"OnGetUserInfoHandler ({user.userName})");
            OrchestratorController.Instance.SelfUser = user;
            OrchestratorController.Instance.GetScenarios();
        }
    }
    private void OnGetScenariosHandler(Scenario[] scenarios) {
        if (scenarios.Length > 0) {
            for( int i=0;i< scenarios.Length;++i)
            Debug.Log($"OnGetScenariosHandler ({scenarios[i].scenarioName})");
            scenarioToConnect = scenarios[0];
            OrchestratorController.Instance.GetSessions();
        }
    }

    private void OnLogout(bool userLogoutSucessfully) {
    }

    private void OnGetSessionsHandler(Session[] sessions) {
        if (sessions != null && sessions.Length > 0) {
            Debug.Log($"OnGetSessionsHandler {sessions.Length}");

        } else {
            Debug.Log($"OnGetScenariosHandler (AddSession({scenarioToConnect.scenarioId}, NetController, Session test) )");
            OrchestratorController.Instance.AddSession(scenarioToConnect.scenarioId, "NetController", "Session test.");
        }
    }

    private void OnAddSessionHandler(Session session) 
    {
        // Here you should store the retrieved session.
        /*
        if (session != null) {
            Debug.Log($"OnAddSessionHandler {session.sessionId}");
//            SubscribeToBinaryChannel();
            OrchestratorController.Instance.JoinSession(session.sessionId);
        } else
            Debug.Log($"OnAddSessionHandler null");
        */
    }

    private void OnGetScenarioHandler(ScenarioInstance scenario)
    {
        // Here you can join the stored session as no commands calls are proceeded.
        /*
        if (session != null)
        {
            Debug.Log($"OnAddSessionHandler {session.sessionId}");
            //            SubscribeToBinaryChannel();
            OrchestratorController.Instance.JoinSession(session.sessionId);
        }
        else
            Debug.Log($"OnAddSessionHandler null");
         */
    }

    private void OnJoinSessionHandler(Session session) {
        if (session != null) Debug.Log($"OnJoinSessionHandler {session.sessionId}");
        else Debug.Log($"OnJoinSessionHandler null");
    }

    private void OnUserJoinedSessionHandler(string userID) {
        Debug.Log($"OnUserJoinedSessionHandler {userID}");
    }

    private void SubscribeToBinaryChannel() {

        OrchestratorWrapper.instance.RegisterForDataStream(OrchestratorController.Instance.SelfUser.userId, "BINARYDATA");
        OrchestratorWrapper.instance.OnDataStreamReceived += OnDataPacketReceived;

        byte[] data = new byte[] { 1, 2, 3, 4, 5 };
        OrchestratorWrapper.instance.SendData("BINARYDATA", data);
    }


    private void OnDataPacketReceived(UserDataStreamPacket pPacket) {
        if (pPacket.dataStreamUserID == OrchestratorController.Instance.SelfUser.userId) {
            Debug.Log($"!!! DATA {pPacket.dataStreamPacket.Length}");
        }
    }


    

    private void OnUserLeftSessionHandler(string userID) {
        Debug.Log($"OnUserLeftSessionHandler {userID}");
    }

    public void Disconnect() {
        connection?.Close();
    }

    void Update() {
        LocalClock = Time.realtimeSinceStartup;
        NetClock = LocalClock + OffsetClock;
        connection?.Update();
    }

    private void OnDestroy() {
        connection?.Close();
    }

    public bool Send(MessageBase message) {
        if (connection.isConnected) {
            connection?.Send(message);
            return true;
        }
        return false;
    }

    public T GetMessage<T>() where T: MessageBase {
        return connection?.GetMessage<T>();
    }
}
