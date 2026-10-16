using System;
using System.IO;
using System.Linq;
using LastStep.AI;
using LastStep.Audio;
using LastStep.Core;
using LastStep.Player;
using LastStep.UI;
using LastStep.Utilities;
using LastStep.VR;
using LastStep.Weapons;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.XR.CoreUtils;

namespace LastStep.Editor
{
    public static class PlayableSetup
    {
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        [MenuItem("LAST STEP/Playable/Configure PC Duel and Menu")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before setup.");
            if (!Phase4Setup.EnsureResources(false)) return;
            MotionPolishSetup.Run();
            ConfigureRosterDifficulties();
            CreatePortraits();
            PortraitBake.Ensure();
            BuildDuel();
            BuildMenu();
            ConfigureBootDifficulties();
            EditorSceneManager.OpenScene(Phase2Setup.ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("LAST STEP PLAYABLE: PC camera, movement, hit zones, shooting, AI, rounds, HUD and menu configured.");
        }

        private static void BuildDuel()
        {
            Phase4Setup.Configure();
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Duel_Staircase") throw new InvalidOperationException("Duel scene was not opened.");
            var duel = UnityEngine.Object.FindAnyObjectByType<DuelManager>();
            var markers = UnityEngine.Object.FindAnyObjectByType<DuelPositionMarkers>();
            var camera = Camera.main;
            if (duel == null || markers == null || camera == null || markers.PlayerStandIn == null || markers.EnemyStandIn == null)
                throw new InvalidOperationException("Phase 2/3 scene references missing.");
            var player = markers.PlayerStandIn.gameObject;
            var enemy = markers.EnemyStandIn.gameObject;
            var playerHealth = GetOrAdd<Health>(player);
            var enemyHealth = GetOrAdd<Health>(enemy);
            MakeHitZones(player.transform, playerHealth);
            MakeHitZones(enemy.transform, enemyHealth);
            GetOrAdd<RigHitZoneFollower>(player);
            GetOrAdd<RigHitZoneFollower>(enemy);
            var capsule = player.GetComponent<CapsuleCollider>();
            if (capsule != null) UnityEngine.Object.DestroyImmediate(capsule);
            var body = GetOrAdd<CharacterController>(player);
            body.height = 1.8f; body.radius = 0.28f; body.center = Vector3.up * 0.9f; body.stepOffset = 0.25f;

            var data = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/ScriptableObjects/Pistol.asset");
            if (data != null) CinematicSetup.EnsureWeaponChoices(data);
            var inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/LastStep.inputactions");
            if (data == null || inputAsset == null) throw new InvalidOperationException("Weapon or input asset missing.");
            var falseStarts = GetOrAdd<FalseStartManager>(duel.gameObject);
            Assign(falseStarts, "duel", duel);
            var countdown = duel.GetComponent<CountdownController>();
            if (countdown != null)
            {
                var countdownData = new SerializedObject(countdown);
                countdownData.FindProperty("prepareFirstRound").boolValue = false;
                countdownData.ApplyModifiedPropertiesWithoutUndo();
            }
            var input = GetOrAdd<PCInputController>(player);
            Assign(input, "actions", inputAsset); Assign(input, "duel", duel); Assign(input, "falseStarts", falseStarts);
            var positions = GetOrAdd<DuelPositionManager>(duel.gameObject);
            Assign(positions, "markers", markers); Assign(positions, "player", player.transform); Assign(positions, "enemy", enemy.transform);
            var rounds = GetOrAdd<RoundManager>(duel.gameObject);
            Assign(rounds, "duel", duel); Assign(rounds, "positions", positions);
            Assign(rounds, "player", playerHealth); Assign(rounds, "enemy", enemyHealth);

            var cameraControl = GetOrAdd<ThirdPersonCamera>(camera.gameObject);
            Assign(cameraControl, "player", player.transform); Assign(cameraControl, "enemy", enemy.transform);
            Assign(cameraControl, "input", input); Assign(cameraControl, "duel", duel);
            var motor = GetOrAdd<PlayerMovement>(player);
            Assign(motor, "input", input); Assign(motor, "duel", duel); Assign(motor, "markers", markers);

            var playerMuzzle = Muzzle(player.transform);
            var enemyMuzzle = Muzzle(enemy.transform);
            var playerAudio = GetOrAdd<AudioSource>(player);
            var enemyAudio = GetOrAdd<AudioSource>(enemy);
            playerAudio.spatialBlend = 0f;
            enemyAudio.spatialBlend = 1f;
            var playerWeapon = GetOrAdd<WeaponController>(player);
            SetupWeapon(playerWeapon, data, duel, playerHealth, true, playerMuzzle, playerAudio);
            var enemyWeapon = GetOrAdd<WeaponController>(enemy);
            SetupWeapon(enemyWeapon, data, duel, enemyHealth, false, enemyMuzzle, enemyAudio);
            var coin = GetOrAdd<CoinTossFlow>(duel.gameObject);
            Assign(coin, "duel", duel);
            Assign(coin, "playerWeapon", playerWeapon);
            Assign(coin, "enemyWeapon", enemyWeapon);
            var playerEffects = GetOrAdd<ShotEffects>(player);
            Assign(playerEffects, "weapon", playerWeapon); Assign(playerEffects, "muzzle", playerMuzzle);
            var enemyEffects = GetOrAdd<ShotEffects>(enemy);
            Assign(enemyEffects, "weapon", enemyWeapon); Assign(enemyEffects, "muzzle", enemyMuzzle);
            var playerFire = GetOrAdd<PlayerWeapon>(player);
            Assign(playerFire, "input", input); Assign(playerFire, "aimCamera", cameraControl);
            Assign(playerFire, "weapon", playerWeapon); Assign(playerFire, "duel", duel);
            var ai = GetOrAdd<EnemyAI>(enemy);
            Assign(ai, "duel", duel); Assign(ai, "enemyHealth", enemyHealth);
            Assign(ai, "playerHealth", playerHealth); Assign(ai, "weapon", enemyWeapon); Assign(ai, "muzzle", enemyMuzzle);
            CreatePistolPlaceholder(player.transform);
            CreatePistolPlaceholder(enemy.transform);
            SetupPresentation(player, duel, playerHealth, playerWeapon, true);
            SetupPresentation(enemy, duel, enemyHealth, enemyWeapon, false);
            var audio = GetOrAdd<AudioManager>(duel.gameObject);
            Assign(audio, "duel", duel);
            Assign(audio, "countdown", UnityEngine.Object.FindAnyObjectByType<CountdownController>());
            Assign(audio, "falseStarts", falseStarts);
            Assign(audio, "playerWeapon", playerWeapon); Assign(audio, "enemyWeapon", enemyWeapon);
            Assign(audio, "playerSource", playerAudio); Assign(audio, "enemySource", enemyAudio);
            Assign(audio, "uiSource", GetOrAdd<AudioSource>(duel.gameObject));
            var ambience = duel.transform.Find("AmbienceAudio");
            if (ambience == null) { ambience = new GameObject("AmbienceAudio").transform; ambience.SetParent(duel.transform, false); }
            Assign(audio, "ambienceSource", GetOrAdd<AudioSource>(ambience.gameObject));
            if (UnityEngine.Object.FindAnyObjectByType<DuelHUD>() == null)
                CreateHUD(duel, playerHealth, enemyHealth, falseStarts);
            var existingHud = UnityEngine.Object.FindAnyObjectByType<DuelHUD>();
            if (existingHud != null)
            {
                Assign(existingHud, "player", playerHealth);
                Assign(existingHud, "enemy", enemyHealth);
                GetOrAdd<GraphicRaycaster>(existingHud.gameObject);
                EnsurePortrait(existingHud);
                EnsureActionHUD(existingHud);
            }
            EnsureEventSystem();
            SetupVR(duel, markers, camera, input, motor, playerFire, playerWeapon, falseStarts);
            CinematicSetup.ApplyDuel();
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureRosterDifficulties()
        {
            var medium = AssetDatabase.LoadAssetAtPath<DifficultySettings>("Assets/ScriptableObjects/Medium.asset");
            if (medium == null)
            {
                medium = ScriptableObject.CreateInstance<DifficultySettings>();
                medium.name = "Medium";
                AssetDatabase.CreateAsset(medium, "Assets/ScriptableObjects/Medium.asset");
            }
            var normal = AssetDatabase.LoadAssetAtPath<DifficultySettings>("Assets/ScriptableObjects/Normal.asset");
            if (normal != null)
            {
                var source = new SerializedObject(normal);
                var target = new SerializedObject(medium);
                foreach (var field in new[] { "minReactionTime", "maxReactionTime", "accuracy", "missChance", "criticalHitChance", "aimSpeed" })
                {
                    var from = source.FindProperty(field);
                    var to = target.FindProperty(field);
                    if (from != null && to != null) to.floatValue = from.floatValue;
                }
                target.FindProperty("difficultyName").stringValue = "Medium";
                target.ApplyModifiedPropertiesWithoutUndo();
            }
            var legendary = AssetDatabase.LoadAssetAtPath<DifficultySettings>("Assets/ScriptableObjects/Legendary.asset");
            if (legendary == null)
            {
                legendary = ScriptableObject.CreateInstance<DifficultySettings>();
                legendary.name = "Legendary";
                AssetDatabase.CreateAsset(legendary, "Assets/ScriptableObjects/Legendary.asset");
            }
            var data = new SerializedObject(legendary);
            data.FindProperty("difficultyName").stringValue = "Legendary";
            data.FindProperty("minReactionTime").floatValue = .16f;
            data.FindProperty("maxReactionTime").floatValue = .34f;
            data.FindProperty("accuracy").floatValue = .94f;
            data.FindProperty("missChance").floatValue = .03f;
            data.FindProperty("criticalHitChance").floatValue = .24f;
            data.FindProperty("aimSpeed").floatValue = 10f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreatePortraits()
        {
            const string folder = "Assets/Resources/OpponentPortraits";
            Directory.CreateDirectory(folder);
            for (int tier = 0; tier < OpponentRoster.TierCount; tier++)
                for (int index = 0; index < OpponentRoster.Count(tier); index++)
                {
                    var entry = OpponentRoster.Get(tier, index);
                    string path = folder + "/" + entry.Name.Replace(' ', '_') + ".png";
                    if (File.Exists(path)) continue;
                    var pixels = new Color32[64 * 64];
                    Color32 background = new Color(.035f,.055f,.08f);
                    Color32 suit = entry.Suit, skin = entry.Skin;
                    for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                    {
                        Color32 color = background;
                        float dx = (x - 32f) / 16f, dy = (y - 40f) / 19f;
                        if (y < 29 && Mathf.Abs(x - 32) < 12 + (29 - y) * .55f) color = suit;
                        if (dx*dx + dy*dy < 1f) color = skin;
                        if (y > 48 && y < 55 && Mathf.Abs(x - 32) < 15) color = new Color(.045f,.037f,.032f);
                        pixels[y * 64 + x] = color;
                    }
                    var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                    texture.SetPixels32(pixels);
                    texture.Apply();
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                    AssetDatabase.ImportAsset(path);
                }
        }

        private static void ConfigureBootDifficulties()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity");
            var manager = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            if (manager == null) throw new InvalidOperationException("Boot GameManager missing.");
            var data = new SerializedObject(manager);
            var list = data.FindProperty("difficulties");
            list.arraySize = 4;
            string[] names = { "Easy", "Medium", "Hard", "Legendary" };
            for (int i = 0; i < names.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<DifficultySettings>("Assets/ScriptableObjects/" + names[i] + ".asset");
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildMenu()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            if (UnityEngine.Object.FindAnyObjectByType<MainMenuController>() == null)
            {
                var canvas = Canvas("MainMenuCanvas", 8);
                var background = Panel(canvas.transform, "Background", Vector2.zero, new Vector2(1920, 1080), new Color(0.018f, 0.025f, 0.04f, 1f));
                Text(background.transform, "Title", "LAST STEP", new Vector2(0, 245), new Vector2(900, 140), 104);
                Text(background.transform, "Subtitle", "A STAIRCASE DUEL", new Vector2(0, 158), new Vector2(800, 50), 28);
                var start = Button(background.transform, "Start Duel", "START DUEL", new Vector2(0, 40));
                var difficulty = Button(background.transform, "Difficulty", "DIFFICULTY: NORMAL", new Vector2(0, -44));
                var mode = Button(background.transform, "Mode", "MODE: PC", new Vector2(0, -128));
                var quit = Button(background.transform, "Quit", "QUIT", new Vector2(0, -212));
                Text(background.transform, "Footer", "50 / 30 / 10 METERS - THREE SHOTS PER ROUND", new Vector2(0, -260), new Vector2(980, 45), 21);
                var menu = canvas.gameObject.AddComponent<MainMenuController>();
                Assign(menu, "startButton", start);
                Assign(menu, "difficultyButton", difficulty);
                Assign(menu, "quitButton", quit);
                Assign(menu, "modeButton", mode);
                Assign(menu, "modeLabel", mode.GetComponentInChildren<TMP_Text>());
                Assign(menu, "difficultyLabel", difficulty.GetComponentInChildren<TMP_Text>());
            }
            else
            {
                var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
                GetOrAdd<GraphicRaycaster>(menu.gameObject);
                var background = menu.transform.Find("Background");
                if (background != null && !background.GetComponentsInChildren<Button>(true).Any(b => b.name == "Mode"))
                {
                    var mode = Button(background, "Mode", "MODE: PC", new Vector2(0, -128));
                    Assign(menu, "modeButton", mode);
                    Assign(menu, "modeLabel", mode.GetComponentInChildren<TMP_Text>());
                    var quit = background.Find("Quit");
                    if (quit != null) ((RectTransform)quit).anchoredPosition = new Vector2(0, -212);
                }
            }
            EnsureEventSystem();
            CinematicSetup.ApplyMenu();
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupWeapon(WeaponController weapon, WeaponData data, DuelManager duel,
            Health owner, bool player, Transform muzzle, AudioSource source)
        {
            Assign(weapon, "data", data); Assign(weapon, "duel", duel);
            Assign(weapon, "owner", owner); Assign(weapon, "muzzle", muzzle); Assign(weapon, "audioSource", source);
            var serialized = new SerializedObject(weapon);
            serialized.FindProperty("playerWeapon").boolValue = player;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform Muzzle(Transform owner)
        {
            var existing = owner.Find("Muzzle");
            if (existing != null) return existing;
            var point = new GameObject("Muzzle").transform;
            point.SetParent(owner, false);
            point.localPosition = new Vector3(0.25f, 1.4f, 0.48f);
            return point;
        }

        private static void MakeHitZones(Transform owner, Health health)
        {
            if (owner.Find("HitZones") != null) return;
            var root = new GameObject("HitZones").transform;
            root.SetParent(owner, false);
            Zone(root, health, "Head", HitZone.Head, new Vector3(0, 1.61f, 0), new Vector3(0.34f, 0.34f, 0.34f));
            Zone(root, health, "Chest", HitZone.Chest, new Vector3(0, 1.24f, 0), new Vector3(0.56f, 0.39f, 0.42f));
            Zone(root, health, "Torso", HitZone.Torso, new Vector3(0, 0.89f, 0), new Vector3(0.5f, 0.32f, 0.4f));
            Zone(root, health, "LeftArm", HitZone.Arm, new Vector3(-0.38f, 1.09f, 0), new Vector3(0.21f, 0.66f, 0.32f));
            Zone(root, health, "RightArm", HitZone.Arm, new Vector3(0.38f, 1.09f, 0), new Vector3(0.21f, 0.66f, 0.32f));
            Zone(root, health, "LeftLeg", HitZone.Leg, new Vector3(-0.16f, 0.37f, 0), new Vector3(0.22f, 0.7f, 0.32f));
            Zone(root, health, "RightLeg", HitZone.Leg, new Vector3(0.16f, 0.37f, 0), new Vector3(0.22f, 0.7f, 0.32f));
        }

        private static void Zone(Transform root, Health health, string name, HitZone zone, Vector3 position, Vector3 size)
        {
            var part = new GameObject(name, typeof(BoxCollider), typeof(HitDetector));
            part.transform.SetParent(root, false);
            part.transform.localPosition = position;
            part.GetComponent<BoxCollider>().size = size;
            part.GetComponent<HitDetector>().Configure(health, zone);
        }

        private static void CreatePistolPlaceholder(Transform owner)
        {
            if (owner.Find("PistolPlaceholder") != null) return;
            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gun.name = "PistolPlaceholder";
            gun.transform.SetParent(owner, false);
            gun.transform.localPosition = new Vector3(0.28f, 1.26f, 0.32f);
            gun.transform.localScale = new Vector3(0.12f, 0.13f, 0.35f);
            gun.layer = 2;
            UnityEngine.Object.DestroyImmediate(gun.GetComponent<Collider>());
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GB_Metal.mat");
            if (material != null) gun.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void SetupPresentation(GameObject actor, DuelManager duel, Health health, WeaponController weapon, bool player)
        {
            var capsule = actor.transform.Find("Capsule");
            if (capsule == null) return;
            var cosmeticCollider = capsule.GetComponent<Collider>();
            if (cosmeticCollider != null) UnityEngine.Object.DestroyImmediate(cosmeticCollider);
            var visual = actor.transform.Find("VisualRig");
            if (visual == null)
            {
                visual = new GameObject("VisualRig").transform;
                visual.SetParent(actor.transform, false);
            }
            var character = GetOrAdd<ProceduralCharacter>(actor);
            var characterData = new SerializedObject(character);
            characterData.FindProperty("protagonist").boolValue = player;
            characterData.ApplyModifiedPropertiesWithoutUndo();
            var presentation = GetOrAdd<CharacterPresentation>(actor);
            Assign(presentation, "duel", duel); Assign(presentation, "health", health);
            Assign(presentation, "weapon", weapon); Assign(presentation, "visual", visual);
            Assign(presentation, "pistolVisual", actor.transform.Find("PistolPlaceholder"));
            var serialized = new SerializedObject(presentation);
            serialized.FindProperty("player").boolValue = player;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupVR(DuelManager duel, DuelPositionMarkers markers, Camera pcCamera,
            PCInputController pcInput, PlayerMovement movement, PlayerWeapon playerFire,
            WeaponController weapon, FalseStartManager falseStarts)
        {
            var rigs = UnityEngine.Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include)
                .Where(x => x.gameObject.scene == SceneManager.GetActiveScene() && x.gameObject.name == "XR Player Origin")
                .ToArray();
            var root = rigs.Length > 0 ? rigs[0].gameObject : null;
            for (int i = 1; i < rigs.Length; i++) UnityEngine.Object.DestroyImmediate(rigs[i].gameObject);
            if (root == null) root = new GameObject("XR Player Origin", typeof(XROrigin));
            var origin = GetOrAdd<XROrigin>(root);
            var offset = Child(root.transform, "Camera Offset");
            var head = Child(offset, "XR Head");
            var xrCamera = GetOrAdd<Camera>(head.gameObject);
            xrCamera.nearClipPlane = 0.01f;
            GetOrAdd<AudioListener>(head.gameObject);
            origin.Camera = xrCamera;
            origin.CameraFloorOffsetObject = offset.gameObject;
            var left = Hand(offset, "Left Hand", new Color(0.14f, 0.22f, 0.3f));
            var right = Hand(offset, "Right Hand", new Color(0.14f, 0.22f, 0.3f));
            var pistol = root.transform.Find("VR Pistol");
            if (pistol == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "VR Pistol"; go.transform.SetParent(root.transform, false);
                go.transform.localScale = new Vector3(0.07f, 0.13f, 0.32f);
                go.layer = 2;
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
                var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GB_Metal.mat");
                if (metal != null) go.GetComponent<Renderer>().sharedMaterial = metal;
                pistol = go.transform;
            }
            var input = GetOrAdd<VRInputController>(root);
            Assign(input, "duel", duel); Assign(input, "falseStarts", falseStarts);
            var haptics = GetOrAdd<HapticController>(root);
            Assign(haptics, "input", input);
            var vrWeapon = GetOrAdd<VRWeaponController>(root);
            Assign(vrWeapon, "input", input); Assign(vrWeapon, "weapon", weapon);
            Assign(vrWeapon, "duel", duel); Assign(vrWeapon, "pistol", pistol);
            Assign(vrWeapon, "leftHand", left); Assign(vrWeapon, "rightHand", right);
            Assign(vrWeapon, "haptics", haptics);
            var fade = GetOrAdd<VRFadeController>(root);
            var veil = head.Find("Comfort Fade");
            if (veil == null)
            {
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "Comfort Fade"; quad.transform.SetParent(head, false);
                quad.transform.localPosition = new Vector3(0, 0, 0.15f);
                quad.transform.localScale = Vector3.one * 10f;
                UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader != null)
                {
                    var material = new Material(shader) { color = new Color(0, 0, 0, 0) };
                    material.SetFloat("_Surface", 1f);
                    material.SetFloat("_Blend", 0f);
                    material.SetFloat("_ZWrite", 0f);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.renderQueue = 3000;
                    AssetDatabase.CreateAsset(material, "Assets/Materials/VR_ComfortFade.mat");
                    quad.GetComponent<Renderer>().sharedMaterial = material;
                }
                veil = quad.transform;
            }
            var fadeMaterial = veil.GetComponent<Renderer>().sharedMaterial;
            if (fadeMaterial != null)
            {
                fadeMaterial.SetFloat("_Surface", 1f);
                fadeMaterial.SetFloat("_Blend", 0f);
                fadeMaterial.SetFloat("_ZWrite", 0f);
                fadeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                fadeMaterial.renderQueue = 3000;
                fadeMaterial.color = new Color(0, 0, 0, 0);
                EditorUtility.SetDirty(fadeMaterial);
            }
            Assign(fade, "fadeRenderer", veil.GetComponent<Renderer>());
            var setup = GetOrAdd<XRPlayerSetup>(duel.gameObject);
            Assign(setup, "origin", origin); Assign(setup, "cameraOffset", offset);
            Assign(setup, "head", head); Assign(setup, "leftHand", left); Assign(setup, "rightHand", right);
            Assign(setup, "duel", duel); Assign(setup, "markers", markers); Assign(setup, "fade", fade);
            Assign(setup, "pcCamera", pcCamera); Assign(setup, "pcInput", pcInput);
            Assign(setup, "pcMovement", movement); Assign(setup, "pcWeapon", playerFire);
            Assign(setup, "vrWeapon", vrWeapon); Assign(setup, "vrInput", input);
            root.SetActive(false);
        }
        private static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child == null) { child = new GameObject(name).transform; child.SetParent(parent, false); }
            return child;
        }
        private static Transform Hand(Transform parent, string name, Color color)
        {
            var hand = parent.Find(name);
            if (hand != null) return hand;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(0.08f, 0.12f, 0.18f);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go.transform;
        }

        private static void CreateHUD(DuelManager duel, Health player, Health enemy, FalseStartManager falseStarts)
        {
            var canvas = Canvas("DuelHUD", 9);
            var hud = canvas.gameObject.AddComponent<DuelHUD>();
            Assign(hud, "duel", duel); Assign(hud, "player", player);
            Assign(hud, "enemy", enemy); Assign(hud, "falseStarts", falseStarts);
            var status = Text(canvas.transform, "Status", "", new Vector2(0, 475), new Vector2(1100, 100), 30);
            var notice = Text(canvas.transform, "Notice", "", new Vector2(0, -335), new Vector2(1100, 80), 38);
            Text(canvas.transform, "Crosshair", "○", Vector2.zero, new Vector2(82, 82), 50);
            Assign(hud, "status", status); Assign(hud, "notice", notice);
            var result = Panel(canvas.transform, "Result", Vector2.zero, new Vector2(590, 400), new Color(0.025f, 0.03f, 0.045f, 0.94f));
            var resultGroup = result.gameObject.AddComponent<CanvasGroup>();
            resultGroup.alpha = 0; resultGroup.blocksRaycasts = false; resultGroup.interactable = false;
            var resultText = Text(result.transform, "ResultTitle", "", new Vector2(0, 105), new Vector2(550, 110), 70);
            var restart = Button(result.transform, "Restart", "RESTART", new Vector2(0, -15));
            var menu = Button(result.transform, "MainMenu", "MAIN MENU", new Vector2(0, -105));
            var next = Button(result.transform, "NextOpponent", "NEXT OPPONENT", new Vector2(0, -180));
            Assign(hud, "result", resultText); Assign(hud, "resultGroup", resultGroup);
            Assign(hud, "restartButton", restart); Assign(hud, "menuButton", menu);
            Assign(hud, "nextButton", next);
            var pause = Panel(canvas.transform, "Pause", Vector2.zero, new Vector2(440, 300), new Color(0.025f, 0.03f, 0.045f, 0.94f));
            var pauseGroup = pause.gameObject.AddComponent<CanvasGroup>();
            pauseGroup.alpha = 0; pauseGroup.blocksRaycasts = false; pauseGroup.interactable = false;
            Text(pause.transform, "PauseTitle", "PAUSED", new Vector2(0, 80), new Vector2(420, 90), 66);
            var resume = Button(pause.transform, "Resume", "RESUME", new Vector2(0, -45));
            Assign(hud, "pauseGroup", pauseGroup); Assign(hud, "resumeButton", resume);
        }

        private static void EnsurePortrait(DuelHUD hud)
        {
            var child = hud.transform.Find("OpponentPortrait");
            if (child == null)
            {
                var go = new GameObject("OpponentPortrait", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(hud.transform, false);
                child = go.transform;
                var rect = (RectTransform)child;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                rect.sizeDelta = new Vector2(98, 98);
                rect.anchoredPosition = new Vector2(-70, -72);
            }
            var image = child.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.white;
            var data = new SerializedObject(hud);
            data.FindProperty("opponentPortrait").objectReferenceValue = image;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureActionHUD(DuelHUD hud)
        {
            var resultPanel = hud.transform.Find("Result") as RectTransform;
            if (resultPanel != null)
            {
                resultPanel.sizeDelta = new Vector2(650f, 520f);
                var title = resultPanel.Find("ResultTitle")?.GetComponent<TMP_Text>();
                if (title != null)
                {
                    title.fontSize = 39;
                    ((RectTransform)title.transform).sizeDelta = new Vector2(620f, 190f);
                    ((RectTransform)title.transform).anchoredPosition = new Vector2(0f, 135f);
                }
                var restart = resultPanel.Find("Restart") as RectTransform;
                var menu = resultPanel.Find("MainMenu") as RectTransform;
                if (restart != null) restart.anchoredPosition = new Vector2(0f, -20f);
                if (menu != null) menu.anchoredPosition = new Vector2(0f, -175f);
                var next = resultPanel.Find("NextOpponent")?.GetComponent<Button>();
                if (next == null) next = Button(resultPanel, "NextOpponent", "NEXT OPPONENT", new Vector2(0f, -97f));
                ((RectTransform)next.transform).anchoredPosition = new Vector2(0f, -97f);
                var resultData = new SerializedObject(hud);
                resultData.FindProperty("nextButton").objectReferenceValue = next;
                resultData.ApplyModifiedPropertiesWithoutUndo();
            }
            var stamina = hud.transform.Find("StaminaMeter");
            if (stamina == null)
            {
                var backdrop = Panel(hud.transform, "StaminaMeter", new Vector2(-720, -405),
                    new Vector2(300, 20), new Color(.05f, .09f, .12f, .86f));
                stamina = backdrop;
                var fill = Panel(stamina, "Fill", Vector2.zero, new Vector2(286, 12),
                    new Color(.18f, .77f, .82f, 1f));
                var image = fill.GetComponent<Image>();
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = 0;
                Text(hud.transform, "StaminaTitle", "STAMINA", new Vector2(-720, -365),
                    new Vector2(300, 35), 23);
            }
            ((RectTransform)stamina).anchoredPosition = new Vector2(-720,-405);
            var staminaTitle = hud.transform.Find("StaminaTitle") as RectTransform;
            if (staminaTitle != null) staminaTitle.anchoredPosition = new Vector2(-720,-365);
            var ammo = hud.transform.Find("AmmoLabel")?.GetComponent<TMP_Text>();
            if (ammo == null) ammo = Text(hud.transform, "AmmoLabel", "", new Vector2(735, -405),
                new Vector2(350, 66), 27);
            ((RectTransform)ammo.transform).anchoredPosition = new Vector2(735, -405);
            ((RectTransform)ammo.transform).sizeDelta = new Vector2(350, 66);
            ammo.fontSize = 27;
            var round = hud.transform.Find("RoundLabel")?.GetComponent<TMP_Text>();
            if (round == null) round = Text(hud.transform,"RoundLabel","",new Vector2(640,-362),new Vector2(540,55),23);
            ((RectTransform)round.transform).anchoredPosition = new Vector2(640,-362);
            ((RectTransform)round.transform).sizeDelta = new Vector2(540,55);
            round.fontSize = 23;
            var crosshair = hud.transform.Find("Crosshair")?.GetComponent<TMP_Text>();
            if (crosshair != null) { crosshair.text="○"; crosshair.fontSize=50; crosshair.color=new Color(.83f,.92f,1f,.75f); }
            var data = new SerializedObject(hud);
            data.FindProperty("staminaBar").objectReferenceValue = stamina.Find("Fill").GetComponent<Image>();
            data.FindProperty("ammoLabel").objectReferenceValue = ammo;
            data.FindProperty("roundLabel").objectReferenceValue = round;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Canvas Canvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scale = go.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1920, 1080);
            scale.matchWidthOrHeight = 0.5f;
            return canvas;
        }
        private static RectTransform Panel(Transform parent, string name, Vector2 location, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size; rect.anchoredPosition = location;
            go.GetComponent<Image>().color = color;
            return rect;
        }
        private static TMP_Text Text(Transform parent, string name, string content, Vector2 location, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size; rect.anchoredPosition = location;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.93f, 0.93f, 0.9f);
            text.raycastTarget = false; text.text = content;
            return text;
        }
        private static Button Button(Transform parent, string name, string label, Vector2 location)
        {
            var panel = Panel(parent, name, location, new Vector2(370, 66), new Color(0.12f, 0.15f, 0.2f, 1f));
            var button = panel.gameObject.AddComponent<Button>();
            Text(panel, "Label", label, Vector2.zero, new Vector2(360, 60), 31);
            return button;
        }
        private static void EnsureEventSystem()
        {
            var system = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (system == null) system = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            if (system.GetComponent<InputSystemUIInputModule>() == null)
                system.gameObject.AddComponent<InputSystemUIInputModule>();
            var legacy = system.GetComponent<StandaloneInputModule>();
            if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy);
        }
        private static T GetOrAdd<T>(GameObject go) where T : Component
        { return go.TryGetComponent<T>(out var value) ? value : go.AddComponent<T>(); }
        private static void Assign(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException("Missing serialized field " + property + " on " + target.name);
            if (field.objectReferenceValue == null) field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

