using System;
using System.Linq;
using LastStep.Core;
using LastStep.UI;
using LastStep.Utilities;
using LastStep.Weapons;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LastStep.Editor
{
    // Non-destructive visual layer over the existing playable staircase.
    public static class CinematicSetup
    {
        private const string Font = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        public static void EnsureWeaponChoices(WeaponData source)
        {
            const string folder="Assets/Resources/Weapons";
            if(!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets","Resources");
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources","Weapons");
            string sourcePath=AssetDatabase.GetAssetPath(source);
            string[] names={"Sentinel","Swift","Vanguard"};
            float[] cooldown={.25f,.14f,.48f};
            float[] recoil={.85f,.55f,1.5f};
            float[] spread={.08f,.24f,.16f};
            int[] head={100,70,140},chest={50,35,70},torso={50,35,70},arm={25,18,35},leg={25,18,35};
            int[] magazine={12,18,7};
            float[] reload={1.35f,1.1f,1.8f};
            for(int i=0;i<names.Length;i++)
            {
                string path=folder+"/"+names[i]+".asset";
                var asset=AssetDatabase.LoadAssetAtPath<WeaponData>(path);
                if(asset==null)
                {
                    if(!AssetDatabase.CopyAsset(sourcePath,path))
                        throw new InvalidOperationException("Could not create "+path);
                    asset=AssetDatabase.LoadAssetAtPath<WeaponData>(path);
                }
                var serialized=new SerializedObject(asset);
                serialized.FindProperty("fireCooldown").floatValue=cooldown[i];
                serialized.FindProperty("recoilDegrees").floatValue=recoil[i];
                serialized.FindProperty("accuracySpreadDegrees").floatValue=spread[i];
                serialized.FindProperty("headDamage").intValue=head[i];
                serialized.FindProperty("chestDamage").intValue=chest[i];
                serialized.FindProperty("torsoDamage").intValue=torso[i];
                serialized.FindProperty("armDamage").intValue=arm[i];
                serialized.FindProperty("legDamage").intValue=leg[i];
                serialized.FindProperty("magazineSize").intValue=magazine[i];
                serialized.FindProperty("reloadSeconds").floatValue=reload[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
        }
        public static void ApplyDuel()
        {
            var root = GameObject.Find("StaircaseGreybox");
            if (root == null) throw new InvalidOperationException("Staircase greybox missing.");
            var camera = Camera.main;
            var moon = root.GetComponentsInChildren<Light>(true).FirstOrDefault(x => x.type == LightType.Directional);
            var layer = root.transform.Find("CinematicEnvironment");
            if (layer == null)
            {
                layer = new GameObject("CinematicEnvironment").transform;
                layer.SetParent(root.transform, false);
                BuildBackdrop(layer);
                BuildWindows(root.transform, layer);
                BuildTrees(layer);
                AddLampLighting(root.transform);
            }
            PolishBackdrop(layer);
            EnsureCityPlate(root.transform, layer);
            PolishArchitecture(root.transform, layer);
            ApplyMaterials(root.transform);
            var sky = root.GetComponent<SkyPresetController>();
            if (sky == null) sky = root.AddComponent<SkyPresetController>();
            Set(sky, "cloudBackdrop", layer.Find("CloudBackdrop")?.GetComponent<Renderer>());
            Set(sky, "moonLight", moon);
            Set(sky, "duelCamera", camera);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.17f,.23f,.34f);
            RenderSettings.ambientEquatorColor = new Color(.09f,.13f,.19f);
            RenderSettings.ambientGroundColor = new Color(.045f,.06f,.085f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(.055f,.083f,.13f);
            RenderSettings.fogDensity = .0045f;
            if (camera != null)
            {
                camera.allowHDR = true;
                camera.farClipPlane = 350f;
                camera.fieldOfView = 50f;
                var shoulder = camera.GetComponent<LastStep.Player.ThirdPersonCamera>();
                if (shoulder != null)
                {
                    var serialized = new SerializedObject(shoulder);
                    serialized.FindProperty("shoulderOffset").vector3Value = new Vector3(1.0f,1.95f,-2.15f);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var urpCamera = camera.GetComponent<UniversalAdditionalCameraData>();
                if (urpCamera != null)
                {
                    urpCamera.renderPostProcessing = true;
                    urpCamera.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    urpCamera.antialiasingQuality = AntialiasingQuality.High;
                }
            }
            EnsureVolume(layer);
            EnsureCharacterFill();
            PolishWeapons(root.transform);
            AddHudBars();
        }
        private static void EnsureCharacterFill()
        {
            var markers = UnityEngine.Object.FindAnyObjectByType<DuelPositionMarkers>();
            if (markers == null) return;
            foreach (var actor in new[] { markers.PlayerStandIn, markers.EnemyStandIn })
            {
                if (actor == null) continue;
                if (actor.Find("CinematicFaceFill") == null)
                {
                    var child = new GameObject("CinematicFaceFill");
                    child.transform.SetParent(actor, false);
                    child.transform.localPosition = new Vector3(.1f, 1.65f, .65f);
                    var light = child.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = new Color(.93f, .84f, .73f);
                    light.intensity = 1.25f;
                    light.range = 3f;
                    light.shadows = LightShadows.None;
                }
                if (actor.Find("CinematicRim") == null)
                {
                    var rim = new GameObject("CinematicRim");
                    rim.transform.SetParent(actor, false);
                    rim.transform.localPosition = new Vector3(-.55f, 1.85f, -.75f);
                    var rimLight = rim.AddComponent<Light>();
                    rimLight.type = LightType.Point;
                    rimLight.color = new Color(.62f, .76f, 1f);
                    rimLight.intensity = 2.6f;
                    rimLight.range = 3.6f;
                    rimLight.shadows = LightShadows.None;
                }
            }
        }

        public static void ApplyMenu()
        {
            var menu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
            if (menu == null) return;
            var background = menu.transform.Find("Background");
            if (background == null) return;
            var footer = background.Find("Footer")?.GetComponent<TMP_Text>();
            if (footer != null)
            {
                footer.text = "50 / 30 / 10 METERS  •  THREE SHOTS PER ROUND";
                footer.fontSize = 21;
                ((RectTransform)footer.transform).sizeDelta = new Vector2(980f, 45f);
            }
            var sky = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Menu/OriginalCinematicBackground.png");
            if (background.Find("NightSkyArt") == null && sky != null)
            {
                var image = new GameObject("NightSkyArt", typeof(RectTransform), typeof(RawImage));
                image.transform.SetParent(background, false);
                var rect = (RectTransform)image.transform;
                rect.sizeDelta = new Vector2(1920, 1080);
                var raw = image.GetComponent<RawImage>();
                raw.texture = sky; raw.color = Color.white; raw.raycastTarget = false;
                image.transform.SetAsFirstSibling();
                var veil = new GameObject("MenuVeil", typeof(RectTransform), typeof(Image));
                veil.transform.SetParent(background, false);
                ((RectTransform)veil.transform).sizeDelta = new Vector2(1920,1080);
                veil.GetComponent<Image>().color = new Color(.008f,.012f,.025f,.18f);
                veil.GetComponent<Image>().raycastTarget = false;
                veil.transform.SetSiblingIndex(1);
            }
            var existingArt = background.Find("NightSkyArt")?.GetComponent<RawImage>();
            if (existingArt != null && sky != null) { existingArt.texture = sky; existingArt.color = Color.white; }
            var existingVeil = background.Find("MenuVeil")?.GetComponent<Image>();
            if (existingVeil != null) existingVeil.color = new Color(.008f,.012f,.025f,.18f);
            var galleryButton = EnsureButton(background, "OpponentGalleryButton", "OPPONENT GALLERY", new Vector2(-445,-125), 25);
            var skyButton = FindOrCreateButton(background, "SkyPresetButton", "SKY: MOONLIT NIGHT", new Vector2(445,-85), 24);
            var effectsButton = FindOrCreateButton(background, "EffectsButton", "EFFECTS: HIGH", new Vector2(445,-169), 25);
            var pistolButton = FindOrCreateButton(background, "PistolSelectButton", "WEAPON: SENTINEL", new Vector2(445,-253), 24);
            var pistolSummary = background.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "PistolSummary");
            if(pistolSummary==null)
                pistolSummary=Text(background,"PistolSummary","Balanced  |  precise  |  moderate recoil",
                    new Vector2(445,-315),new Vector2(520,48),19);
            var preview=background.GetComponentsInChildren<RawImage>(true).FirstOrDefault(t => t.name == "PistolPreview");
            if(preview==null)
            {
                var previewObject=new GameObject("PistolPreview",typeof(RectTransform),typeof(RawImage));
                previewObject.transform.SetParent(background,false);
                var rect=(RectTransform)previewObject.transform;
                rect.anchoredPosition=new Vector2(445,-420);
                rect.sizeDelta=new Vector2(260,146);
                preview=previewObject.GetComponent<RawImage>();
                preview.raycastTarget=false;
            }
            Set(menu, "skyButton", skyButton); Set(menu, "skyLabel", skyButton.GetComponentInChildren<TMP_Text>());
            Set(menu, "effectsButton", effectsButton); Set(menu, "effectsLabel", effectsButton.GetComponentInChildren<TMP_Text>());
            Set(menu, "pistolButton", pistolButton); Set(menu, "pistolLabel", pistolButton.GetComponentInChildren<TMP_Text>());
            Set(menu, "pistolSummary", pistolSummary);
            Set(menu, "pistolPreview", preview);
            EnsureGallery(menu.gameObject, galleryButton);
            BuildReferenceMenu(menu, background, galleryButton, skyButton, effectsButton,
                pistolButton, pistolSummary, preview);
        }

        private static void BuildReferenceMenu(MainMenuController menu, Transform background,
            Button galleryButton, Button skyButton, Button effectsButton, Button pistolButton,
            TMP_Text pistolSummary, RawImage preview)
        {
            var title = background.Find("Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.text = "JOHN WICK <color=#d5192b>NMU</color>";
                title.fontSize = 86;
                title.fontStyle = FontStyles.Bold;
                var rect = (RectTransform)title.transform;
                rect.anchoredPosition = new Vector2(-255, 235);
                rect.sizeDelta = new Vector2(1180, 140);
            }
            var subtitle = background.Find("Subtitle")?.GetComponent<TMP_Text>();
            if (subtitle != null)
            {
                subtitle.text = "A CINEMATIC STAIRCASE DUEL";
                ((RectTransform)subtitle.transform).anchoredPosition = new Vector2(-310, 155);
            }
            var branding = background.Find("UniversityBrand")?.GetComponent<TMP_Text>();
            if (branding == null) branding = Text(background, "UniversityBrand",
                "NEW MANSOURA UNIVERSITY   <color=#d5192b>#NMU</color>",
                new Vector2(-575, 445), new Vector2(690, 64), 29);
            branding.fontStyle = FontStyles.Bold;
            var start = background.Find("Start Duel")?.GetComponent<Button>();
            var quit = background.Find("Quit")?.GetComponent<Button>();
            var weaponOpen = EnsureButton(background, "WeaponOpenButton", "WEAPON SELECTION",
                new Vector2(-605, -13), 25);
            var settingsOpen = EnsureButton(background, "SettingsOpenButton", "SETTINGS",
                new Vector2(-605, -181), 25);
            var duelStyle = EnsureButton(background, "DuelStyleButton", "MODE: LEVEL CAMPAIGN",
                new Vector2(-605, 25), 25);
            if (start != null) StyleNav(start, "START GAME", new Vector2(-605, 105), true);
            StyleNav(duelStyle, "MODE: LEVEL CAMPAIGN", new Vector2(-605, 25), false);
            StyleNav(weaponOpen, "WEAPON SELECTION", new Vector2(-605, -55), false);
            StyleNav(galleryButton, "OPPONENTS", new Vector2(-605, -135), false);
            StyleNav(settingsOpen, "SETTINGS", new Vector2(-605, -215), false);
            if (quit != null) StyleNav(quit, "EXIT", new Vector2(-605, -295), false);
            Set(menu, "duelStyleButton", duelStyle);
            Set(menu, "duelStyleLabel", duelStyle.GetComponentInChildren<TMP_Text>());

            var weaponRoot = background.Find("WeaponSelectionPanel") as RectTransform;
            if (weaponRoot == null) weaponRoot = Panel(background, "WeaponSelectionPanel",
                new Vector2(680, 690), new Color(.018f, .025f, .04f, .97f));
            weaponRoot.anchoredPosition = new Vector2(240, -70);
            var weaponGroup = weaponRoot.GetComponent<CanvasGroup>();
            if (weaponGroup == null) weaponGroup = weaponRoot.gameObject.AddComponent<CanvasGroup>();
            if (weaponRoot.Find("Heading") == null)
                Text(weaponRoot, "Heading", "WEAPON SELECTION", new Vector2(0, 285), new Vector2(610, 65), 40);
            pistolButton.transform.SetParent(weaponRoot, false);
            ((RectTransform)pistolButton.transform).anchoredPosition = new Vector2(0, 165);
            pistolSummary.transform.SetParent(weaponRoot, false);
            ((RectTransform)pistolSummary.transform).anchoredPosition = new Vector2(0, 95);
            ((RectTransform)pistolSummary.transform).sizeDelta = new Vector2(600, 80);
            preview.transform.SetParent(weaponRoot, false);
            ((RectTransform)preview.transform).anchoredPosition = new Vector2(0, -95);
            ((RectTransform)preview.transform).sizeDelta = new Vector2(450, 260);
            var equip = EnsureButton(weaponRoot, "Equip", "EQUIP", new Vector2(0, -265), 29);
            StyleNav(equip, "EQUIP", new Vector2(0, -265), true);
            var weaponClose = EnsureButton(weaponRoot, "Close", "BACK", new Vector2(253, 285), 21);
            ((RectTransform)weaponClose.transform).sizeDelta = new Vector2(125, 48);

            var settingsRoot = background.Find("SettingsPanel") as RectTransform;
            if (settingsRoot == null) settingsRoot = Panel(background, "SettingsPanel",
                new Vector2(680, 690), new Color(.018f, .025f, .04f, .97f));
            settingsRoot.anchoredPosition = new Vector2(240, -70);
            var settingsGroup = settingsRoot.GetComponent<CanvasGroup>();
            if (settingsGroup == null) settingsGroup = settingsRoot.gameObject.AddComponent<CanvasGroup>();
            if (settingsRoot.Find("Heading") == null)
                Text(settingsRoot, "Heading", "SETTINGS", new Vector2(0, 285), new Vector2(610, 65), 40);
            var difficulty = background.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Difficulty");
            var mode = background.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == "Mode");
            if (difficulty != null) { difficulty.SetParent(settingsRoot, false); difficulty.anchoredPosition = new Vector2(0, 190); }
            if (mode != null) { mode.SetParent(settingsRoot, false); mode.anchoredPosition = new Vector2(0, 105); }
            skyButton.transform.SetParent(settingsRoot, false);
            ((RectTransform)skyButton.transform).anchoredPosition = new Vector2(0, 20);
            effectsButton.transform.SetParent(settingsRoot, false);
            ((RectTransform)effectsButton.transform).anchoredPosition = new Vector2(0, -65);
            var settingsClose = EnsureButton(settingsRoot, "Close", "BACK", new Vector2(253, 285), 21);
            ((RectTransform)settingsClose.transform).sizeDelta = new Vector2(125, 48);
            weaponGroup.alpha = settingsGroup.alpha = 0;
            weaponGroup.interactable = settingsGroup.interactable = false;
            weaponGroup.blocksRaycasts = settingsGroup.blocksRaycasts = false;
            Set(menu, "weaponOpenButton", weaponOpen);
            Set(menu, "weaponEquipButton", equip);
            Set(menu, "weaponCloseButton", weaponClose);
            Set(menu, "weaponPanel", weaponGroup);
            Set(menu, "settingsOpenButton", settingsOpen);
            Set(menu, "settingsCloseButton", settingsClose);
            Set(menu, "settingsPanel", settingsGroup);
        }

        private static void StyleNav(Button button, string caption, Vector2 position, bool primary)
        {
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = position;
            if (button.GetComponent<Image>() is Image image)
                image.color = primary ? new Color(.58f, .025f, .055f, .97f)
                    : new Color(.025f, .035f, .055f, .87f);
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) { label.text = caption; label.fontStyle = FontStyles.Bold; }
        }
        private static Button FindOrCreateButton(Transform root, string name, string caption, Vector2 position, float size)
        {
            var existing = root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);
            return existing != null ? existing : EnsureButton(root, name, caption, position, size);
        }

        private static void BuildBackdrop(Transform layer)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sky/MoonlitClouds.png");
            if (texture == null) throw new InvalidOperationException("Generated moonlit cloud texture missing.");
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "CloudBackdrop";
            quad.transform.SetParent(layer, false);
            quad.transform.localPosition = new Vector3(0, 15, 175);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(520, 135, 1);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cine_CloudBackdrop.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Cine_CloudBackdrop" };
                AssetDatabase.CreateAsset(material, "Assets/Materials/Cine_CloudBackdrop.mat");
            }
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Cull", 0);
            quad.GetComponent<Renderer>().sharedMaterial = material;
            quad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void PolishBackdrop(Transform layer)
        {
            var backdrop = layer.Find("CloudBackdrop");
            if (backdrop == null) return;
            backdrop.localPosition = new Vector3(0,15,175);
            backdrop.localRotation = Quaternion.identity;
            backdrop.localScale = new Vector3(520,135,1);
            var material = backdrop.GetComponent<Renderer>().sharedMaterial;
            if (material != null)
            {
                material.SetFloat("_Cull",0);
                material.SetTextureScale("_BaseMap",Vector2.one);
                material.SetTextureOffset("_BaseMap",Vector2.zero);
                EditorUtility.SetDirty(material);
            }
        }
        private static void EnsureCityPlate(Transform root, Transform layer)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sky/DistantCityPlate.png");
            if(texture==null)return;
            var billboard=layer.Find("DistantCityPlate");
            if(billboard==null)
            {
                var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name="DistantCityPlate";
                quad.transform.SetParent(layer,false);
                UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
                billboard=quad.transform;
            }
            billboard.localPosition=new Vector3(0,17f,105f);
            billboard.localScale=new Vector3(250f,83f,1f);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cine_CityPlate.mat");
            if(material==null)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Cine_CityPlate"};
                AssetDatabase.CreateAsset(material,"Assets/Materials/Cine_CityPlate.mat");
            }
            material.SetTexture("_BaseMap",texture);
            material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_Surface",1f);
            material.SetFloat("_Blend",0f);
            material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite",0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue=(int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            billboard.GetComponent<Renderer>().sharedMaterial=material;
            billboard.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            var city=root.Find("DistantCityGreybox");
            if(city!=null)foreach(var renderer in city.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            foreach(var groupName in new[]{"CityWindows","CityRoofline"})
            {
                var group=layer.Find(groupName);
                if(group!=null)foreach(var renderer in group.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            }
        }

        private static void PolishArchitecture(Transform root, Transform layer)
        {
            var stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cine_ColumnStone.mat");
            if(stone==null)
            {
                stone=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Cine_ColumnStone",enableInstancing=true};
                AssetDatabase.CreateAsset(stone,"Assets/Materials/Cine_ColumnStone.mat");
            }
            stone.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Stone/RainDarkStone.png"));
            stone.SetColor("_BaseColor",new Color(.70f,.73f,.78f));
            stone.SetFloat("_Smoothness",.46f);
            EditorUtility.SetDirty(stone);
            var dark = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GB_Metal.mat");
            var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cine_LampGlass.mat");
            if(glass==null)
            {
                glass=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Cine_LampGlass"};
                AssetDatabase.CreateAsset(glass,"Assets/Materials/Cine_LampGlass.mat");
            }
            glass.SetColor("_BaseColor",new Color(3f,1.35f,.42f));
            EditorUtility.SetDirty(glass);
            foreach (var pillar in root.GetComponentsInChildren<Transform>(true).Where(x=>x.name=="GreyboxPillar"))
            {
                foreach(var name in new[]{"Plinth","Capital"})
                {
                    var existing=pillar.Find(name);
                    if(existing!=null && existing.GetComponent<Renderer>()!=null) existing.GetComponent<Renderer>().sharedMaterial=stone;
                }
                var existingColumn = pillar.Find("CinematicColumn");
                if (existingColumn != null)
                {
                    AddFluting(existingColumn,stone);
                    continue;
                }
                var shaft = pillar.Find("Shaft");
                if (shaft != null && shaft.GetComponent<Renderer>() != null) shaft.GetComponent<Renderer>().enabled = false;
                var group = new GameObject("CinematicColumn").transform;
                group.SetParent(pillar,false);
                ColumnPart(group,"FlutedShaft",new Vector3(0,1.44f,0),new Vector3(.46f,1.03f,.46f),stone,16);
                ColumnPart(group,"NeckRing",new Vector3(0,2.46f,0),new Vector3(.56f,.055f,.56f),stone,16);
                ColumnPart(group,"BaseRing",new Vector3(0,.44f,0),new Vector3(.55f,.07f,.55f),stone,16);
                BoxPart(group,"CapitalTrim",new Vector3(0,2.81f,0),new Vector3(1.28f,.09f,1.28f),stone);
                BoxPart(group,"PlinthTrim",new Vector3(0,.39f,0),new Vector3(1.58f,.07f,1.58f),stone);
                AddFluting(group,stone);
            }
            foreach (var lamp in root.GetComponentsInChildren<Transform>(true).Where(x=>x.name=="GreyboxLamp"))
            {
                if (lamp.Find("CinematicLantern") != null) continue;
                var housing = lamp.Find("Housing");
                if (housing != null && housing.GetComponent<Renderer>() != null) housing.GetComponent<Renderer>().enabled=false;
                var group = new GameObject("CinematicLantern").transform;
                group.SetParent(lamp,false);
                BoxPart(group,"WarmGlass",new Vector3(0,2.86f,0),new Vector3(.38f,.52f,.38f),glass);
                foreach(int side in new[]{-1,1})
                {
                    BoxPart(group,"FrameX",new Vector3(side*.23f,2.86f,0),new Vector3(.055f,.62f,.52f),dark);
                    BoxPart(group,"FrameZ",new Vector3(0,2.86f,side*.23f),new Vector3(.52f,.62f,.055f),dark);
                }
                BoxPart(group,"Roof",new Vector3(0,3.26f,0),new Vector3(.74f,.09f,.74f),dark);
                ColumnPart(group,"Finial",new Vector3(0,3.38f,0),new Vector3(.10f,.14f,.10f),dark,8);
            }
            AddSceneDetails(root,layer,stone,dark,glass);
            InstallImportedArchitecture(root, stone, dark);
        }
        private static void InstallImportedArchitecture(Transform root, Material stone, Material metal)
        {
            var column = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PolyPizza/Column/Column2.fbx");
            var lamp = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/PolyPizza/Lamp/StreetLight.fbx");
            if (column != null)
                foreach (var pillar in root.GetComponentsInChildren<Transform>(true).Where(x => x.name == "GreyboxPillar"))
                    PlaceImportedMesh(pillar, column, "ImportedStoneColumn", 2.9f, stone,
                        new[] { "CinematicColumn", "Shaft", "Plinth", "Capital" });
            if (lamp != null)
                foreach (var fixture in root.GetComponentsInChildren<Transform>(true).Where(x => x.name == "GreyboxLamp"))
                {
                    PlaceImportedMesh(fixture, lamp, "ImportedStreetLamp", 3.4f, metal,
                        new[] { "Housing", "Post" });
                    // Keep the warm glass visible alongside the licensed lamp mesh.
                    var lantern = fixture.Find("CinematicLantern");
                    if (lantern != null)
                    {
                        lantern.localPosition = new Vector3(0f,.35f,0f);
                        foreach (var renderer in lantern.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
                    }
                }
        }
        private static void PlaceImportedMesh(Transform parent, GameObject source, string name,
            float height, Material material, string[] replaced)
        {
            if (parent.Find(name) != null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) { UnityEngine.Object.DestroyImmediate(instance); return; }
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            instance.transform.localScale *= height / Mathf.Max(.01f, bounds.size.y);
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) { bounds.Encapsulate(renderer.bounds); renderer.sharedMaterial = material; }
            var bottom = parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            instance.transform.localPosition -= bottom;
            foreach (string oldName in replaced)
            {
                var old = parent.Find(oldName);
                if (old != null) foreach (var renderer in old.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            }
        }
        private static void AddFluting(Transform column,Material stone)
        {
            if(column.Find("Fluting")!=null) return;
            var group=new GameObject("Fluting").transform;group.SetParent(column,false);
            for(int i=0;i<10;i++)
            {
                float angle=i*Mathf.PI*2f/10f;
                var strip=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                strip.name="StoneRib";strip.transform.SetParent(group,false);
                strip.transform.localPosition=new Vector3(Mathf.Sin(angle)*.442f,1.45f,Mathf.Cos(angle)*.442f);
                strip.transform.localScale=new Vector3(.035f,.88f,.035f);
                UnityEngine.Object.DestroyImmediate(strip.GetComponent<Collider>());
                strip.GetComponent<Renderer>().sharedMaterial=stone;
            }
        }
        private static void AddSceneDetails(Transform root,Transform layer,Material stone,Material dark,Material glass)
        {
            if(layer.Find("StairOrnaments")==null)
            {
                var group=new GameObject("StairOrnaments").transform;group.SetParent(layer,false);
                foreach(var post in root.GetComponentsInChildren<Renderer>(true).Where(x=>x.name.StartsWith("RailPost_")))
                {
                    var cap=GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    cap.name="BalusterFinial";cap.transform.SetParent(group,true);
                    cap.transform.position=post.bounds.center+Vector3.up*(post.bounds.extents.y+.045f);
                    cap.transform.localScale=Vector3.one*.095f;
                    UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());
                    cap.GetComponent<Renderer>().sharedMaterial=stone;
                }
            }
            if(layer.Find("CityRoofline")==null)
            {
                var group=new GameObject("CityRoofline").transform;group.SetParent(layer,false);
                var city=root.Find("DistantCityGreybox");
                if(city!=null)
                {
                    int index=0;
                    foreach(var building in city.GetComponentsInChildren<Renderer>(true))
                    {
                        var b=building.bounds;
                        var cap=GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cap.name="RoofCornice";cap.transform.SetParent(group,true);
                        cap.transform.position=new Vector3(b.center.x,b.max.y+.11f,b.center.z);
                        cap.transform.localScale=new Vector3(b.size.x+.35f,.22f,b.size.z+.35f);
                        UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());
                        cap.GetComponent<Renderer>().sharedMaterial=dark;
                        if(index%4==0)
                        {
                            var top=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                            top.name="RoofTurret";top.transform.SetParent(group,true);
                            top.transform.position=new Vector3(b.center.x,b.max.y+1.1f,b.center.z);
                            top.transform.localScale=new Vector3(.9f,1.0f,.9f);
                            UnityEngine.Object.DestroyImmediate(top.GetComponent<Collider>());
                            top.GetComponent<Renderer>().sharedMaterial=stone;
                        }
                        index++;
                    }
                }
            }
        }
        private static void ColumnPart(Transform parent,string name,Vector3 position,Vector3 scale,Material material,int sides)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.transform.localScale=scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial=material;
        }
        private static void BoxPart(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;
            go.transform.localScale=scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial=material;
        }

        private static void ApplyMaterials(Transform root)
        {
            const string stonePath = "Assets/Art/Stone/OriginalWetLimestone.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(stonePath) ??
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Stone/RainDarkStone.png");
            if (texture == null) return;
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            if (importer != null && (importer.wrapMode != TextureWrapMode.Repeat || importer.anisoLevel != 4))
            { importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 4; importer.SaveAndReimport(); }
            var wet = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cine_WetPaving.mat");
            if (wet == null)
            {
                wet = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Cine_WetPaving", enableInstancing = true };
                AssetDatabase.CreateAsset(wet, "Assets/Materials/Cine_WetPaving.mat");
            }
            wet.SetTexture("_BaseMap", texture);
            wet.SetTextureScale("_BaseMap", Vector2.one);
            wet.SetTexture("_BumpMap",null);
            wet.DisableKeyword("_NORMALMAP");
            wet.SetColor("_BaseColor", new Color(.72f,.75f,.8f));
            wet.SetFloat("_Smoothness", .38f);
            wet.SetFloat("_Metallic", 0f);
            wet.SetFloat("_Cull", 0f);
            var tread = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cine_WetStepTop.mat");
            if (tread == null)
            { tread = new Material(wet) { name = "Cine_WetStepTop" }; AssetDatabase.CreateAsset(tread, "Assets/Materials/Cine_WetStepTop.mat"); }
            tread.CopyPropertiesFromMaterial(wet);
            tread.SetTextureScale("_BaseMap", new Vector2(9f, 1f));
            var landing = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cine_WetLandingTop.mat");
            if (landing == null)
            { landing = new Material(wet) { name = "Cine_WetLandingTop" }; AssetDatabase.CreateAsset(landing, "Assets/Materials/Cine_WetLandingTop.mat"); }
            landing.CopyPropertiesFromMaterial(wet);
            landing.SetTextureScale("_BaseMap", new Vector2(11f, 2f));
            var paving = root.Find("StairsAndLandings");
            if (paving != null)
                foreach (var renderer in paving.GetComponentsInChildren<Renderer>(true).ToArray())
                    if (renderer.name.StartsWith("Landing_") || renderer.name.StartsWith("Flight_"))
                    {
                        renderer.sharedMaterial = wet;
                        var bounds=renderer.bounds;
                        var block=new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(block);
                        block.SetVector("_BaseMap_ST",new Vector4(
                            Mathf.Max(.2f,bounds.size.x/5f),Mathf.Max(.2f,bounds.size.z/3.9f),0f,0f));
                        renderer.SetPropertyBlock(block);
                        // Cube side UVs compress the stone image into long bands. A separate
                        // top face gives every tread a readable, correctly oriented paving UV.
                        var existing = renderer.transform.Find("WetStoneTop");
                        var top = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
                        top.name = "WetStoneTop";
                        top.transform.SetParent(renderer.transform, false);
                        top.transform.localPosition = new Vector3(0f,.5008f,0f);
                        top.transform.localRotation = Quaternion.Euler(-90f,0f,0f);
                        top.transform.localScale = Vector3.one;
                        var collider = top.GetComponent<Collider>();
                        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
                        var topRenderer = top.GetComponent<Renderer>();
                        topRenderer.sharedMaterial = renderer.name.StartsWith("Landing_") ? landing : tread;
                        topRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    }
        }

        private static void BuildWindows(Transform root, Transform layer)
        {
            var buildings = root.Find("DistantCityGreybox");
            if (buildings == null) return;
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cine_CityWindow.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Cine_CityWindow", enableInstancing = true };
                material.SetColor("_BaseColor", new Color(1f,.64f,.26f));
                material.SetFloat("_Cull",0);
                AssetDatabase.CreateAsset(material, "Assets/Materials/Cine_CityWindow.mat");
            }
            material.SetFloat("_Cull",0);
            var group = new GameObject("CityWindows").transform;
            group.SetParent(layer, false);
            int buildingIndex = 0;
            foreach (var renderer in buildings.GetComponentsInChildren<Renderer>(true))
            {
                Bounds b = renderer.bounds;
                for (int row = 0; row < 4; row++) for (int col = 0; col < 3; col++)
                {
                    if (((buildingIndex * 7 + row * 3 + col * 5) % 5) == 0) continue;
                    var pane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    pane.name = "Window";
                    pane.transform.SetParent(group, true);
                    pane.transform.position = new Vector3(Mathf.Lerp(b.min.x+.7f,b.max.x-.7f,(col+.5f)/3f),
                        Mathf.Lerp(b.min.y+2f,b.max.y-1f,(row+.5f)/4f),b.min.z-.03f);
                    pane.transform.rotation = Quaternion.Euler(0,180,0);
                    pane.transform.localScale = new Vector3(.35f,.5f,1f);
                    UnityEngine.Object.DestroyImmediate(pane.GetComponent<Collider>());
                    pane.GetComponent<Renderer>().sharedMaterial = material;
                    pane.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
                buildingIndex++;
            }
        }

        private static void BuildTrees(Transform layer)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cine_Cypress.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Cine_Cypress" };
                material.SetColor("_BaseColor", new Color(.025f,.055f,.06f));
                AssetDatabase.CreateAsset(material,"Assets/Materials/Cine_Cypress.mat");
            }
            var trees = new GameObject("CypressSilhouettes").transform;
            trees.SetParent(layer,false);
            for (int i = 0; i < 14; i++) foreach (int side in new[] {-1,1})
            {
                var crown = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                crown.name = "Cypress";
                crown.transform.SetParent(trees,false);
                float z = i * 8f - 26f;
                float height = Mathf.Lerp(6f,13f,(i*7%11)/10f);
                crown.transform.localPosition = new Vector3(side*(18f+(i%3)*2f),height*.5f-3f,z);
                crown.transform.localScale = new Vector3(.85f,height*.5f,.85f);
                UnityEngine.Object.DestroyImmediate(crown.GetComponent<Collider>());
                crown.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        private static void AddLampLighting(Transform root)
        {
            var lamps = root.GetComponentsInChildren<Transform>(true).Where(x => x.name == "GreyboxLamp").ToArray();
            for (int i = 0; i < lamps.Length; i++)
            {
                var existing = lamps[i].Find("CinematicWarmLight");
                var go = existing != null ? existing.gameObject : new GameObject("CinematicWarmLight",typeof(Light));
                if (existing == null) go.transform.SetParent(lamps[i],false);
                go.transform.localPosition = new Vector3(0,2.8f,0);
                var light = go.GetComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f,.67f,.33f);
                light.intensity = 6f;
                light.range = 12f;
                light.shadows = LightShadows.None;
            }
        }

        private static void EnsureVolume(Transform layer)
        {
            var pipeline = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null) pipeline.supportsHDR = true;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/CinematicVolume.asset");
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile,"Assets/Settings/CinematicVolume.asset");
            }
            if (!profile.TryGet<Bloom>(out var bloom)) bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(.38f);
            bloom.threshold.Override(1.1f);
            if (!profile.TryGet<Vignette>(out var vignette)) vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(.2f);
            if (!profile.TryGet<Tonemapping>(out var tonemapping)) tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            if (!profile.TryGet<ColorAdjustments>(out var grade)) grade = profile.Add<ColorAdjustments>(true);
            grade.contrast.Override(15f);
            grade.saturation.Override(-8f);
            EditorUtility.SetDirty(profile);
            var go = layer.Find("CinematicVolume");
            if (go == null) { go = new GameObject("CinematicVolume",typeof(Volume)).transform; go.SetParent(layer,false); }
            var volume = go.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
        }

        private static void PolishWeapons(Transform root)
        {
            var metal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/GB_Metal.mat");
            foreach (var actor in root.GetComponentsInChildren<ProceduralCharacter>(true))
            {
                var pistol = actor.transform.Find("PistolPlaceholder");
                if (pistol == null) continue;
                pistol.localPosition = new Vector3(.50f,1.29f,.56f);
                pistol.localScale = Vector3.one;
                var muzzle = pistol.parent.Find("Muzzle");
                if (muzzle != null) muzzle.localPosition = new Vector3(.50f,1.325f,.88f);
                if(pistol.GetComponent<PistolVisualVariant>()==null) pistol.gameObject.AddComponent<PistolVisualVariant>();
                if (pistol.Find("CinematicPistol") != null) continue;
                var renderer = pistol.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;
                var oldSlide=pistol.Find("Slide");
                if(oldSlide!=null) UnityEngine.Object.DestroyImmediate(oldSlide.gameObject);
                foreach(var part in new[]{"Barrel","Grip","FrontSight"})
                {
                    var old=pistol.Find(part);if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                }
                var grip=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Cine_PistolGrip.mat");
                if(grip==null)
                {
                    grip=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Cine_PistolGrip"};
                    grip.SetColor("_BaseColor",new Color(.045f,.05f,.056f));
                    grip.SetFloat("_Smoothness",.24f);
                    AssetDatabase.CreateAsset(grip,"Assets/Materials/Cine_PistolGrip.mat");
                }
                var group=new GameObject("CinematicPistol").transform;group.SetParent(pistol,false);
                PistolPart(group,"MachinedSlide",new Vector3(0,.055f,.095f),new Vector3(.105f,.095f,.35f),metal);
                PistolPart(group,"Frame",new Vector3(0,-.018f,.08f),new Vector3(.092f,.08f,.30f),metal);
                PistolPart(group,"EjectionPort",new Vector3(.056f,.105f,.08f),new Vector3(.005f,.012f,.07f),grip);
                PistolPart(group,"Barrel",new Vector3(0,.035f,.285f),new Vector3(.07f,.065f,.065f),metal);
                PistolPart(group,"MuzzleBore",new Vector3(0,.035f,.319f),new Vector3(.034f,.035f,.006f),grip);
                PistolPart(group,"AngledGrip",new Vector3(0,-.14f,-.035f),new Vector3(.087f,.24f,.12f),grip);
                PistolPart(group,"MagazineBase",new Vector3(0,-.275f,-.04f),new Vector3(.105f,.025f,.135f),metal);
                PistolPart(group,"TriggerGuardTop",new Vector3(0,-.105f,.115f),new Vector3(.07f,.018f,.10f),metal);
                PistolPart(group,"TriggerGuardFront",new Vector3(0,-.15f,.165f),new Vector3(.065f,.10f,.018f),metal);
                PistolPart(group,"RearSight",new Vector3(0,.115f,-.045f),new Vector3(.065f,.027f,.02f),grip);
                PistolPart(group,"FrontSight",new Vector3(0,.115f,.235f),new Vector3(.025f,.027f,.025f),grip);
            }
        }
        private static void PistolPart(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent,false);
            go.transform.localPosition = position; go.transform.localScale = scale;
            go.layer = 2;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void AddHudBars()
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<DuelHUD>();
            if (hud == null) return;
            var status = hud.transform.Find("Status") as RectTransform;
            if (status != null)
            {
                status.anchoredPosition = new Vector2(0, 390);
                status.sizeDelta = new Vector2(900, 58);
                status.GetComponent<TMP_Text>().fontSize = 19;
            }
            var player = Bar(hud.transform,"PlayerHealthBar",new Vector2(-568,443),new Color(.33f,.82f,1f));
            var enemy = Bar(hud.transform,"EnemyHealthBar",new Vector2(568,443),new Color(1f,.22f,.32f));
            SetBar(player, new Vector2(-568,443));
            SetBar(enemy, new Vector2(568,443));
            Set(hud,"playerBar",player); Set(hud,"enemyBar",enemy);
            var playerHealth = HudText(hud.transform,"PlayerHealthLabel",new Vector2(-568,485),new Vector2(425,45),24);
            var enemyHealth = HudText(hud.transform,"EnemyHealthLabel",new Vector2(568,485),new Vector2(425,45),24);
            playerHealth.alignment = TextAlignmentOptions.Left;
            enemyHealth.alignment = TextAlignmentOptions.Right;
            var brand = HudText(hud.transform,"CenterBrand",new Vector2(0,480),new Vector2(520,85),49);
            brand.fontStyle = FontStyles.Bold;
            var playerPortrait = hud.transform.Find("PlayerPortrait")?.GetComponent<Image>();
            if (playerPortrait == null)
            {
                var portrait = new GameObject("PlayerPortrait",typeof(RectTransform),typeof(Image));
                portrait.transform.SetParent(hud.transform,false);
                playerPortrait = portrait.GetComponent<Image>();
            }
            var playerRect = (RectTransform)playerPortrait.transform;
            playerRect.anchorMin = playerRect.anchorMax = new Vector2(0,1);
            playerRect.sizeDelta = new Vector2(112,112);
            playerRect.anchoredPosition = new Vector2(70,-80);
            playerPortrait.preserveAspect = true;
            playerPortrait.raycastTarget = false;
            var enemyPortrait = hud.transform.Find("OpponentPortrait")?.GetComponent<Image>();
            if (enemyPortrait != null)
            {
                var enemyRect=(RectTransform)enemyPortrait.transform;
                enemyRect.anchorMin=enemyRect.anchorMax=new Vector2(1,1);
                enemyRect.sizeDelta=new Vector2(112,112);
                enemyRect.anchoredPosition=new Vector2(-70,-80);
                enemyPortrait.preserveAspect=true;
            }
            Set(hud,"playerPortrait",playerPortrait);
            Set(hud,"playerHealthLabel",playerHealth);
            Set(hud,"enemyHealthLabel",enemyHealth);
            Set(hud,"centerBrand",brand);
        }
        private static void SetBar(Image image,Vector2 position)
        {
            var rect=(RectTransform)image.transform.parent;
            rect.anchoredPosition=position;
            rect.sizeDelta=new Vector2(405,11);
        }
        private static TMP_Text HudText(Transform parent,string name,Vector2 position,Vector2 size,float font)
        {
            var label=parent.Find(name)?.GetComponent<TMP_Text>();
            if(label==null)label=Text(parent,name,"",position,size,font);
            var rect=(RectTransform)label.transform;
            rect.anchoredPosition=position;
            rect.sizeDelta=size;
            label.fontSize=font;
            label.raycastTarget=false;
            return label;
        }
        private static Image Bar(Transform parent,string name,Vector2 position,Color tint)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.Find("Fill").GetComponent<Image>();
            var back = new GameObject(name,typeof(RectTransform),typeof(Image));
            back.transform.SetParent(parent,false);
            ((RectTransform)back.transform).anchoredPosition = position;
            ((RectTransform)back.transform).sizeDelta = new Vector2(285,8);
            back.GetComponent<Image>().color = new Color(.05f,.08f,.13f,.9f);
            back.GetComponent<Image>().raycastTarget = false;
            var fill = new GameObject("Fill",typeof(RectTransform),typeof(Image));
            fill.transform.SetParent(back.transform,false);
            var rect = (RectTransform)fill.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = fill.GetComponent<Image>();
            image.color = tint; image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.raycastTarget = false;
            return image;
        }

        private static void EnsureGallery(GameObject menu,Button openButton)
        {
            var gallery = menu.GetComponent<OpponentGallery>();
            if (gallery == null) gallery = menu.AddComponent<OpponentGallery>();
            var root = menu.transform.Find("GalleryPanel");
            if (root == null)
            {
                root = Panel(menu.transform,"GalleryPanel",new Vector2(1100,730),new Color(.012f,.02f,.038f,.98f)).transform;
                Text(root,"GalleryHeading","OPPONENT GALLERY",new Vector2(0,300),new Vector2(800,70),45);
                var portraitGo = new GameObject("Portrait",typeof(RectTransform),typeof(Image));
                portraitGo.transform.SetParent(root,false);
                ((RectTransform)portraitGo.transform).anchoredPosition = new Vector2(0,100);
                ((RectTransform)portraitGo.transform).sizeDelta = new Vector2(230,230);
                portraitGo.GetComponent<Image>().raycastTarget = false;
                Text(root,"OpponentName","",new Vector2(0,-50),new Vector2(900,65),48);
                Text(root,"Details","",new Vector2(0,-130),new Vector2(900,90),25);
                EnsureButton(root,"Previous","PREVIOUS",new Vector2(-320,-260),24);
                EnsureButton(root,"Next","NEXT",new Vector2(320,-260),24);
                EnsureButton(root,"Practice","PRACTICE",new Vector2(0,-260),24);
                EnsureButton(root,"Close","CLOSE",new Vector2(425,300),22);
            }
            var group = root.GetComponent<CanvasGroup>();
            if (group == null) group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.interactable = false; group.blocksRaycasts = false;
            Set(gallery,"panel",group); Set(gallery,"openButton",openButton);
            Set(gallery,"closeButton",root.Find("Close")?.GetComponent<Button>());
            Set(gallery,"previousButton",root.Find("Previous")?.GetComponent<Button>());
            Set(gallery,"nextButton",root.Find("Next")?.GetComponent<Button>());
            Set(gallery,"practiceButton",root.Find("Practice")?.GetComponent<Button>());
            Set(gallery,"title",root.Find("OpponentName")?.GetComponent<TMP_Text>());
            Set(gallery,"details",root.Find("Details")?.GetComponent<TMP_Text>());
            Set(gallery,"portrait",root.Find("Portrait")?.GetComponent<Image>());
        }
        private static RectTransform Panel(Transform parent,string name,Vector2 size,Color color)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Image));
            go.transform.SetParent(parent,false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rect;
        }
        private static Button EnsureButton(Transform parent,string name,string label,Vector2 position,float fontSize)
        {
            var child = parent.Find(name);
            if (child != null) return child.GetComponent<Button>();
            var panel = Panel(parent,name,new Vector2(350,64),new Color(.10f,.16f,.23f,.96f));
            panel.anchoredPosition = position;
            var button = panel.gameObject.AddComponent<Button>();
            Text(panel,"Label",label,Vector2.zero,new Vector2(340,60),fontSize);
            return button;
        }
        private static TMP_Text Text(Transform parent,string name,string value,Vector2 position,Vector2 size,float fontSize)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));
            go.transform.SetParent(parent,false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size; rect.anchoredPosition = position;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Font);
            text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.87f,.91f,.96f);
            text.raycastTarget = false; text.text = value;
            return text;
        }
        private static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException("Missing field " + field);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
