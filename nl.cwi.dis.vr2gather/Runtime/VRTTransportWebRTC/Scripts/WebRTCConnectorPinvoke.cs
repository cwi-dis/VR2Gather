using AOT;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using UnityEngine;
using VRT.Core;

using Debug = UnityEngine.Debug;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Policy;
using Cwipc;
using System.ComponentModel.Composition;

namespace VRT.Transport.WebRTC
{
 
    public unsafe class WebRTCConnectorPinvoke
    {
        // Create string param callback delegate
        public delegate void debugCallback(IntPtr request, int console_level, int color, int size);

        enum Color { red, green, blue, black, white, yellow, orange };

        [MonoPInvokeCallback(typeof(debugCallback))]
        static void OnDebugCallback(IntPtr message, int console_level, int color, int size)
        {
            // Ptr to string
            string debug_string;
            try {
                debug_string = Marshal.PtrToStringAnsi(message, size);
            }
            catch(ArgumentException) {
                debug_string = $"OnDebugCallback: Marshal.PtrToStringAnsi() raised an exception (string size={size})";
            }
            // Add specified color
            debug_string = $"WebRTCConnectorPinvoke: <color={((Color)color).ToString()}>{debug_string}</color>";
            // Output the message
            if (console_level == 0)
            {
                Debug.Log(debug_string);
            } else if (console_level == 1)
            {
                Debug.LogWarning(debug_string);
            } else
            {
                Debug.LogError(debug_string);
            }
        }

        public static void ConfigureDebug(string logFileDirectory, int debugLevel)
        {
            Debug.Log($"WebRTCConnector: Installing message callback");
            WebRTCConnectorPinvoke.RegisterDebugCallback(OnDebugCallback);
            WebRTCConnectorPinvoke.set_logging(logFileDirectory, debugLevel);
        }
        
        // Logging in Unity
        [DllImport("WebRTCConnector")]
        public static extern void set_logging(string log_directory, int logLevel);
        [DllImport("WebRTCConnector", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterDebugCallback(debugCallback cb);

        // Initialization and cleanup
        [DllImport("WebRTCConnector")]
        public static extern int initialize(string ip_send, UInt32 port_send, string ip_recv, UInt32 port_recv,
            UInt32 number_of_tiles, UInt32 client_id, string api_version);
        [DllImport("WebRTCConnector")]
        public static extern void clean_up();

        // Video data
        [DllImport("WebRTCConnector")]
        public static extern int send_tile(byte* data, UInt32 size, UInt32 tile_number, UInt32 quality);
        [DllImport("WebRTCConnector")]
        public static extern int get_tile_size(UInt32 client_id, UInt32 tile_number);
        [DllImport("WebRTCConnector")]
        public static extern void retrieve_tile(byte* buffer, UInt32 size, UInt32 client_id, UInt32 tile_number);

        // Audio data
        [DllImport("WebRTCConnector")]
        public static extern int send_audio(byte* data, UInt32 size);
        [DllImport("WebRTCConnector")]
        public static extern int get_audio_size(UInt32 client_id);
        [DllImport("WebRTCConnector")]
        public static extern void retrieve_audio(byte* buffer, UInt32 size, UInt32 client_id);

        // Control messages (e.g., quality decision-making)
        [DllImport("WebRTCConnector")]
        public static extern int send_control_packet(byte* data, UInt32 size);
    }
}
