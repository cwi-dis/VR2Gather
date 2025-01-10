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
   
    public class Loader
    {
        private static bool didSetMSPaths = false;

        public static void PreLoadModule(string module_base)
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            string libPath = Path.Combine(VRT.NativeLibraries.VRTNativeLoader.platformLibrariesPath, module_base);
            UnityEngine.Debug.Log($"VRT.Transport.Dash.Loader: will call LoadLibrary({libPath})");
            IntPtr rv = API_kernel.LoadLibrary(libPath);
            UnityEngine.Debug.Log($"VRT.Transport.Dash.Loader: LoadLibrary({libPath}) returned {rv}");
#endif
        } 

        public static void PostLoadModule(string module_base)
        {
            // xxxjack if (didSetMSPaths) return;
            
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

            UnityEngine.Debug.Log($"VRT.Transport.Dash.Loader: SIGNALS_SMD_PATH={dirName}");
            Environment.SetEnvironmentVariable("SIGNALS_SMD_PATH", dirName);
            didSetMSPaths = true;
        }
    }
}