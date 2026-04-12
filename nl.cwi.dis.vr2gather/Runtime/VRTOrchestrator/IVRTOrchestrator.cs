namespace VRT.Orchestrator
{
    /// <summary>
    /// Combined orchestrator interface. Extends IVRTOrchestratorLogin,
    /// IVRTOrchestratorComm, and IVRTOrchestratorDataStream (which all extend
    /// IVRTOrchestratorSessionState). OrchestratorController implements this interface.
    /// Code that only needs a subset of functionality should prefer the more
    /// specific sub-interface.
    /// </summary>
    public interface IVRTOrchestrator : IVRTOrchestratorLogin, IVRTOrchestratorComm, IVRTOrchestratorDataStream
    {
    }
}
