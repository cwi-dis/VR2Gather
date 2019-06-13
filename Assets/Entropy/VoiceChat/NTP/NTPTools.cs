using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

public class NTPTools {
    public static UInt64 offset;

    [StructLayout(LayoutKind.Explicit)]
    public struct NTPTime
    {
        [FieldOffset(0)]
        public UInt64 time;
        [FieldOffset(0)]
        public byte T0;
        [FieldOffset(0)]
        public byte T1;
        [FieldOffset(1)]
        public byte T2;
        [FieldOffset(2)]
        public byte T3;
        [FieldOffset(3)]
        public byte T4;
        [FieldOffset(4)]
        public byte T5;
        [FieldOffset(5)]
        public byte T6;
        [FieldOffset(6)]
        public byte T7;
        [FieldOffset(7)]
        public byte T8;
    }

    public static void GetNetworkTime() {
        //default Windows time server
        const string ntpServer = "time.windows.com";

        // NTP message size - 16 bytes of the digest (RFC 2030)
        var ntpData = new byte[48];

        //Setting the Leap Indicator, Version Number and Mode values
        ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

        var addresses = Dns.GetHostEntry(ntpServer).AddressList;

        //The UDP port number assigned to NTP is 123
        var ipEndPoint = new IPEndPoint(addresses[0], 123);
        //NTP uses UDP

        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            socket.Connect(ipEndPoint);

            //Stops code hang if NTP is blocked
            socket.ReceiveTimeout = 3000;

            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();
        }

        const byte serverReplyTime = 40;
        ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);
        ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

        intPart = SwapEndianness(intPart);
        fractPart = SwapEndianness(fractPart);

        offset = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L) - (ulong)(UnityEngine.Time.realtimeSinceStartup * 1000.0f);
    }

    static uint SwapEndianness(ulong x) {
        return (uint)(((x & 0x000000ff) << 24) + ((x & 0x0000ff00) << 8) + ((x & 0x00ff0000) >> 8) + ((x & 0xff000000) >> 24));
    }

    static NTPTime temp;
    public static NTPTime GetNTPTime() {
        temp.time = (UInt64)(UnityEngine.Time.realtimeSinceStartup * 1000.0f) + offset;
        return temp;
    }
}
