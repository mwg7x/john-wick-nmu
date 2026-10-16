using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastStep.Editor
{
    internal sealed class GreyboxAssets
    {
        internal readonly Material Stone, Edge, Metal, LampGlow, Skyline, Player, Enemy;
        internal readonly GameObject PillarPrefab, LampPrefab, PlayerPrefab, EnemyPrefab;

        internal GreyboxAssets()
        {
            Stone = Material("GB_Stone", new Color(0.43f, 0.46f, 0.49f));
            Edge = Material("GB_StoneEdge", new Color(0.57f, 0.59f, 0.61f));
            Metal = Material("GB_Metal", new Color(0.11f, 0.14f, 0.17f));
            LampGlow = Material("GB_Lamp", new Color(0.95f, 0.7f, 0.35f), true);
            Skyline = Material("GB_Skyline", new Color(0.19f, 0.24f, 0.30f));
            Player = Material("GB_Player", new Color(0.23f, 0.43f, 0.55f));
            Enemy = Material("GB_Enemy", new Color(0.52f, 0.28f, 0.21f));
            PillarPrefab = Prefab("Assets/Prefabs/Environment/GreyboxPillar.prefab", MakePillar);
            LampPrefab = Prefab("Assets/Prefabs/Environment/GreyboxLamp.prefab", MakeLamp);
            PlayerPrefab = Prefab("Assets/Prefabs/Characters/PlayerStandIn.prefab", () => MakeParticipant("PlayerStandIn", Player));
            EnemyPrefab = Prefab("Assets/Prefabs/Characters/EnemyStandIn.prefab", () => MakeParticipant("EnemyStandIn", Enemy));
        }

        private static Material Material(string name, Color color, bool emission = false)
        {
            string path = "Assets/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is missing. Resolve Phase 1 packages first.");
            material = new Material(shader) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.12f);
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 2f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Prefab(string path, Func<GameObject> factory)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;
            var temporary = factory();
            try { return PrefabUtility.SaveAsPrefabAsset(temporary, path); }
            finally { UnityEngine.Object.DestroyImmediate(temporary); }
        }

        internal static GameObject Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        internal static GameObject Primitive(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Material material, bool collider = true, bool isStatic = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            if (isStatic) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        internal static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool collider = true)
            => Primitive(name, PrimitiveType.Cube, parent, position, scale, material, collider);

        private GameObject MakePillar()
        {
            var root = new GameObject("GreyboxPillar");
            Box("Plinth", root.transform, new Vector3(0, 0.2f, 0), new Vector3(1.5f, 0.4f, 1.5f), Stone);
            Box("Shaft", root.transform, new Vector3(0, 1.45f, 0), new Vector3(0.8f, 2.1f, 0.8f), Edge);
            Box("Capital", root.transform, new Vector3(0, 2.65f, 0), new Vector3(1.15f, 0.3f, 1.15f), Stone);
            return root;
        }

        private GameObject MakeLamp()
        {
            var root = new GameObject("GreyboxLamp");
            Primitive("Foot", PrimitiveType.Cylinder, root.transform, new Vector3(0, 0.15f, 0), new Vector3(0.45f, 0.15f, 0.45f), Metal, false);
            Primitive("Post", PrimitiveType.Cylinder, root.transform, new Vector3(0, 1.45f, 0), new Vector3(0.12f, 1.3f, 0.12f), Metal, false);
            Box("Housing", root.transform, new Vector3(0, 2.85f, 0), new Vector3(0.48f, 0.62f, 0.48f), LampGlow, false);
            Box("Cap", root.transform, new Vector3(0, 3.22f, 0), new Vector3(0.66f, 0.12f, 0.66f), Metal, false);
            return root;
        }

        private static GameObject MakeParticipant(string name, Material material)
        {
            var root = new GameObject(name);
            var body = Primitive("Capsule", PrimitiveType.Capsule, root.transform, new Vector3(0, 0.92f, 0), new Vector3(0.7f, 0.9f, 0.7f), material, true, false);
            body.layer = 2; // Preview-only stand-ins are not future combat hit zones.
            return root;
        }

        internal static Transform Instance(GameObject prefab, Transform parent, Vector3 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = position;
            return go.transform;
        }
    }
}
