namespace VRT.Orchestrator.Wrapping
{
    /// <summary>
    /// Combined orchestrator interface. Extends both IVRTOrchestratorLogin and
    /// IVRTOrchestratorComm (which both extend IVRTOrchestratorSessionState).
    /// OrchestratorController implements this interface.
    /// Code that only needs a subset of functionality should prefer the more
    /// specific sub-interface.
    /// </summary>
    public interface IVRTOrchestrator : IVRTOrchestratorLogin, IVRTOrchestratorComm
    {
    }
}
