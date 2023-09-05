using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRT.Core;


namespace VRT.Core
{

   
    /// <summary>
    /// Per-session parameters that should be the same for all participants in the experience.
    /// </summary>
    public class SessionConfig
    {
        public static SessionConfig.ProtocolType ProtocolFromString(string s)
        {
            switch (s.ToLower())
            {
                case "socketio":
                    return SessionConfig.ProtocolType.SocketIO;
                case "dash":
                    return SessionConfig.ProtocolType.Dash;
                case "tcp":
                    return SessionConfig.ProtocolType.TCP;
                case "webrtc":
                    return SessionConfig.ProtocolType.WebRTC;
            }
            throw new System.Exception($"Unknown protocoltype \"{s}\"");
        }

        public static string ProtocolToString(SessionConfig.ProtocolType v)
        {
            return v.ToString().ToLower();
        }

        public enum ProtocolType
        {
            None = 0,
            SocketIO = 1,
            Dash = 2,
            TCP = 3,
            WebRTC = 4,
        };

        /// <summary>
		/// The experience name (so _not_ the session name or the scene name)
		/// </summary>
        public string scenarioName;
        /// <summary>
		/// Optional string for variants of an experience.
		/// </summary>
        public string scenarioVariant;
        /// <summary>
		/// The transport protocol to use (socketio, tcp, dash)
		/// </summary>
        public ProtocolType protocolType = ProtocolType.SocketIO;
        /// <summary>
		/// The codec to use for webcam video (4CC)
		/// </summary>
        public string videoCodec = "h264";
        /// <summary>
		/// The codec to use for pointcloud transmission (4CC)
		/// </summary>
        public string pointCloudCodec = "cwi1";
        /// <summary>
		/// The codec to use for voice (4CC)
		/// </summary>
        public string voiceCodec = "VR2A";

        public static SessionConfig _Instance;
        public static SessionConfig Instance
        {
            get
            {
                if (_Instance == null) _Instance = new SessionConfig();
                return _Instance;
            }
        }

        public static void FromJson(string message)
        {
            var inst = Instance;
            JsonUtility.FromJsonOverwrite(message, inst);
        }
    }    
}
