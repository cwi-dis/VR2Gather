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
        [Tooltip("Transform at neck level (further below/behind HMD — RiggingAttachPointNeck). Used for height measurement.")]
        public Transform neck;
        [Tooltip("Transform at top of skull (slightly above HMD — RiggingAttachPointHeadTop)")]
        public Transform headTop;
        [Tooltip("Transform tracking the player's left hand/controller")]
        public Transform leftHand;
        [Tooltip("Transform tracking the player's right hand/controller")]
        public Transform rightHand;
        // Future: leftFoot, rightFoot, waist, ...
        [Tooltip("The player's own hand models. Representations with hands of their own (avatars) hide these while active.")]
        public GameObject[] handModels;

        int m_HandModelsHidden;

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

        /// <summary>
        /// Hide (or stop hiding) the hand models. Counted, because during a representation
        /// switch the new representation can be enabled before the old one is disabled.
        /// </summary>
        public void HideHandModels(bool hide)
        {
            m_HandModelsHidden = Mathf.Max(0, m_HandModelsHidden + (hide ? 1 : -1));
            if (handModels == null) return;
            foreach (var handModel in handModels)
            {
                if (handModel != null) handModel.SetActive(m_HandModelsHidden == 0);
            }
        }
    }
}
