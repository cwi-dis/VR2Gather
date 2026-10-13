using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace VRT.Pilots.Common
{
    /// <summary>
    /// Turns a Humanoid model into a VR2Gather avatar representation, with the same structure
    /// as P_Mannequin. Expected hierarchy:
    ///
    ///   wrapper (the representation: SizeAdjust, PlayerRepresentationWirer)
    ///     └─ model (the Humanoid FBX, with the Animator)
    ///
    /// Bones are found through the Humanoid mapping (Animator.GetBoneTransform), so any Humanoid
    /// rig works, whatever its bone names. All references are stored in the components, visible
    /// in the Inspector. The tracking inputs (vrTargets, SizeAdjust sources) are left empty:
    /// PlayerRepresentationWirer fills them at runtime from the player.
    ///
    /// This has no Editor dependencies, so it could also set up avatars loaded at runtime (#358).
    /// At runtime, call it on an inactive wrapper and activate it afterwards, so the components'
    /// Awake() runs after their fields are set. The Editor menu item is in HumanoidAvatarSetupTool.
    /// </summary>
    public static class HumanoidAvatarSetup
    {
        public const string VRConstraintsName = "VR Constraints";
        public const string SyncName = "SyncSkeletonToVRRig";

        static readonly HumanBodyBones[] RequiredBones =
        {
            HumanBodyBones.Hips, HumanBodyBones.Neck, HumanBodyBones.Head,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
        };

        // Hint (pole vector) offsets from the elbow/knee: elbows bend back and down, knees forward.
        static readonly Vector3 ArmHintOffset = Vector3.back * 0.7f + Vector3.down * 0.4f;
        static readonly Vector3 LegHintOffset = Vector3.forward * 0.3f;

        /// <summary>
        /// Set up (or set up again: a previous setup is removed first) the avatar under wrapper.
        /// Returns null on success, or an error message.
        /// </summary>
        public static string Setup(GameObject wrapper)
        {
            var animator = wrapper.GetComponentInChildren<Animator>(true);
            if (animator == null)
                return $"No Animator found under '{wrapper.name}'. Put the Humanoid model (FBX) as a child of the wrapper.";
            if (animator.gameObject == wrapper)
                return $"The Animator is on '{wrapper.name}' itself. Create an empty wrapper, put the model in it as a child, and run this on the wrapper.";
            if (!animator.isHuman)
                return $"'{animator.name}' is not Humanoid. In the model's import settings, set Rig > Animation Type to Humanoid.";
            var missing = new List<string>();
            foreach (var bone in RequiredBones)
            {
                if (animator.GetBoneTransform(bone) == null) missing.Add(bone.ToString());
            }
            if (missing.Count > 0)
                return $"'{animator.name}' has no {string.Join(", ", missing)} in its Humanoid mapping. Check Rig > Configure in the import settings.";
            Transform Bone(HumanBodyBones bone) => animator.GetBoneTransform(bone);
            var model = animator.gameObject;

            RemovePreviousSetup(wrapper, model);

            // The Animator must not move the model around: SyncSkeletonToVRRig positions it.
            // SyncSkeletonToVRRig.neck moves the whole model (the Animator's own transform), as in
            // P_Mannequin. A Humanoid Animator resets the hips every frame, but relative to the
            // model, so they come along. (Trolley moved only the hips, with a Body Constraint;
            // then the feet stay behind at the player root and the legs slant.)
            animator.applyRootMotion = false;

            // Rig with the constraints, under the model
            var rigBuilder = model.GetComponent<RigBuilder>();
            if (rigBuilder == null) rigBuilder = model.AddComponent<RigBuilder>();
            var vrConstraints = NewChild(model.transform, VRConstraintsName);
            var rig = vrConstraints.gameObject.AddComponent<Rig>();
            rig.weight = 1;

            // Proxy for the head direction (used to turn the body). Not the head bone itself:
            // SyncSkeletonToVRRig maps the head before the neck, and the neck mapping moves the
            // whole model, so the head would be moved twice.
            var headTarget = NewChild(vrConstraints, "Head Constraint");
            headTarget.SetPositionAndRotation(Bone(HumanBodyBones.Head).position, Bone(HumanBodyBones.Head).rotation);

            var leftArmTarget = AddTwoBoneIK(vrConstraints, "Left Arm IK",
                Bone(HumanBodyBones.LeftUpperArm), Bone(HumanBodyBones.LeftLowerArm), Bone(HumanBodyBones.LeftHand), ArmHintOffset);
            var rightArmTarget = AddTwoBoneIK(vrConstraints, "Right Arm IK",
                Bone(HumanBodyBones.RightUpperArm), Bone(HumanBodyBones.RightLowerArm), Bone(HumanBodyBones.RightHand), ArmHintOffset);
            var leftLegTarget = AddTwoBoneIK(vrConstraints, "Left Leg IK",
                Bone(HumanBodyBones.LeftUpperLeg), Bone(HumanBodyBones.LeftLowerLeg), Bone(HumanBodyBones.LeftFoot), LegHintOffset);
            var rightLegTarget = AddTwoBoneIK(vrConstraints, "Right Leg IK",
                Bone(HumanBodyBones.RightUpperLeg), Bone(HumanBodyBones.RightLowerLeg), Bone(HumanBodyBones.RightFoot), LegHintOffset);
            // Feet stay on the floor (the wrapper's origin), without physics
            leftLegTarget.gameObject.AddComponent<KeepFeetAboveGround>().RootObject = wrapper;
            rightLegTarget.gameObject.AddComponent<KeepFeetAboveGround>().RootObject = wrapper;

            rigBuilder.layers.Add(new RigLayer(rig, true));

            // SyncSkeletonToVRRig, as a child of the wrapper. The rigTargets must be the very
            // transforms the constraints read: the animation stream only picks up objects the
            // constraints reference directly.
            var sync = NewChild(wrapper.transform, SyncName).gameObject.AddComponent<SyncSkeletonToVRRig>();
            sync.head = new SyncSkeletonToVRRig.VRMap { rigTarget = headTarget };
            sync.neck = new SyncSkeletonToVRRig.VRMap
            {
                rigTarget = model.transform,
                rigSource = Bone(HumanBodyBones.Neck),
                positionOnly = true,
                includeYRotation = true,
            };
            sync.leftHand = new SyncSkeletonToVRRig.VRMap { rigTarget = leftArmTarget };
            sync.rightHand = new SyncSkeletonToVRRig.VRMap { rigTarget = rightArmTarget };
            sync.mannequinTransform = model.transform;

            // SizeAdjust measures from the model's feet (the model root) to the neck, where the
            // avatar is anchored (#356). Like P_Mannequin it doesn't react to HMD tracking by
            // itself: a self player switches that on (otherwise remote copies would resize too).
            var sizeAdjust = wrapper.GetComponent<SizeAdjust>();
            if (sizeAdjust == null) sizeAdjust = wrapper.AddComponent<SizeAdjust>();
            sizeAdjust.Destination = wrapper;
            sizeAdjust.DestinationTop = Bone(HumanBodyBones.Neck).gameObject;
            sizeAdjust.DestinationBottom = model;
            sizeAdjust.setHeightOnStart = false;
            sizeAdjust.setHeightOnHMDTracking = false;

            if (wrapper.GetComponent<PlayerRepresentationWirer>() == null) wrapper.AddComponent<PlayerRepresentationWirer>();

            if (Application.isPlaying) rigBuilder.Build();
            return null;
        }

        static void RemovePreviousSetup(GameObject wrapper, GameObject model)
        {
            var sync = wrapper.GetComponentInChildren<SyncSkeletonToVRRig>(true);
            if (sync != null)
            {
                if (sync.gameObject == wrapper || sync.gameObject == model) Object.DestroyImmediate(sync);
                else Object.DestroyImmediate(sync.gameObject);
            }
            var vrConstraints = model.transform.Find(VRConstraintsName);
            if (vrConstraints != null) Object.DestroyImmediate(vrConstraints.gameObject);
            var rigBuilder = model.GetComponent<RigBuilder>();
            if (rigBuilder != null) Object.DestroyImmediate(rigBuilder);
        }

        static Transform NewChild(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        /// <summary>
        /// Two Bone IK under parent, with its target at the tip bone and the hint at the mid bone
        /// plus hintOffset. Returns the target: that's what SyncSkeletonToVRRig must move.
        /// </summary>
        static Transform AddTwoBoneIK(Transform parent, string label, Transform root, Transform mid, Transform tip, Vector3 hintOffset)
        {
            var ikObject = NewChild(parent, label);
            var target = NewChild(ikObject, label + " Target");
            target.SetPositionAndRotation(tip.position, tip.rotation);
            var hint = NewChild(ikObject, label + " Hint");
            hint.position = mid.position + hintOffset;

            var ik = ikObject.gameObject.AddComponent<TwoBoneIKConstraint>();
            var data = ik.data;
            data.root = root;
            data.mid = mid;
            data.tip = tip;
            data.target = target;
            data.hint = hint;
            data.targetPositionWeight = 1;
            data.targetRotationWeight = 1;
            data.hintWeight = 1;
            data.maintainTargetPositionOffset = false;
            data.maintainTargetRotationOffset = false;
            ik.data = data;
            ik.weight = 1;
            return target;
        }
    }
}
