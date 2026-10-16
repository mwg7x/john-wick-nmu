using System;
using System.Collections.Generic;
using System.Linq;
using LastStep.Core;
using LastStep.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastStep.Editor
{
    public static class Phase2Setup
    {
        public const string ScenePath = "Assets/Scenes/Duel_Staircase.unity";
        public const string RootName = "StaircaseGreybox";

        [MenuItem("LAST STEP/Phase 2/Create Missing Greybox")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before building the greybox.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (scene.GetRootGameObjects().Any(go => go.name == RootName))
            {
                Debug.Log("LAST STEP: existing greybox preserved. Use its Inspector/Scene view to edit it.");
                return;
            }
            var duel = AssetDatabase.LoadAssetAtPath<DuelSettings>("Assets/ScriptableObjects/DuelSettings.asset");
            if (duel == null) throw new InvalidOperationException("Phase 1 DuelSettings is missing.");
            const string configPath = "Assets/Settings/StaircaseGreyboxSettings.asset";
            var config = AssetDatabase.LoadAssetAtPath<StaircaseGreyboxSettings>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<StaircaseGreyboxSettings>();
                AssetDatabase.CreateAsset(config, configPath);
            }
            var stations = Stations(duel, config);
            for (int i = 1; i < stations.Count; i++)
                if (stations[i].z - stations[i - 1].z < config.LandingLength + 1f)
                    throw new InvalidOperationException("Duel distances are too close for the configured landings. Reduce Landing Length or separate distances.");

            var assets = new GreyboxAssets();
            var root = new GameObject(RootName);
            BuildStairs(root.transform, stations, config, assets);
            BuildBoundaries(root.transform, stations, config, assets);
            BuildSkyline(root.transform, config, assets);
            var markers = BuildMarkers(root.transform, duel, config, assets);
            BuildLighting(root.transform);
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Duel scene Main Camera is missing.");
            PositionPreview(markers, camera, 0);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 400f;
            camera.fieldOfView = 58f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.11f);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.48f, 0.58f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.32f, 0.39f);
            RenderSettings.ambientGroundColor = new Color(0.17f, 0.19f, 0.23f);
            RenderSettings.fog = false;
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, "Assets/Prefabs/Environment/StaircaseGreybox.prefab", InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("LAST STEP PHASE 2: staircase, landings, rails, walls, lamps, pillars, skyline and exact-distance markers created.");
        }

        private static float CenterHeight(DuelSettings duel, StaircaseGreyboxSettings config)
            => 1f + (duel.GetDistance(0) * 0.5f + config.ExtensionLandings * config.ExtensionSpacing * 0.5f) * config.VerticalFraction;

        private static Vector3 Station(float signedDistance, float centerHeight, float fraction)
            => new Vector3(0, centerHeight - signedDistance * fraction, signedDistance * Mathf.Sqrt(1f - fraction * fraction));

        private static List<Vector3> Stations(DuelSettings duel, StaircaseGreyboxSettings config)
        {
            var offsets = new SortedSet<float> { 0f };
            for (int i = 0; i < duel.StageCount; i++)
            { offsets.Add(-duel.GetDistance(i) * 0.5f); offsets.Add(duel.GetDistance(i) * 0.5f); }
            for (int i = 1; i <= config.ExtensionLandings; i++)
            {
                float offset = (duel.GetDistance(0) + i * config.ExtensionSpacing) * 0.5f;
                offsets.Add(-offset); offsets.Add(offset);
            }
            return offsets.Select(d => Station(d, CenterHeight(duel, config), config.VerticalFraction)).ToList();
        }

        private static void BuildStairs(Transform root, List<Vector3> stations, StaircaseGreyboxSettings config, GreyboxAssets assets)
        {
            var ground = GreyboxAssets.Group("StairsAndLandings", root).transform;
            for (int i = 0; i < stations.Count; i++)
            {
                var point = stations[i];
                GreyboxAssets.Box("Landing_" + i.ToString("00"), ground, new Vector3(0, point.y * 0.5f, point.z),
                    new Vector3(config.StairWidth + 5f, point.y, config.LandingLength), assets.Stone);
                if (i == stations.Count - 1) continue;
                var next = stations[i + 1];
                int steps = Mathf.CeilToInt((point.y - next.y) / config.MaximumStepRise);
                float start = point.z + config.LandingLength * 0.5f;
                float length = next.z - config.LandingLength * 0.5f - start;
                float tread = length / steps;
                for (int j = 0; j < steps; j++)
                {
                    float top = Mathf.Lerp(point.y, next.y, (j + 1f) / steps);
                    GreyboxAssets.Box("Flight_" + i.ToString("00") + "_Step_" + j.ToString("00"), ground,
                        new Vector3(0, top * 0.5f, start + (j + 0.5f) * tread), new Vector3(config.StairWidth, top, tread + 0.002f), assets.Stone);
                    GreyboxAssets.Box("Nosing_" + i + "_" + j, ground,
                        new Vector3(0, top + 0.005f, start + (j + 1f) * tread - 0.03f), new Vector3(config.StairWidth, 0.012f, 0.06f), assets.Edge, false);
                }
            }
        }

        private static void BuildBoundaries(Transform root, List<Vector3> stations, StaircaseGreyboxSettings config, GreyboxAssets assets)
        {
            var walls = GreyboxAssets.Group("WallsAndRailings", root).transform;
            var props = GreyboxAssets.Group("LampsAndPillars", root).transform;
            for (int i = 0; i < stations.Count; i++)
            {
                foreach (int side in new[] { -1, 1 })
                {
                    Vector3 point = stations[i] + Vector3.right * side * (config.StairWidth * 0.5f + 1.3f);
                    if (i % 2 == 0) GreyboxAssets.Instance(assets.PillarPrefab, props, point);
                    else GreyboxAssets.Instance(assets.LampPrefab, props, point);
                    float edge = side * (config.StairWidth * 0.5f + 0.22f);
                    GreyboxAssets.Box("LandingWall_" + i + "_" + side, walls,
                        new Vector3(edge, stations[i].y + 0.5f, stations[i].z), new Vector3(0.4f, 1f, config.LandingLength), assets.Stone);
                    if (i == stations.Count - 1) continue;
                    Vector3 a = stations[i] + Vector3.forward * (config.LandingLength * 0.5f);
                    Vector3 b = stations[i + 1] - Vector3.forward * (config.LandingLength * 0.5f);
                    Vector3 direction = b - a;
                    var wall = GreyboxAssets.Box("FlightWall_" + i + "_" + side, walls,
                        (a + b) * 0.5f + new Vector3(edge, 0.3f, 0), new Vector3(0.4f, 0.65f, direction.magnitude + 0.1f), assets.Stone);
                    wall.transform.localRotation = Quaternion.LookRotation(direction);
                    var rail = GreyboxAssets.Box("Handrail_" + i + "_" + side, walls,
                        (a + b) * 0.5f + new Vector3(edge, 1.05f, 0), new Vector3(0.13f, 0.12f, direction.magnitude + 0.1f), assets.Metal, false);
                    rail.transform.localRotation = Quaternion.LookRotation(direction);
                    for (int post = 0; post <= 4; post++)
                        GreyboxAssets.Box("RailPost_" + i + "_" + side + "_" + post, walls,
                            Vector3.Lerp(a, b, post / 4f) + new Vector3(edge, 0.75f, 0), new Vector3(0.07f, 0.6f, 0.07f), assets.Metal, false);
                }
            }
        }

        private static DuelPositionMarkers BuildMarkers(Transform root, DuelSettings duel, StaircaseGreyboxSettings config, GreyboxAssets assets)
        {
            var group = GreyboxAssets.Group("DuelPositions", root);
            var pairs = new DuelMarkerPair[duel.StageCount];
            float height = CenterHeight(duel, config);
            for (int i = 0; i < pairs.Length; i++)
            {
                float distance = duel.GetDistance(i);
                var player = GreyboxAssets.Group("PlayerPosition_" + distance.ToString("0.##"), group.transform).transform;
                var enemy = GreyboxAssets.Group("EnemyPosition_" + distance.ToString("0.##"), group.transform).transform;
                player.localPosition = Station(-distance * 0.5f, height, config.VerticalFraction);
                enemy.localPosition = Station(distance * 0.5f, height, config.VerticalFraction);
                enemy.localRotation = Quaternion.Euler(0, 180, 0);
                pairs[i] = new DuelMarkerPair(distance, player, enemy);
            }
            var standIns = GreyboxAssets.Group("ScaleStandIns", root).transform;
            var playerBody = GreyboxAssets.Instance(assets.PlayerPrefab, standIns, pairs[0].Player.position);
            var enemyBody = GreyboxAssets.Instance(assets.EnemyPrefab, standIns, pairs[0].Enemy.position);
            enemyBody.rotation = pairs[0].Enemy.rotation;
            var component = group.AddComponent<DuelPositionMarkers>();
            component.Configure(duel, pairs, playerBody, enemyBody);
            return component;
        }

        private static void BuildSkyline(Transform root, StaircaseGreyboxSettings config, GreyboxAssets assets)
        {
            var skyline = GreyboxAssets.Group("DistantCityGreybox", root).transform;
            var random = new System.Random(config.SkylineSeed);
            for (int row = 0; row < 3; row++)
                for (int i = 0; i < 13; i++)
                {
                    float height = 5f + (float)random.NextDouble() * 14f;
                    var block = GreyboxAssets.Box("CityMass_" + row + "_" + i, skyline,
                        new Vector3((i - 6) * 12f + row * 3f, height * 0.5f - 12f, 75f + row * 30f),
                        new Vector3(6f + (float)random.NextDouble() * 5f, height, 8f), assets.Skyline, false);
                    block.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
        }

        private static void BuildLighting(Transform root)
        {
            var lighting = GreyboxAssets.Group("GreyboxLighting", root).transform;
            var moon = GreyboxAssets.Group("MoonKey", lighting).AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.66f, 0.76f, 1f);
            moon.intensity = 1.05f;
            moon.shadows = LightShadows.Soft;
            moon.transform.rotation = Quaternion.Euler(38, -32, 0);
            RenderSettings.sun = moon;
            var lamps = root.GetComponentsInChildren<Transform>().Where(t => t.name == "GreyboxLamp").ToArray();
            foreach (var lamp in lamps.Where((t, i) => i == 2 || i == 7))
            {
                var light = GreyboxAssets.Group("WarmFill", lamp).AddComponent<Light>();
                light.transform.localPosition = new Vector3(0, 2.75f, 0);
                light.type = LightType.Point;
                light.color = new Color(1f, 0.65f, 0.32f);
                light.intensity = 3f;
                light.range = 9f;
                light.shadows = LightShadows.None;
            }
        }

        public static void PositionPreview(DuelPositionMarkers markers, Camera camera, int stage)
        {
            if (markers == null || camera == null || !markers.TryGetStage(stage, out var pair))
                throw new InvalidOperationException("Preview requires valid camera and marker references.");
            markers.PlayerStandIn.SetPositionAndRotation(pair.Player.position, pair.Player.rotation);
            markers.EnemyStandIn.SetPositionAndRotation(pair.Enemy.position, pair.Enemy.rotation);
            camera.transform.position = pair.Player.position + new Vector3(1.55f, 2.15f, -3f);
            camera.transform.LookAt(pair.Enemy.position + Vector3.up * 1.15f);
        }
    }
}
