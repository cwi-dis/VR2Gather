using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct SystemTime {
    public ushort Year;
    public ushort Month;
    public ushort DayOfWeek;
    public ushort Day;
    public ushort Hour;
    public ushort Minute;
    public ushort Second;
    public ushort Millisecond;
};

public class SyncTool {

    [DllImport("kernel32.dll", EntryPoint = "SetSystemTime", SetLastError = true)]
    public extern static bool Win32SetSystemTime(ref SystemTime sysTime);

    public static DateTime sysTime; // The system clock time
    public static DateTime netTime; // The NTP server clock time
    public static DateTime myTime;  // The clock of the client synced with the NTP (should be same value as NTP)
    public static long delta;       // The delta value between system clock and NTP clock
    public static long offset = 0 * TimeSpan.TicksPerSecond;

    public static DateTime GetNetworkTime() {
        const string ntpServer = "time.google.com";

        // NTP message size - 16 bytes of the digest (RFC 2030)
        var ntpData = new byte[48];

        //Setting the Leap Indicator, Version Number and Mode values
        ntpData[0] = 0x1B; //LI = 0 (no warning), VN = 3 (IPv4 only), Mode = 3 (Client Mode)

        var addresses = Dns.GetHostEntry(ntpServer).AddressList;

        //The UDP port number assigned to NTP is 123
        var ipEndPoint = new IPEndPoint(addresses[0], 123);
        //NTP uses UDP

        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)) {
            socket.Connect(ipEndPoint);

            //Stops code hang if NTP is blocked
            socket.ReceiveTimeout = 3000;

            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();
        }

        //Offset to get to the "Transmit Timestamp" field (time at which the reply 
        //departed the server for the client, in 64-bit timestamp format."
        const byte serverReplyTime = 40;

        //Get the seconds part
        ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

        //Get the seconds fraction
        ulong fractPart = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

        //Convert From big-endian to little-endian
        intPart = SwapEndianness(intPart);
        fractPart = SwapEndianness(fractPart);

        var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);

        //**UTC** time
        var networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).AddMilliseconds((long)milliseconds);
        
        return networkDateTime.ToLocalTime();
    }

    public static DateTime GetSystemTime() {
        return DateTime.Now;
    }

    public static void UpdateTimes() {
        netTime = GetNetworkTime();
        sysTime = GetSystemTime();
        UpdateDelta();
        UpdateMyTime();
        //Debug.Log("NETWORK: " + netTime);
        //Debug.Log("SYSTEM: " + sysTime);
        //Debug.Log("MY TIME: " + myTime);
        //Debug.Log("TOOL: " + ToDateTime(GetMyTimeString()));
    }

    public static void UpdateDelta() {
        netTime = GetNetworkTime();
        sysTime = GetSystemTime();
        delta = sysTime.Ticks - netTime.Ticks;
    }

    public static long GetDelta() {
        UpdateDelta();
        return delta;
    }
    
    public static void UpdateMyTime() {
        UpdateDelta();
        myTime = new DateTime(GetSystemTime().Ticks + offset - delta);
    }

    public static DateTime GetMyTime() {
        UpdateMyTime();
        return myTime;
    }

    /// <Summary> Converts myTime to string format HH:mm:ss.fff </Summary>
    /// <returns> Returns the string of myTime HH:mm:ss.fff </returns>
    public static string GetMyTimeString() {
        return GetMyTime().ToString("HH:mm:ss.fff");
    }

    /// <Summary> Parse to DateTime format a given string </Summary>
    /// <param name="str"> string with HH:mm:ss.fff format </param>
    /// <returns> Returns the DateTime conversion of the given string</returns>
    public static DateTime ToDateTime(string str) {
        DateTime dateTime = new DateTime(myTime.Year, myTime.Month, myTime.Day);
        string aux = dateTime.ToString("MM/dd/yyyy ");
        aux = aux + str;
        dateTime = DateTime.ParseExact(aux, "MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture);
        return dateTime;
    }

    /// <Summary> Get the delay in seconds (double) between this client and a message received </Summary>
    /// <param name="received"> The DateTime of the received message </param>
    /// <returns> Returns the value in seconds (double) of the delay between this client and the message received </returns>
    public static double GetDelay(DateTime received){
        long ticksDelay = myTime.Ticks - received.Ticks;
        TimeSpan ts = TimeSpan.FromTicks(ticksDelay);
        return ts.TotalSeconds;
    }

    public static double GetDelayMilis(DateTime received) {
        long ticksDelay = myTime.Ticks - received.Ticks;
        TimeSpan ts = TimeSpan.FromTicks(ticksDelay);
        return ts.TotalMilliseconds;
    }

    static uint SwapEndianness(ulong x) {
        return (uint)(((x & 0x000000ff) << 24) +
                      ((x & 0x0000ff00) << 8) +
                      ((x & 0x00ff0000) >> 8) +
                      ((x & 0xff000000) >> 24));
    }

    public static void SyncSystemClock() {
        // Set system date and time
        SystemTime updatedTime = new SystemTime();
        updatedTime.Year = (ushort)2009;
        updatedTime.Month = (ushort)3;
        updatedTime.Day = (ushort)16;
        updatedTime.Hour = (ushort)10;
        updatedTime.Minute = (ushort)0;
        updatedTime.Second = (ushort)0;

        DateTime ntpTime = GetNetworkTime();

        updatedTime.Year = (ushort)ntpTime.Year;
        updatedTime.Month = (ushort)ntpTime.Month;
        updatedTime.Day = (ushort)ntpTime.Day;
        updatedTime.Hour = (ushort)ntpTime.Hour;
        updatedTime.Minute = (ushort)ntpTime.Minute;
        updatedTime.Second = (ushort)ntpTime.Second;

        // Call the unmanaged function that sets the new date and time instantly
        Win32SetSystemTime(ref updatedTime);
    }
}
