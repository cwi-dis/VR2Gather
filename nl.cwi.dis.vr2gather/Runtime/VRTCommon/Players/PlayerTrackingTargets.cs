using UnityEngine;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Stores the player's VR tracking transforms in one place so that avatar
    /// representation prefabs (children of the player) can wire themselves without
    /// cross-prefab Inspector overrides. Also relays player-level events that
    /// representations need (ViewAdjusted).
    ///
    /// Place on the player prefab root and wire the fields in the Inspector.
    /// playerRoot is implicitly this.transform.
    /// </summary>
    public class PlayerTrackingTargets : MonoBehaviour
    {
        [Tooltip("Transform at head level (slightly below/behind HMD — RiggingAttachPointHead)")]
        public Transform head;
        [Tooltip("Transform at neck level (further below/behind HMD — RiggingAttachPointNeck)")]
        public Transform neck;
        [Tooltip("Transform at top of skull (slightly above HMD — RiggingAttachPointHeadTop). Used for height measurement.")]
        public Transform headTop;
        [Tooltip("Transform tracking the player's left hand/controller")]
        public Transform leftHand;
        [Tooltip("Transform tracking the player's right hand/controller")]
        public Transform rightHand;
        // Future: leftFoot, rightFoot, waist, ...

        /// <summary>
        /// Raised when the user has adjusted their view height (ViewAdjust), so representations
        /// can re-measure the player's height. PlayerRepresentationWirer subscribes SizeAdjust.
        /// </summary>
        public event System.Action ViewAdjusted;

        /// <summary>
        /// Wire ViewAdjust.viewAdjusted to this, in the player prefab.
        /// </summary>
        public void NotifyViewAdjusted()
        {
            ViewAdjusted?.Invoke();
        }
    }
}
