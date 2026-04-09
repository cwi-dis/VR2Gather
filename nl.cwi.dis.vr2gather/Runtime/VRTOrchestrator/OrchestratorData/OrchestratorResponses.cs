namespace VRT.Orchestrator.Responses
{
    // Status returned with every orchestrator response.
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

    // Class that stores a user data-stream packet incoming from the orchestrator.
    // Part of the contract: used by transport assemblies.
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

    // Class that stores a user message incoming from the orchestrator.
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

    // Class that stores a user event incoming from the orchestrator.
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
