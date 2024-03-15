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
using Best.SocketIO;
using Best.HTTP.JSON.LitJson;

// class used as a convenience to represent the commands and the responses
namespace VRT.Orchestrator.WSManagement
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
        public Parameter(Parameter old)
        {
            ParamName = old.ParamName;
            type = old.type;
            ParamValue = old.ParamValue;
        }

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
        public OrchestratorCommand(OrchestratorCommand old)
        {
            SocketEventName = old.SocketEventName;
            commandID = old.commandID;
            if (old.Parameters != null)
            {
                Parameters = new List<Parameter>();
                foreach (var p in old.Parameters)
                {
                    Parameters.Add(new Parameter(p));
                }
            }
           
            ResponseCallback = old.ResponseCallback;
        }

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
        public Action<Socket> OrchestratorMessageCallback;

        // Constructors
        public OrchestratorMessageReceiver(string socketEventName,
            Action<Socket> orchestratorMessageCallback)
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