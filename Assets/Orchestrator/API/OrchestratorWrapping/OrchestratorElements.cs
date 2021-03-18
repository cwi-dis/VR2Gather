﻿//  © - 2020 – viaccess orca 
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

using LitJson;
using System.Collections.Generic;
using VRT.Core;

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
        public string userIP = "";
        public string userMQexchangeName = "";
        public string userMQurl = "";

        public string userPCurl = "";
        public string userAudioUrl = "";

        public string webcamName = "";
        public string microphoneName = "";

        public UserRepresentationType userRepresentationType;

        // empty constructor callled by the JsonData parser
        public UserData() { }
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
