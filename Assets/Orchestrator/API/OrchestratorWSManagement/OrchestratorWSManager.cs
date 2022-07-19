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

using BestHTTP.SocketIO;
using LitJson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRT.Orchestrator.Wrapping;

/** NOTES:
 * NOTE 1 CommandId:
 *              the Id number incremented: this is not used for the moment to manage the sent commands (but could be later), 
 *              BUT: BestHTTP library that manages the socketio doesn't send an event with an empty body, so forcing the commandId
 *              inside the parameters allows the BestHTTP library to be used
 *              
 * NOTE 2 Json data:
 *              The orchestrator store list of elements not as an array, but on JS as an object with each element defined in the
 *              main object by its ID. That allows to access it directly via its ID on JS, but it's not parsable directly on C#.
 **/

namespace VRT.Orchestrator.WSManagement
{
    public interface IOrchestratorConnectionListener
    {
        void OnSocketConnect();
        void OnSocketConnecting();
        void OnSocketDisconnect();
        void OnSocketError(ResponseStatus message);
    }

    // interface to implement to be updated from messages exchanged on the socketio
    public interface IMessagesListener
    {
        void OnOrchestratorResponse(int commandID, int status, string response);
        void OnOrchestratorRequest(string request);
    }

    // the class that owns the orchestrator connection logics
    public class OrchestratorWSManager
    {
        #region declarations

        // orchestrator url
        private string OrchestratorUrl;

        // To manage the connection with the orchestrator
        private SocketManager Manager;

        // Is this client is connected to the orchestrator
        public Boolean isSocketConnected = false;

        // To deal with the commands sent and the responses
        public Boolean IsWaitingResponse = false;
        public int commandId = 0;

        // listener for the connection state with the orchestrator upon socketio
        private IOrchestratorConnectionListener connectionListener;

        // Listener for the messages exchanged with the Orchetsrator
        public IMessagesListener messagesListener;

        // Last command sent
        public OrchestratorCommand sentCommand;

        private List<OrchestratorCommand> commandQueue = new List<OrchestratorCommand>();

        #endregion

        // Constructor
        public OrchestratorWSManager(string orchestratorUrl, IOrchestratorConnectionListener connectionListener, IMessagesListener messagesListener)
        {
            this.OrchestratorUrl = orchestratorUrl;
            this.connectionListener = connectionListener;
            this.messagesListener = messagesListener;
        }
        public OrchestratorWSManager(string orchestratorUrl, IOrchestratorConnectionListener connectionListener) : this(orchestratorUrl, connectionListener, null)
        {
        }

        #region socket connection & disconnection

        // socket.io connection to the orchestrator
        public void SocketConnect(List<OrchestratorMessageReceiver> messagesToManage)
        {
            // Socket config
            SocketOptions options = new SocketOptions();
            options.AutoConnect = false;
            options.ConnectWith = BestHTTP.SocketIO.Transports.TransportTypes.WebSocket;
            
            // Create the Socket.IO manager
            Manager = new SocketManager(new Uri(OrchestratorUrl), options);
            
            Manager.Encoder = new BestHTTP.SocketIO.JsonEncoders.LitJsonEncoder(); //
            Manager.Socket.AutoDecodePayload = false;
            Manager.Socket.On(SocketIOEventTypes.Connect, OnServerConnect);
            Manager.Socket.On(SocketIOEventTypes.Disconnect, OnServerDisconnect);
            Manager.Socket.On("connecting", OnServerConnecting);
            Manager.Socket.On(SocketIOEventTypes.Error, OnServerError);

            // Listen to the messages received
            messagesToManage.ForEach(delegate (OrchestratorMessageReceiver messageReceiver)
            {
                Manager.Socket.On(messageReceiver.SocketEventName, messageReceiver.OrchestratorMessageCallback);
            });
            
            // Open the socket
            Manager.Open();
        }

        // Socket.io disconnection
        public void SocketDisconnect()
        {
            Manager.Close();
        }

        // Called when socket is connected to the orchestrator
        void OnServerConnect(Socket socket, Packet packet, params object[] args)
        {
            isSocketConnected = true;

            if (connectionListener != null)
            {
                connectionListener.OnSocketConnect();
            }
        }

        // Called when socket is connecting to the orchestrator
        void OnServerConnecting(Socket socket, Packet packet, params object[] args)
        {
            isSocketConnected = false;

            if (connectionListener != null)
            {
                connectionListener.OnSocketConnecting();
            }
        }

        // Called when socket is disconnected from the orchestrator
        void OnServerDisconnect(Socket socket, Packet packet, params object[] args)
        {
            isSocketConnected = false;

            if (connectionListener != null)
            {
                connectionListener.OnSocketDisconnect();
            }
        }

        // Called when socket connection error occurs
        void OnServerError(Socket socket, Packet packet, params object[] args)
        {
            if(args != null)
            {
                Error error = args[0] as Error;
                ResponseStatus status = new ResponseStatus((int)error.Code, error.Message);

                if (connectionListener != null)
                {
                    connectionListener.OnSocketError(status);
                }
            }
        }

        #endregion

        #region messages socket.io managing

        public void EmitPacket(OrchestratorCommand command)
        {
            lock (this) {
                if (command.Parameters != null) {
                    object[] parameters = new object[command.Parameters.Count];

                    for (int i = 0; i < parameters.Length; i++) {
                        parameters[i] = command.Parameters[i].ParamValue;
                    }

                    // emit the packet on socket.io
                    Manager.Socket.Emit(command.SocketEventName, null, parameters);
                }
            }
        }

        // Emit a command
        public void EmitCommand(OrchestratorCommand command)
        {
            lock (this) {
                // the JsonData that will own the parameters and their values
                JsonData parameters = new JsonData();

                if (command.Parameters != null) {
                    // for each parameter defined in the command, fill the parameter with its value
                    command.Parameters.ForEach(delegate (Parameter parameter) {
                        if (parameter.ParamValue != null) {
                            if (parameter.type == typeof(bool)) {
                                parameters[parameter.ParamName] = (bool)parameter.ParamValue;
                            } else {
                                parameters[parameter.ParamName] = parameter.ParamValue.ToString();
                            }
                        } else {
                            parameters[parameter.ParamName] = "";
                        }
                    });
                }

                // increment the commandId and add it to the command parameters (see NOTE on top of file)
                commandId++;
                parameters["commandId"] = commandId;
                command.commandID = commandId;

                commandQueue.Add(command);

                // warn the messages Listener that new message is emitted
                if (messagesListener != null) {
                    messagesListener.OnOrchestratorRequest(command.SocketEventName + " " + parameters.ToJson());
                }

                // emit the command on socket.io
                Manager.Socket.Emit(command.SocketEventName, OnAckCallback, parameters);

                /*
                // send the command
                if (!SendCommand(command.SocketEventName, parameters))
                {
                    UnityEngine.Debug.Log("[OrchestratorWSManager][EmitCommand] Fail to send command: " + command.SocketEventName);
                    // problem while sending the command
                    sentCommand = null;
                    return false;
                }
                // command succesfully sent
                sentCommand = command;
                return true;
                */
            }
        }

        // Callback that is called on a command response
        private void OnAckCallback(Socket socket, Packet originalPacket, params object[] args)
        {
            int ackedCmdIndex = -1;
            OrchestratorCommand ackedCmd = null;

            OrchestratorResponse response = JsonToOrchestratorResponse(originalPacket.Payload);

            //warn the messages Listener that a response is received from the orchestrator
            if (messagesListener != null)
            {
                messagesListener.OnOrchestratorResponse(response.commandId, response.error, originalPacket.Payload);
            }
            lock(this)
            {
                for (int i = 0; i < commandQueue.Count; i++)
                {
                    if (response.commandId == commandQueue[i].commandID)
                    {
                        ackedCmd = commandQueue[i];
                        if (ackedCmdIndex >= 0)
                        {
                            throw new Exception($"OrchestratorWSManager: ACK for commandId {response.commandId} which has duplicate entry");
                        }
                        ackedCmdIndex = i;
                    }
                }
                if (ackedCmdIndex < 0)
                {
                    throw new Exception($"OrchestratorWSManager: ACK for commandId {response.commandId} which is unknown");
                }
                commandQueue.RemoveAt(ackedCmdIndex);
            }
  
            ackedCmd.ResponseCallback.Invoke(ackedCmd, response);

            /*
            for (int i = 0; i < commandQueue.Count; i++)
            {
                UnityEngine.Debug.Log(commandQueue[i].commandID);
            }
            */

            //IsWaitingResponse = false;
            // If a function is declared in the grammar to treat the response 
            // for this command, then call this function
            //sentCommand?.ResponseCallback.Invoke(sentCommand, response);
        }

        // Parse the first level of this JSON string response
        public OrchestratorResponse JsonToOrchestratorResponse(string jsonMessage)
        {
            JsonData jsonResponse = JsonMapper.ToObject(jsonMessage);
            OrchestratorResponse response = new OrchestratorResponse();
            response.commandId = (int)jsonResponse[0]["commandId"];
            response.error = (int)jsonResponse[0]["error"];
            response.message = jsonResponse[0]["message"].ToString();
            response.body = jsonResponse[0]["body"];
            return response;
        }

        #endregion
    }
}