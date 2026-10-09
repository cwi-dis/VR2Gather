using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if VRT_WITH_STATS
using Statistics = Cwipc.Statistics;
#endif

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Marks a GameObject as a gaze target for GazeDetector. The target is "looked at" when the
    /// gaze ray hits any collider on this GameObject or its children (triggers included, layers
    /// ignored), so the size of the colliders determines how forgiving gaze detection is.
    ///
    /// Logs stats records when the local player starts and stops looking at this object, and its
    /// world position at start and whenever it changes (for distance computations).
    /// Wire OnGazeEnter / OnGazeExit for additional behaviour (e.g. a NetworkTrigger).
    ///
    /// Also logs a closing gazing=0 (and final position) on OnDisable, so a gaze span never
    /// survives this object being disabled or destroyed (e.g. its scene unloading). See
    /// VRTApp-Trolley#94.
    /// </summary>
    public class GazeTarget : MonoBehaviour
    {
        static readonly List<GazeTarget> s_Active = new();

        /// <summary>
        /// All currently enabled gaze targets.
        /// </summary>
        public static IReadOnlyList<GazeTarget> Active => s_Active;

        [SerializeField, Tooltip("Fired when the local player starts gazing at this object.")]
        UnityEvent m_OnGazeEnter;

        [SerializeField, Tooltip("Fired when the local player stops gazing at this object.")]
        UnityEvent m_OnGazeExit;

        [SerializeField, Tooltip("How often to check and log position changes, in milliseconds. 0 disables periodic logging.")]
        float m_PositionLogIntervalMs = 200f;

        Collider[] m_Colliders;
        Vector3 m_LastLoggedPosition;
        float m_TimeSinceLastLog;

        void Awake()
        {
            m_Colliders = GetComponentsInChildren<Collider>(true);
            if (m_Colliders.Length == 0)
                Debug.LogWarning($"GazeTarget({name}): no colliders, will never be gazed at");
        }

        void OnEnable()
        {
            s_Active.Add(this);
        }

        void Start()
        {
            m_LastLoggedPosition = transform.position;
            m_TimeSinceLastLog = 0f;
#if VRT_WITH_STATS
            Statistics.Output("GazeTarget", $"gazing=0, target={gameObject.name}");
            LogPosition();
#endif
        }

        void OnDisable()
        {
            s_Active.Remove(this);
#if VRT_WITH_STATS
            Statistics.Output("GazeTarget", $"gazing=0, target={gameObject.name}");
            LogPosition();
#endif
        }

        void Update()
        {
            if (m_PositionLogIntervalMs <= 0f) return;
            m_TimeSinceLastLog += Time.deltaTime * 1000f;
            if (m_TimeSinceLastLog < m_PositionLogIntervalMs) return;
            m_TimeSinceLastLog = 0f;
            Vector3 pos = transform.position;
            if (pos == m_LastLoggedPosition) return;
            m_LastLoggedPosition = pos;
#if VRT_WITH_STATS
            LogPosition();
#endif
        }

#if VRT_WITH_STATS
        void LogPosition()
        {
            Vector3 pos = transform.position;
            Statistics.Output("GazeTarget", $"pos_x={pos.x:F2}, pos_y={pos.y:F2}, pos_z={pos.z:F2}, target={gameObject.name}");
        }
#endif

        /// <summary>
        /// Test the gaze ray against this target's colliders. Returns true (and the distance to the
        /// nearest hit) if any enabled collider is hit within maxDistance.
        /// </summary>
        public bool Raycast(Ray ray, float maxDistance, out float distance)
        {
            distance = float.MaxValue;
            bool hit = false;
            foreach (var c in m_Colliders)
            {
                if (c == null || !c.enabled) continue;
                if (c.Raycast(ray, out RaycastHit h, maxDistance) && h.distance < distance)
                {
                    distance = h.distance;
                    hit = true;
                }
            }
            return hit;
        }

        public void NotifyGazeEnter()
        {
#if VRT_WITH_STATS
            Statistics.Output("GazeTarget", $"gazing=1, target={gameObject.name}");
#endif
            m_OnGazeEnter?.Invoke();
        }

        public void NotifyGazeExit()
        {
#if VRT_WITH_STATS
            Statistics.Output("GazeTarget", $"gazing=0, target={gameObject.name}");
#endif
            m_OnGazeExit?.Invoke();
        }
    }
}
