using System;
using UnityEngine;

namespace LastStep.Weapons
{
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class RigHitZoneFollower : MonoBehaviour
    {
        private Transform[] zones;
        private Transform[] bones;
        private bool initialized;
        private static readonly string[] ZoneNames =
            { "Head", "Chest", "Torso", "LeftArm", "RightArm", "LeftLeg", "RightLeg" };
        private static readonly string[][] BoneNames =
        {
            new[] { "head" }, new[] { "spine_03", "chest" }, new[] { "spine_01", "spine" },
            new[] { "upperarm_l" }, new[] { "upperarm_r" }, new[] { "thigh_l", "upleg_l" },
            new[] { "thigh_r", "upleg_r" }
        };
        private static readonly HumanBodyBones[] HumanBones =
        {
            HumanBodyBones.Head, HumanBodyBones.Chest, HumanBodyBones.Spine,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg
        };
        private void LateUpdate()
        {
            if (!initialized) Initialize();
            if (zones == null) return;
            for (int i = 0; i < zones.Length; i++)
            {
                if (zones[i] == null || bones[i] == null) continue;
                zones[i].position = bones[i].position;
                zones[i].rotation = bones[i].rotation;
            }
        }
        private void Initialize()
        {
            var root = transform.Find("HitZones");
            var visual = transform.Find("VisualRig");
            if (root == null || visual == null || visual.childCount == 0) return;
            zones = new Transform[ZoneNames.Length];
            bones = new Transform[ZoneNames.Length];
            var animator = visual.GetComponentInChildren<Animator>();
            var skeleton = visual.GetComponentsInChildren<Transform>();
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = root.Find(ZoneNames[i]);
                if (animator != null && animator.isHuman) bones[i] = animator.GetBoneTransform(HumanBones[i]);
                if (bones[i] != null) continue;
                foreach (var candidate in skeleton)
                {
                    foreach (var name in BoneNames[i])
                        if (string.Equals(candidate.name, name, StringComparison.OrdinalIgnoreCase))
                        { bones[i] = candidate; break; }
                    if (bones[i] != null) break;
                }
            }
            initialized = true;
        }
    }
}
