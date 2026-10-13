using UnityEngine;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Base component for avatar prefabs used as altRepOne/altRepTwo in VR2Gather.
    /// Place on the avatar prefab root alongside SyncSkeletonToVRRig and SizeAdjust.
    ///
    /// On activation (OnEnable) and on explicit Apply() calls, wires the avatar's
    /// tracking inputs (SyncSkeletonToVRRig vrTargets, SizeAdjust sources) from the
    /// PlayerTrackingTargets found in the parent player hierarchy, and subscribes
    /// SizeAdjust.AdjustHeight to its ViewAdjusted event (unsubscribed in OnDisable).
    /// Optionally hides the player's own hand models while active, because the avatar
    /// has hands of its own.
    /// So the player prefab needs no references into the representation.
    ///
    /// Subclass and override OnApply() to add app-specific setup such as skin/hair
    /// tinting. The avatar selection UI can call Apply() to re-apply after a change.
    /// </summary>
    public class PlayerRepresentationWirer : MonoBehaviour
    {
        [Tooltip("Hide the player's own hand models while this representation is active (it has hands of its own). Switch off to see both, e.g. to check hand alignment.")]
        [SerializeField] bool hideHandModels = true;

        PlayerTrackingTargets m_SubscribedTargets;
        SizeAdjust m_SubscribedSizeAdjust;
        PlayerTrackingTargets m_HidingHandModelsOf;

        void OnEnable() => Apply();

        void OnDisable() => Unsubscribe();

        public void Apply()
        {
            Unsubscribe();
            var targets = GetComponentInParent<PlayerTrackingTargets>();
            if (targets == null)
            {
                Debug.LogWarning($"{name}: PlayerRepresentationWirer: no PlayerTrackingTargets found in parent hierarchy");
                return;
            }

            var sync = GetComponentInChildren<SyncSkeletonToVRRig>();
            if (sync != null)
            {
                sync.head.vrTarget = targets.head;
                sync.neck.vrTarget = targets.neck;
                sync.leftHand.vrTarget = targets.leftHand;
                sync.rightHand.vrTarget = targets.rightHand;
                sync.mannequinTransform = transform;
            }

            var sizeAdjust = GetComponentInChildren<SizeAdjust>();
            if (sizeAdjust != null)
            {
                // Measure at the neck: that's where SyncSkeletonToVRRig anchors the avatar, and it
                // hardly moves when the user bends their head (#356).
                sizeAdjust.SourceTop = targets.neck.gameObject;
                sizeAdjust.SourceBottom = targets.gameObject;
                targets.ViewAdjusted += sizeAdjust.AdjustHeight;
                m_SubscribedTargets = targets;
                m_SubscribedSizeAdjust = sizeAdjust;
            }

            if (hideHandModels)
            {
                targets.HideHandModels(true);
                m_HidingHandModelsOf = targets;
            }

            OnApply(targets);
        }

        void Unsubscribe()
        {
            if (m_SubscribedTargets != null && m_SubscribedSizeAdjust != null)
            {
                m_SubscribedTargets.ViewAdjusted -= m_SubscribedSizeAdjust.AdjustHeight;
            }
            m_SubscribedTargets = null;
            m_SubscribedSizeAdjust = null;
            if (m_HidingHandModelsOf != null)
            {
                m_HidingHandModelsOf.HideHandModels(false);
                m_HidingHandModelsOf = null;
            }
        }

        /// <summary>
        /// Called after tracking wiring is complete. Override in subclasses to apply
        /// app-specific configuration (e.g. skin tone, hair colour).
        /// </summary>
        protected virtual void OnApply(PlayerTrackingTargets targets) { }
    }
}
