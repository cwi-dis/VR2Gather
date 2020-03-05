using System;
using System.Runtime.InteropServices;

public class mcu {
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct Player {
		byte myID;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
		int[] fov;
		int[] lod;
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
		internal extern static bool SendInit(byte id, [MarshalAs(UnmanagedType.LPStr)]string url, float[] position, float rotation, int[] fov, int[] lod);

		[DllImport("MCU-UB")]
		internal extern static bool SendPosition(byte id, float[] position);

		[DllImport("MCU-UB")]
		internal extern static bool SendRotation(byte id, float rotation);

		[DllImport("MCU-UB")]
		internal extern static bool SendFOV(byte id, int[] fov);

		[DllImport("MCU-UB")]
		internal extern static bool SendLOD(byte id, int[] lod);
	}
}
