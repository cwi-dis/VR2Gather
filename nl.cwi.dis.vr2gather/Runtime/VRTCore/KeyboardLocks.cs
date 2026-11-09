using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRT.Core
{
    /// <summary>
    /// State of the keyboard's lock keys (the lamps), asked from the operating system.
    ///
    /// The Input System only reports whether a key is held down, not whether Caps Lock is on, and only while
    /// Unity has focus; with a headset on, the Oculus/Link app usually has it. The operating system knows the
    /// toggle state regardless of focus (VR2Gather#376).
    /// </summary>
    public static class KeyboardLocks
    {
#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);
#elif UNITY_EDITOR_OSX || (UNITY_STANDALONE_OSX && !UNITY_EDITOR)
        [System.Runtime.InteropServices.DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern ulong CGEventSourceFlagsState(int stateID);
#endif

        /// <summary>
        /// True if Caps Lock is on. On Windows and macOS this is the toggle state; elsewhere (Linux, Android)
        /// it falls back to the Caps Lock key being held down, through the Input System.
        /// </summary>
        public static bool CapsLockOn()
        {
            try
            {
#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
                if ((GetKeyState(0x14) & 1) != 0) return true; // VK_CAPITAL; low bit: toggled on
#elif UNITY_EDITOR_OSX || (UNITY_STANDALONE_OSX && !UNITY_EDITOR)
                if ((CGEventSourceFlagsState(0) & 0x10000UL) != 0) return true; // combined session state; alpha shift (Caps Lock)
#endif
            }
            catch (Exception e)
            {
                Debug.LogWarning($"KeyboardLocks: cannot read Caps Lock state: {e.Message}");
            }
            return Keyboard.current != null && Keyboard.current.capsLockKey.isPressed;
        }
    }
}
