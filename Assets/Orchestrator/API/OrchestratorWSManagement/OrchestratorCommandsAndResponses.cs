using System;
using System.Collections.Generic;
using BestHTTP;
using BestHTTP.SocketIO.Events;
using LitJson;

// class used as a convenience to represent the commands and the responses
namespace OrchestratorWSManagement
{
    // template for functions that treat the responses (command = the command sent, response = the response to parse)
    public delegate void ResponseCallbackManager(OrchestratorCommand command, OrchestratorResponse response);

    //// template for functions that treats a message from the orchestrator
    //public delegate void OrchestratorMessageCallbackManager(OrchestratorMessage message);

    // Class that defines the parameters used for the commands
    public class Parameter
    {
        // name of the parameter
        public string ParamName;
        // type of the parameter value (not used anymore but could be reused to add genericity)
        public Type type;
        // the object owning the value of the parameter
        public Object ParamValue;

        // Constructor
        public Parameter(string paramName, Type type, Object paramValue)
        {
            ParamName = paramName;
            ParamValue = paramValue;
            this.type = type;
        }
        public Parameter(string paramName, Type type): this(paramName, type, null){}
    }

    // Class that defines the availble commands
    public class OrchestratorCommand
    {
        // the name of the event sent on the socket (= the name of the command)
        public string SocketEventName;

        // the id of the command sent to link with Ack callback
        public int commandID;

        // list of parameters for this command
        public List<Parameter> Parameters;

        // the function that will be used to process the response
        public ResponseCallbackManager ResponseCallback;


        // Constructors
        public OrchestratorCommand(string socketEventName,
            List<Parameter> parameters,
            ResponseCallbackManager responseCallback)
        {
            this.SocketEventName = socketEventName;
            this.Parameters = parameters;
            this.ResponseCallback = responseCallback;
        }
        public OrchestratorCommand(string socketEventName, List<Parameter> parameters) : this(socketEventName, parameters, null)
        {
        }

        // retrieves a parameter by name
        public Parameter GetParameter(string parameterName)
        {
            if (Parameters != null)
            {
                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (Parameters[i].ParamName == parameterName)
                    {
                        return Parameters[i];
                    }
                }
            }
            return null;
        }
    }

    public class OrchestratorMessageReceiver
    {
        // the name of the event sent on the socket (= the name of the command)
        public string SocketEventName;

        // the function that will be used to process the response
        public SocketIOCallback OrchestratorMessageCallback;

        // Constructors
        public OrchestratorMessageReceiver(string socketEventName,
            SocketIOCallback orchestratorMessageCallback)
        {
            this.SocketEventName = socketEventName;
            this.OrchestratorMessageCallback = orchestratorMessageCallback;
        }
    }

    // class that stores the response to a command before the parsing of the jsondata
    public class OrchestratorResponse
    {
        public int commandId;
        public int error;
        public string message;
        public JsonData body;
    }
}