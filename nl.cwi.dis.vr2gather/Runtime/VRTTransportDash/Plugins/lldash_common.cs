using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using VRT.Core;
using VRT.NativeLibraries;
using System.Diagnostics;
namespace VRT.Transport.Dash
{

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DashStreamDescriptor
        {
            public uint MP4_4CC;
            public uint tileNumber;    // objectX. In VRTogether, for pointclouds, we use this field for tileNumber
            public int nx;    // objectY. In VRTogether, for pointclouds, we use this field for nx
            public int ny;    // objectWidth. In VRTogether, for pointclouds, we use this field for ny
            public int nz;    // objectHeight. In VRTogether, for pointclouds, we use this field for nz
            public uint totalWidth;
            public uint totalHeight;
        }

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    public class API_libdl
    {
        [DllImport("libdl.dylib", CharSet = CharSet.Auto)]
        public static extern IntPtr dlopen(string fileName, int flags);
        [DllImport("libdl.dylib", CharSet = CharSet.Auto)]
        public static extern IntPtr dlsym(IntPtr handle, string symbol);
        [DllImport("libdl.dylib", CharSet = CharSet.Auto)]
        public static extern int dlclose(IntPtr handle);
    }

#endif   
    public class Loader
    {
        private static bool didSetMSPaths = false;

        public static void PreLoadModule(string module_base)
        {
            string libPath = Path.Combine(VRT.NativeLibraries.VRTNativeLoader.platformLibrariesPath, module_base);
            string overrideLibPath = VRTConfig.Instance.TransportDash.nativeLibraryPath;
            if (!string.IsNullOrEmpty(overrideLibPath))
            {
                libPath = Path.Combine(overrideLibPath, module_base);
            }
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            UnityEngine.Debug.Log($"VRT.Transport.Dash.Loader: will call LoadLibrary({libPath})");
            IntPtr rv = API_libdl.dlopen(libPath, 0x0002); // RTLD_LAZY
            if (rv == IntPtr.Zero)
            {
                UnityEngine.Debug.LogError($"VRT.Transport.Dash.Loader: API_libdl.dlopen({libPath}) failed");
                return;
            }
            UnityEngine.Debug.Log($"VRT.Transport.Dash.Loader: LoadLibrary({libPath}) returned {rv}");
            string dirName = Path.GetDirectoryName(libPath);
            UnityEngine.Debug.Log($"VRT.Transport.Dash.Loader: SIGNALS_SMD_PATH={dirName}");
            Environment.SetEnvironmentVariable("SIGNALS_SMD_PATH", dirName);
            didSetMSPaths = true;
#endif
        }

        public static void PostLoadModule(string module_base)
        {
            if (didSetMSPaths) return;
#if old
            // This code only works on Windows. And we don't need it anymore anyway, because we alsways include the dynamic libraries.
            IntPtr hMod = API_kernel.GetModuleHandle(module_base);
            if (hMod == IntPtr.Zero)
            {
                UnityEngine.Debug.Log($"VRT.Transport.Dash.Loader: Cannot get handle for {module_base}, GetModuleHandle returned NULL. PATH={Environment.GetEnvironmentVariable("PATH")}, SIGNALS_SMD_PATH={Environment.GetEnvironmentVariable("SIGNALS_SMD_PATH")} ");
                UnityEngine.Debug.LogError($"Cannot GetModuleHandle({module_base}). Try re-installing the application");
                return;
            }
            StringBuilder modPath = new StringBuilder(255);
            int rv = API_kernel.GetModuleFileName(hMod, modPath, 255);
            if (rv < 0)
            {
                UnityEngine.Debug.Log($"VRT.Transport.Dash.Loader: Cannot get filename for {module_base}, handle={hMod}, GetModuleFileName returned " + rv);
                UnityEngine.Debug.LogError($"Cannot get filename for {module_base} from handle. Try re-installing the application");
                return;
            }
            string dirName = Path.GetDirectoryName(modPath.ToString());
            dirName = dirName.Replace("\\", "/");
#else
            string dirName = VRT.NativeLibraries.VRTNativeLoader.platformLibrariesPath;
#endif
            UnityEngine.Debug.Log($"VRT.Transport.Dash.Loader: SIGNALS_SMD_PATH={dirName}");
            Environment.SetEnvironmentVariable("SIGNALS_SMD_PATH", dirName);
            didSetMSPaths = true;
        }
    }
}