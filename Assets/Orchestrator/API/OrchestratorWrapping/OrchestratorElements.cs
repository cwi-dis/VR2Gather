using LitJson;
using System.Collections.Generic;

namespace OrchestratorWrapping
{
    // Base class for the elements returned by the orchestrator
    public abstract class OrchestratorElement
    {
        // used to retrieve the ID
        public virtual string GetId()
        {
            return string.Empty;
        }

        //used to display the element for Gui
        public virtual string GetGuiRepresentation()
        {
            return string.Empty;
        }

        // Parse a JSonData to a C# object
        public static T ParseJsonData<T>(JsonData data)
        {
            return JsonMapper.ToObject<T>(data.ToJson());
        }
    }

    public class User: OrchestratorElement
    {
        public string userId = "";
        public string userName = "";
        public string userPassword = "";
        public bool userAdmin = false;
        public UserData userData;
        public SfuData sfuData;

        // empty constructor callled by the JsonData parser
        public User()
        {
        }

        // Parse a JSonData to a C# object
        public static User ParseJsonData(JsonData data)
        {
            return JsonMapper.ToObject<User>(data.ToJson());
        }

        public override string GetId()
        {
            return userId;
        }

        public override string GetGuiRepresentation()
        {
            return userName;
        }
    }

    public class UserData: OrchestratorElement
    {
        public string userMQexchangeName = "";
        public string userMQurl = "";

        public string userPCurl = "";
        public string userAudioUrl = "";

        public eUserRepresentationType userRepresentationType;
        public string webcamName = "";
        public string microphoneName = "";

        public enum eUserRepresentationType
        {
            __NONE__,
            __2D__,
            __AVATAR__,
            __TVM__,
            __PCC_CWI_,
            __PCC_CWIK4A_,
            __PCC_PROXY__,
            __PCC_SYNTH__,
            __PCC_CERTH__,
            __SPECTATOR__
        }

        // empty constructor callled by the JsonData parser
        public UserData() { }

        // Useless since UserData declaration shouldn't specify the whole data to be declared in a single step
        /*
        public UserData(string pMQname, string pMQurl, string pPCurl, string pAudioUrl)
        {
            userMQexchangeName = pMQname;
            userMQurl = pMQurl;

            userPCurl = pPCurl;
            userAudioUrl = pAudioUrl;
        }
        */
    }

    public class SfuData : OrchestratorElement
    {
        public string url_gen = "";
        public string url_audio = "";
        public string url_pcc = "";

        // empty constructor callled by the JsonData parser
        public SfuData() { }
    }

    public class LivePresenterData : OrchestratorElement
    {
        public string liveAddress = "";
        public string vodAddress = "";

        // empty constructor callled by the JsonData parser
        public LivePresenterData() { }
    }

    public class DataStream : OrchestratorElement
    {
        public string dataStreamUserId = "";
        public string dataStreamKind = "";
        public string dataStreamDescription = "";

        // empty constructor callled by the JsonData parser
        public DataStream() { }

        // Parse a JSonData to a C# object
        public static DataStream ParseJsonData(JsonData data)
        {
            return JsonMapper.ToObject<DataStream>(data.ToJson());
        }
    }

    public class NtpClock: OrchestratorElement
    {
        public string ntpDate;
        public System.Int64 ntpTimeMs;

        public NtpClock() {}
        public int Timestamp { get { return (int)(ntpTimeMs / 1000); } }
    }

    public class Scenario : OrchestratorElement
    {
        public string scenarioId;
        public string scenarioName;
        public string scenarioDescription;
        public List<Room> scenarioRooms = new List<Room>();
        public JsonData scenarioGltf;

        public static Scenario ParseJsonData(JsonData data)
        {
            Scenario scenario = new Scenario();
            scenario.scenarioId = data["scenarioId"].ToString();
            scenario.scenarioName = data["scenarioName"].ToString();
            scenario.scenarioDescription = data["scenarioDescription"].ToString();
            JsonData rooms = data["scenarioRooms"];
            scenario.scenarioRooms = Helper.ParseElementsList<Room>(rooms);
            scenario.scenarioGltf = data["scenarioGltf"];
            return scenario;
        }

        public override string GetId()
        {
            return scenarioId;
        }

        public override string GetGuiRepresentation()
        {
            return scenarioName + " (" + scenarioDescription + ")";
        }
    }

    public class ScenarioInstance : OrchestratorElement
    {
        public string scenarioRefId; //store reference on the source scenario
        public string sessionId;
        public string scenarioId;
        public string scenarioName;
        public string scenarioDescription;
        public List<OrchestratorElement> scenarioRooms = new List<OrchestratorElement>();

        public override string GetId()
        {
            return scenarioId;
        }

        public override string GetGuiRepresentation()
        {
            return scenarioName + " (" + scenarioDescription + ")";
        }
    }

    public class Room : OrchestratorElement
    {
        public string roomId;
        public string roomName;
        public string roomDescription;
        public int roomCapacity;

        public static Room ParseJsonData(JsonData data)
        {
            return JsonMapper.ToObject<Room>(data.ToJson());
        }

        public override string GetId()
        {
            return roomId;
        }

        public override string GetGuiRepresentation()
        {
            return roomName + " (" + roomDescription + ")";
        }
    }

    public class RoomInstance : Room
    {
        public string roomRefId;
        public string[] roomUsers;

        public static new RoomInstance ParseJsonData(JsonData data)
        {
            return JsonMapper.ToObject<RoomInstance>(data.ToJson());
        }

        public override string GetId()
        {
            return roomId;
        }

        public override string GetGuiRepresentation()
        {
            return roomName + " (" + roomDescription + ")";
        }
    }

    public class Session : OrchestratorElement
    {
        public string sessionId;
        public string sessionName;
        public string sessionDescription;
        public string sessionAdministrator;
        public string sessionMaster;
        public string scenarioId; // the scenario ID
        public string[] sessionUsers;

        public static Session ParseJsonData(JsonData data)
        {
            return JsonMapper.ToObject<Session>(data.ToJson());
        }

        public override string GetId()
        {
            return sessionId;
        }

        public override string GetGuiRepresentation()
        {
            return sessionName + " (" + sessionDescription + ")";
        }
    }
}
