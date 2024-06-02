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
        public string protocolType = "socketio";
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
