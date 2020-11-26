using UnityEngine;

namespace Pilots
{

    /// <summary>
    /// Base class for message to be sent via the SendEventToXXX API
    /// </summary>
    public class BaseMessage
    {
        public string SenderId;
        public string TimeStamp;
    }
}