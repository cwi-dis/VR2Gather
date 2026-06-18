using UnityEngine;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Base component for avatar prefabs used as altRepOne/altRepTwo in VR2Gather.
    /// Place on the avatar prefab root alongside SyncSkeletonToVRRig and SizeAdjust.
    ///
    /// On activation (OnEnable) and on explicit Apply() calls, wires the avatar's
    /// tracking inputs (SyncSkeletonToVRRig vrTargets, SizeAdjust sources) from the
    /// PlayerTrackingTargets found in the parent player hierarchy.
    ///
    /// Subclass and override OnApply() to add app-specific setup such as skin/hair
    /// tinting. The avatar selection UI can call Apply() to re-apply after a change.
    /// </summary>
    public class PlayerRepresentationWirer : MonoBehaviour
    {
        void OnEnable() => Apply();

        public void Apply()
        {
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
                sizeAdjust.SourceTop = targets.headTop.gameObject;
                sizeAdjust.SourceBottom = targets.gameObject;
            }

            OnApply(targets);
        }

        /// <summary>
        /// Called after tracking wiring is complete. Override in subclasses to apply
        /// app-specific configuration (e.g. skin tone, hair colour).
        /// </summary>
        protected virtual void OnApply(PlayerTrackingTargets targets) { }
    }
}
