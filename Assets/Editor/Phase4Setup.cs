using System;
using System.IO;
using LastStep.Core;
using LastStep.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastStep.Editor
{
    [InitializeOnLoad]
    public static class Phase4Setup
    {
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string PendingKey = "LastStep.Phase4.ResourceImport";

        static Phase4Setup()
        {
            EditorApplication.update += () =>
            {
                int pending = SessionState.GetInt(PendingKey, 0);
                if (pending == 0 || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) == null) return;
                SessionState.SetInt(PendingKey, 0);
                if (pending == 2) Phase4Validation.Run();
                else Configure();
            };
        }

        public static bool EnsureResources(bool resumeValidation)
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null) return true;
            if (SessionState.GetInt(PendingKey, 0) != 0) return false;
            SessionState.SetInt(PendingKey, resumeValidation ? 2 : 1);
            string package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly).resolvedPath;
            UnityEditor.AssetPackage.Package.Import(Path.Combine(package, "Package Resources/TMP Essential Resources.unitypackage"), false);
            Debug.Log("LAST STEP: importing TMP essentials; Phase 4 resumes automatically when the font is ready.");
            return false;
        }

        [MenuItem("LAST STEP/Phase 4/Configure Countdown")]
        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before setup.");
            if (!EnsureResources(false)) return;
            Phase3Setup.Configure();
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Duel_Staircase") return;
            var duel = UnityEngine.Object.FindAnyObjectByType<DuelManager>();
            if (duel == null) throw new InvalidOperationException("Missing scene DuelManager.");
            var countdown = duel.GetComponent<CountdownController>();
            if (countdown == null) countdown = duel.gameObject.AddComponent<CountdownController>();
            Assign(countdown, "duel", duel);
            if (UnityEngine.Object.FindAnyObjectByType<CountdownView>() == null) CreateView(countdown);
            EditorSceneManager.SaveScene(duel.gameObject.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = duel.gameObject;
            Debug.Log("LAST STEP PHASE 4: countdown and TextMeshPro display configured.");
        }

        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property.objectReferenceValue == null) property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateView(CountdownController countdown)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidOperationException("TextMeshPro essential font import failed.");

            var canvasObject = new GameObject("CountdownCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var panel = new GameObject("CountdownPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panel.transform.SetParent(canvasObject.transform, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.7f);
            rect.sizeDelta = new Vector2(340, 254);
            var background = panel.GetComponent<Image>();
            background.color = new Color(0.025f, 0.03f, 0.045f, 0.76f);
            background.raycastTarget = false;
            var group = panel.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var round = Text(panel.transform, font, "Round", 81, 26);
            var distance = Text(panel.transform, font, "Distance", 39, 22);
            var cue = Text(panel.transform, font, "Cue", -43, 88);
            var view = panel.AddComponent<CountdownView>();
            Assign(view, "countdown", countdown);
            Assign(view, "group", group);
            Assign(view, "roundLabel", round);
            Assign(view, "distanceLabel", distance);
            Assign(view, "cueLabel", cue);
        }

        private static TMP_Text Text(Transform parent, TMP_FontAsset font, string name, float y, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(320, size + 20);
            rect.anchoredPosition = new Vector2(0, y);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.94f, 0.94f, 0.9f);
            text.raycastTarget = false;
            text.text = "";
            return text;
        }
    }
}
