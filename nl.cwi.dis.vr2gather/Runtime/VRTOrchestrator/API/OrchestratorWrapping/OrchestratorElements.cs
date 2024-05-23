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

using Best.HTTP.JSON.LitJson;
using VRT.Core;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using VRT.Orchestrator.Responses;

namespace VRT.Orchestrator.Wrapping
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
        public static T ParseJsonString<T>(string data)
        {
            try
            {
                return JsonMapper.ToObject<T>(data);

            }
            catch (JsonException ex)
            {
                UnityEngine.Debug.LogError("OrchestratorElements: Error parsing JSON. See log message.");
                UnityEngine.Debug.Log($"OrchestratorElements: Exception: {ex}");
                UnityEngine.Debug.Log($"OrchestratorElements: JSON data: {data}");
                return default(T);
            }
        }
        public static T ParseJsonData<T>(JsonData data)
        {
            return ParseJsonString<T>(data.ToJson());
        }
    }

    public class User: OrchestratorElement
    {
        public string userId = "";
        public string userName = "";
        public string userPassword = "";
        public UserData userData;
        public SfuData sfuData;

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

        public string userAudioUrl = "";

        public string webcamName = "";
        public string microphoneName = "";

        public UserRepresentationType userRepresentationType = UserRepresentationType.SimpleAvatar;

        // empty constructor callled by the JsonData parser
        public UserData() { }


        public string AsJsonString()
        {
            return JsonMapper.ToJson(this);
        }
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
