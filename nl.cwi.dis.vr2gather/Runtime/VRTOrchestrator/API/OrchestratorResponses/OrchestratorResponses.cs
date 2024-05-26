using System;
using System.Collections.Generic;
using VRT.Orchestrator.Wrapping;

namespace VRT.Orchestrator.Responses { 
    public interface IOrchestratorResponseBody { }

    public class OrchestratorResponse<T>
    {
        public int error { get; set; }
        public string message { get; set; }

        public T body;

        public ResponseStatus ResponseStatus {
            get {
                return new ResponseStatus(error, message);
            }
        }
    }

    public class EmptyResponse : IOrchestratorResponseBody {}

    public class VersionResponse : IOrchestratorResponseBody {
        public string orchestratorVersion;
    }

    public class LoginResponse : IOrchestratorResponseBody {
        public string userId;
    }

    public class SessionUpdateEventData {
        public string userId;
        public User userData;
    }

    public class SessionUpdate {
        public string eventId;
        public SessionUpdateEventData eventData;
    }

    public class SceneEvent {
        public string sceneEventFrom;
    }

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

    // class that stores a user data-stream packet incoming from the orchestrator
    public class UserDataStreamPacket
    {
        public string dataStreamUserID;
        public string dataStreamType;
        public string dataStreamDesc;
        public byte[] dataStreamPacket;

        public UserDataStreamPacket() { }

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

    // class that stores a user message incoming from the orchestrator
    public class UserMessage
    {
        public string messageFrom;
        public string messageFromName;
        public string message;

        public UserMessage() { }

        public UserMessage(string pFromID, string pFromName, string pMessage)
        {
            messageFrom = pFromID;
            messageFromName = pFromName;
            message = pMessage;
        }
    }

    // class that stores a user event incoming from the orchestrator
    // necessary new parameters welcomed
    public class UserEvent
    {
        public string sceneEventFrom;
        public string sceneEventData;

        public UserEvent() { }

        public UserEvent(string pFromID, string pMessage)
        {
            sceneEventFrom = pFromID;
            sceneEventData = pMessage;
        }
    }
}
