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

using System.Collections.Generic;
using VRT.Orchestrator.Responses;

//Interfaces to be implemented to supervise the orchestrator
namespace VRT.Orchestrator.Wrapping
{
    public interface IOrchestratorConnectionListener
    {
        void OnSocketConnect();
        void OnSocketConnecting();
        void OnSocketDisconnect();
        void OnSocketError(ResponseStatus message);
    }

    // Interface to implement to listen the user messages emitted spontaneously
    // by the orchestrator
    public interface IUserMessagesListener
    {
        void OnUserMessageReceived(UserMessage userMessage);
        void OnMasterEventReceived(UserEvent pSceneEventData);
        void OnUserEventReceived(UserEvent pSceneEventData);
    }

    // Interface to implement to listen the user events emitted spontaneously
    // from the session updates by the orchestrator
    public interface IUserSessionEventsListener
    {
        void OnUserJoinedSession(string userID, User user);
        void OnUserLeftSession(string userID);
    }

    // Interface for clients that will use the orchestrator wrapper
    // each function is the response of a command and contains the data returned by the orchestrator
    // functions are called by the wrapper upon the response of the orchestrator
    public interface IOrchestratorResponsesListener
    {
        void OnError(ResponseStatus status);
        void OnConnect();
        void OnConnecting();
        void OnGetOrchestratorVersionResponse(ResponseStatus status, string version);
        void OnDisconnect();

        void OnLoginResponse(ResponseStatus status, string userId);
        void OnLogoutResponse(ResponseStatus status);

        void OnGetNTPTimeResponse(ResponseStatus status, NtpClock ntpTime);

        void OnGetSessionsResponse(ResponseStatus status, List<Session> sessions);
        void OnAddSessionResponse(ResponseStatus status, Session session);
        void OnGetSessionInfoResponse(ResponseStatus status, Session session);
        void OnDeleteSessionResponse(ResponseStatus status);
        void OnJoinSessionResponse(ResponseStatus status, Session session);
        void OnLeaveSessionResponse(ResponseStatus status);
        void OnUpdateUserDataJsonResponse(ResponseStatus status);

        void OnSendMessageResponse(ResponseStatus status);
        void OnSendMessageToAllResponse(ResponseStatus status);

    }
}