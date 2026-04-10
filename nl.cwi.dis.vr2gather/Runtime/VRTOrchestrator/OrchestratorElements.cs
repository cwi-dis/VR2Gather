using VRT.Core;
using System.Collections.Generic;
using UnityEngine;

namespace VRT.Orchestrator.Elements
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

        public static T ParseJsonString<T>(string data)
        {
            return JsonUtility.FromJson<T>(data);
        }

        public string AsJsonString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    public class User: OrchestratorElement
    {
        public string userId = "";
        public string userName = "";
        public UserData userData;
        public SfuData sfuData;
        public int webRTCClientId = -1;

        public User()
        {
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
        /// <summary>
        /// The self-representation of this user.
        /// </summary>
        public UserRepresentationType userRepresentation = UserRepresentationType.SimpleAvatar;
        /// <summary>
        /// True if the user has voice (microphonne)
        /// </summary>
        public bool hasVoice = false; 
        /// <summary>
        /// If needed this contains the base URL on which this user streams their representation.
        /// (Whether it is needed or not depends on the transport protocol used in the session)
        /// </summary>
        public string userRepresentationTCPUrl = "";

        // empty constructor called by the JsonData parser
        public UserData() { }
    }

    public class SfuData : OrchestratorElement
    {
        public string url_gen = "";
        public string url_audio = "";
        public string url_pcc = "";

        public SfuData() {}
    }

    public class DataStream : OrchestratorElement
    {
        public string dataStreamUserId = "";
        public string dataStreamKind = "";
        public string dataStreamDescription = "";

        public DataStream() { }
    }

    public class NtpClock: OrchestratorElement
    {
        public string ntpDate;
        public long ntpTimeMs;

        public NtpClock() { }

        public double Timestamp { get { return (ntpTimeMs / 1000.0); } }
    }

    public class Scenario : OrchestratorElement
    {
        public string scenarioId;
        public string scenarioName;
        public string scenarioDescription;

        public Scenario() { }

        public override string GetId()
        {
            return scenarioId;
        }

        public override string GetGuiRepresentation()
        {
            return scenarioName + " (" + scenarioDescription + ")";
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
        public List<User> sessionUserDefinitions;

        public Session() { }

        public override string GetId()
        {
            return sessionId;
        }

        public override string GetGuiRepresentation()
        {
            return sessionName + " (" + sessionDescription + ")";
        }

        public User[] GetUsers()
        {
            return sessionUserDefinitions.ToArray();
        }

        public User GetUser(string userID)
        {
            foreach(var userDefinition in sessionUserDefinitions)
            {
                if (userDefinition.userId == userID)
                {
                    return userDefinition;
                }
            }
            return null;
        }

        public int GetUserCount()
        {
            return sessionUserDefinitions.Count;
        }
    }
}
