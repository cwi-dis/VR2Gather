using System;
using System.Runtime.InteropServices;

public class mcu {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct Player {
		byte myID;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
		byte[] fov;
		string url;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		float[] position;
		float rotation;
	};

	public class _API {
		[DllImport("MCU-UB")]
		internal extern static bool Connect([MarshalAs(UnmanagedType.LPStr)]string _ip, int _port);

		[DllImport("MCU-UB")]
		internal extern static bool SendDisconnect(byte id);

		[DllImport("MCU-UB")]
		internal extern static bool SendInit(byte id, [MarshalAs(UnmanagedType.LPStr)]string url, float[] position, float rotation);

		[DllImport("MCU-UB")]
		internal extern static bool SendPosition(byte id, float[] position);

		[DllImport("MCU-UB")]
		internal extern static bool SendRotation(byte id, float rotation);

		[DllImport("MCU-UB")]
		internal extern static bool SendFOV(byte id, byte[] fov);
	}
}
