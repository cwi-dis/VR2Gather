using UnityEngine;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Each frame, casts a ray from the local player's head forward and notifies GazeTarget
    /// components when the player starts or stops looking at them.
    ///
    /// Head direction is a proxy for gaze: without eye tracking, people look around with their
    /// eyes, so give targets generous colliders. Only GazeTargets are tested (no physics layers
    /// involved), which also means gaze passes through walls and other objects.
    ///
    /// Place on the camera or head GameObject of the local player (P_Self_Player).
    /// </summary>
    public class GazeDetector : MonoBehaviour
    {
        [SerializeField, Tooltip("Transform to cast from. Defaults to Camera.main if unset.")]
        Transform m_GazeOrigin;

        [SerializeField, Tooltip("Maximum gaze detection distance in metres.")]
        float m_MaxDistance = 20f;

        GazeTarget m_CurrentTarget;

        void Start()
        {
            if (m_GazeOrigin == null)
                m_GazeOrigin = Camera.main?.transform;
            if (m_GazeOrigin == null)
                Debug.LogWarning($"GazeDetector({name}): No gaze origin set and Camera.main not found.");
        }

        void Update()
        {
            if (m_GazeOrigin == null) return;
            var targets = GazeTarget.Active;
            // Most scenes have no gaze targets: nothing to do, unless we still have to exit the last one
            if (targets.Count == 0 && m_CurrentTarget == null) return;

            var ray = new Ray(m_GazeOrigin.position, m_GazeOrigin.forward);
            GazeTarget newTarget = null;
            float nearest = float.MaxValue;
            // Indexed loop: foreach over an IReadOnlyList would allocate an enumerator every frame
            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target.Raycast(ray, m_MaxDistance, out float distance) && distance < nearest)
                {
                    nearest = distance;
                    newTarget = target;
                }
            }

            if (newTarget == m_CurrentTarget) return;

            // Unity-aware null checks: '?.' uses true C# null and ignores Unity's destroyed-object
            // state, so it would still call into a target destroyed during scene teardown (NRE).
            if (m_CurrentTarget != null) m_CurrentTarget.NotifyGazeExit();
            m_CurrentTarget = newTarget;
            if (m_CurrentTarget != null) m_CurrentTarget.NotifyGazeEnter();
        }

        void OnDisable()
        {
            if (m_CurrentTarget != null) m_CurrentTarget.NotifyGazeExit();
            m_CurrentTarget = null;
        }
    }
}
