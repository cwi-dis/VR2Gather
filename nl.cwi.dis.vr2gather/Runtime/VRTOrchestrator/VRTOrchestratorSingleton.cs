using System;

namespace VRT.Orchestrator
{
    /// <summary>
    /// Static service locator for the orchestrator singleton.
    ///
    /// Use the typed properties to access only the sub-interface your code needs:
    /// - Login  — session creation, login/logout, connecting to the orchestrator
    /// - Comm   — within-session messaging and events between participants
    /// - Streams — binary data streams (SocketIO transport)
    ///
    /// The full IVRTOrchestrator interface is available via Instance but is marked
    /// obsolete — prefer the narrower sub-interfaces.
    /// </summary>
    public static class VRTOrchestratorSingleton
    {
        private static IVRTOrchestrator _instance;

        /// <summary>
        /// Register the orchestrator implementation. Called by OrchestratorController.Awake().
        /// </summary>
        public static void Register(IVRTOrchestrator impl)
        {
            _instance = impl;
        }

        /// <summary>
        /// Unregister the orchestrator implementation. Called by OrchestratorController.OnDestroy().
        /// Only clears the registration if impl is the currently registered instance.
        /// </summary>
        public static void Unregister(IVRTOrchestrator impl)
        {
            if (_instance == impl) _instance = null;
        }

        /// <summary>Orchestrator interface for session creation, login, and join/leave.</summary>
        public static IVRTOrchestratorLogin Login => _instance;

        /// <summary>Orchestrator interface for within-session event messaging.</summary>
        public static IVRTOrchestratorComm Comm => _instance;

        /// <summary>Orchestrator interface for binary data streams.</summary>
        public static IVRTOrchestratorDataStream Streams => _instance;

        /// <summary>
        /// Full orchestrator interface. Prefer Login, Comm, or Streams instead.
        /// </summary>
        [Obsolete("Use VRTOrchestratorSingleton.Login, .Comm, or .Streams instead of the full interface.")]
        public static IVRTOrchestrator Instance => _instance;

        /// <summary>Convert a DateTime to a Unix timestamp (seconds since epoch).</summary>
        public static double GetClockTimestamp(DateTime pDate)
        {
            return pDate.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}
