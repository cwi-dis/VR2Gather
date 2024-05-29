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

namespace VRT.Transport.WebRTC
{
 
    public unsafe class WebRTCConnectorPinvoke
    {
        // Create string param callback delegate
        public delegate void debugCallback(IntPtr request, int color, int size);

        enum Color { red, green, blue, black, white, yellow, orange };

        [MonoPInvokeCallback(typeof(debugCallback))]
        static void OnDebugCallback(IntPtr request, int color, int size)
        {
            // Ptr to string
            string debug_string = Marshal.PtrToStringAnsi(request, size);
            // Add specified color
            debug_string = $"WebRTCConnectorPinvoke: <color={((Color)color).ToString()}>{debug_string}</color>";
            // Log the string
            Debug.Log(debug_string);
        }

        public static void ConfigureDebug(string logFileDirectory, int debugLevel)
        {
            Debug.Log($"WebRTCConnector: Installing message callback");
            WebRTCConnectorPinvoke.RegisterDebugCallback(OnDebugCallback);
            
            WebRTCConnectorPinvoke.set_logging(logFileDirectory, debugLevel);
        }
        
        [DllImport("WebRTCConnector")]
        public static extern void set_logging(string log_directory, int logLevel);
        [DllImport("WebRTCConnector", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RegisterDebugCallback(debugCallback cb);
        [DllImport("WebRTCConnector")]
        public static extern int initialize(string ip_send, UInt32 port_send, string ip_recv, UInt32 port_recv,
            UInt32 number_of_tiles, UInt32 client_id, string api_version);
        [DllImport("WebRTCConnector")]
        public static extern void clean_up();
        [DllImport("WebRTCConnector")]
        public static extern int send_tile(byte* data, UInt32 size, UInt32 tile_number);
        [DllImport("WebRTCConnector")]
        public static extern int get_tile_size(UInt32 client_id, UInt32 tile_number);
        [DllImport("WebRTCConnector")]
        public static extern void retrieve_tile(byte* buffer, UInt32 size, UInt32 client_id, UInt32 tile_number);

        // Audio frame functions
        [DllImport("WebRTCConnector")]
        public static extern int send_audio(byte* data, UInt32 size);
        [DllImport("WebRTCConnector")]
        public static extern int get_audio_size(UInt32 client_id);
        [DllImport("WebRTCConnector")]
        public static extern void retrieve_audio(byte* buffer, UInt32 size, UInt32 client_id);

        [DllImport("WebRTCConnector")]
        public static extern int send_control(byte* data, UInt32 size);
        [DllImport("WebRTCConnector")]
        public static extern int get_control_size();
        [DllImport("WebRTCConnector")]
        public static extern void retrieve_control(byte* buffer);
    }
}
