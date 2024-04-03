using System;
using System.IO;
using System.Runtime.InteropServices;

namespace VRT.Core
{

    public class API_kernel
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetModuleFileName(IntPtr hModule, System.Text.StringBuilder modulePath, int nSize);
    }
}