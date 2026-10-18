using System.Collections;
using LastStep.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastStep.Core
{
    [DisallowMultipleComponent]
    public sealed class CoinTossFlow : MonoBehaviour
    {
        [SerializeField] private DuelManager duel;
        [SerializeField] private WeaponController playerWeapon;
        [SerializeField] private WeaponController enemyWeapon;
        private Canvas canvas;
        private TMP_Text title;
        private TMP_Text briefing;
        private Button[] buttons;
        private Transform coin;
        private AudioSource sound;
        private Texture2D headsArt, tailsArt;
        private int choice = -1;
        private int result = -1;
        private int enemyChoice = -1;
        private bool awaitingWeapon;
        private bool completed;
        public int CoinResult => result;
        public bool PlayerWon => choice == result && result >= 0;
        public bool IsAwaitingSide => !completed && choice < 0;
        public bool IsAwaitingWeapon => awaitingWeapon;

        private IEnumerator Start()
        {
            while (duel != null && duel.CurrentState == GameState.Boot) yield return null;
            if (duel == null || duel.CurrentState != GameState.Intro) yield break;
            BuildInterface();
            title.text = "THE DUEL BEGINS";
            var game = GameManager.Instance;
            string rival = game != null ? game.CurrentOpponent.Name.ToUpperInvariant() : "RIVAL";
            string tier = game != null && game.SelectedDifficulty != null
                ? game.SelectedDifficulty.DifficultyName.ToUpperInvariant() : "DUEL";
            briefing.text = (game != null && game.QuickDuel ? "QUICK DUEL" : "LEVEL CAMPAIGN") +
                "  /  " + rival + "  /  " + tier +
                "\n50 METERS  •  THREE SHOTS EACH ROUND  •  NO RELOAD\nCALL THE COIN TO CHOOSE YOUR PISTOL";
            SetButton(0, "HEADS", () => ChooseSide(0), true);
            SetButton(1, "TAILS", () => ChooseSide(1), true);
            SetButton(2, "", null, false);
        }

        public bool ChooseSide(int side)
        {
            if (duel == null || duel.CurrentState != GameState.Intro || choice >= 0 || (side != 0 && side != 1)) return false;
            choice = side;
            result = Random.Range(0, 2);
            foreach (var button in buttons) button.gameObject.SetActive(false);
            if (coin != null) coin.gameObject.SetActive(true);
            StartCoroutine(Flip());
            return true;
        }
        private IEnumerator Flip()
        {
            title.text = "THE COIN IS IN THE AIR";
            if (sound != null) { sound.pitch = 1.6f; sound.volume = .2f; sound.Play(); }
            float elapsed = 0f;
            while (elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                if (coin != null) coin.localRotation = Quaternion.Euler(90f + elapsed * 1000f, elapsed * 85f, 0f);
                yield return null;
            }
            if (coin != null) coin.localRotation = Quaternion.Euler(result == 0 ? 90f : 270f, 0f, 0f);
            if (sound != null) { sound.pitch = .82f; sound.volume = .8f; sound.Play(); }
            title.text = (result == 0 ? "HEADS" : "TAILS") + "\n" +
                (PlayerWon ? "YOU WON THE TOSS - CHOOSE FIRST" : "OPPONENT WON THE TOSS");
            yield return new WaitForSeconds(.8f);
            if (coin != null) coin.gameObject.SetActive(false);
            if (!PlayerWon)
            {
                enemyChoice = Random.Range(0, PistolCatalog.Count);
                enemyWeapon.Equip(enemyChoice);
                title.text = "OPPONENT SELECTED " + PistolCatalog.Name(enemyChoice).ToUpperInvariant() +
                    "\nCHOOSE FROM THE REMAINING PISTOLS";
            }
            ShowWeaponChoices();
        }
        private void ShowWeaponChoices()
        {
            awaitingWeapon = true;
            for (int index = 0; index < PistolCatalog.Count; index++)
            {
                var rect=(RectTransform)buttons[index].transform;
                rect.anchoredPosition=new Vector2((index-1)*405f,-205f);
                rect.sizeDelta=new Vector2(375f,116f);
                var art=buttons[index].transform.Find("CoinArt");
                if(art!=null)art.gameObject.SetActive(false);
                var label=buttons[index].GetComponentInChildren<TMP_Text>();
                if(label!=null)
                {
                    ((RectTransform)label.transform).anchoredPosition=Vector2.zero;
                    ((RectTransform)label.transform).sizeDelta=new Vector2(360f,106f);
                    label.fontSize=22f;
                }
                int selected = index;
                SetButton(index, PistolCatalog.Name(index).ToUpperInvariant() + "\n" + PistolCatalog.Summary(index),
                    () => ChooseWeapon(selected), index != enemyChoice);
            }
        }
        public bool ChooseWeapon(int index)
        {
            if (!awaitingWeapon || completed || index < 0 || index >= PistolCatalog.Count || index == enemyChoice) return false;
            awaitingWeapon = false;
            completed = true;
            GameManager.Instance?.SetPistol(index);
            playerWeapon.Equip(index);
            if (enemyChoice < 0)
            {
                int pick = Random.Range(0, PistolCatalog.Count - 1);
                enemyChoice = pick >= index ? pick + 1 : pick;
                enemyWeapon.Equip(enemyChoice);
            }
            foreach (var button in buttons) button.gameObject.SetActive(false);
            title.text = "YOU: " + PistolCatalog.Name(index).ToUpperInvariant() + "   |   " +
                (GameManager.Instance != null ? GameManager.Instance.CurrentOpponent.Name.ToUpperInvariant() : "RIVAL") +
                ": " + PistolCatalog.Name(enemyChoice).ToUpperInvariant();
            briefing.text = "THREE SHOTS EACH ROUND. MAKE EACH ONE COUNT.";
            StartCoroutine(BeginRound());
            return true;
        }
        private IEnumerator BeginRound()
        {
            yield return new WaitForSeconds(.9f);
            if (canvas != null) canvas.gameObject.SetActive(false);
            if (coin != null) Destroy(coin.gameObject);
            if (duel != null && duel.CurrentState == GameState.Intro) duel.TryPrepareRound(1, 0);
        }

        private void BuildInterface()
        {
            var root = new GameObject("CoinTossCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var veil = new GameObject("Veil", typeof(RectTransform), typeof(Image));
            veil.transform.SetParent(root.transform, false);
            var veilRect = (RectTransform)veil.transform;
            veilRect.anchorMin = Vector2.zero; veilRect.anchorMax = Vector2.one;
            veilRect.offsetMin = veilRect.offsetMax = Vector2.zero;
            veil.GetComponent<Image>().color = new Color(.008f, .016f, .030f, .16f);
            Accent(root.transform, new Vector2(0f, 428f), new Vector2(560f, 3f), new Color(.72f, .78f, .85f, .75f));
            title = Label(root.transform, new Vector2(0f, 385f), new Vector2(1200f, 70f), 43);
            title.color = new Color(.96f, .95f, .90f);
            briefing = Label(root.transform, new Vector2(0f, 289f), new Vector2(1360f, 106f), 22);
            briefing.color = new Color(.77f, .85f, .91f);
            headsArt = CoinTexture(true);
            tailsArt = CoinTexture(false);
            buttons = new Button[3];
            for (int i = 0; i < buttons.Length; i++)
            {
                var buttonObject = new GameObject("Choice" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(root.transform, false);
                var rect = (RectTransform)buttonObject.transform;
                rect.anchoredPosition = new Vector2((i - .5f) * 410f, -205f);
                rect.sizeDelta = new Vector2(390f, 116f);
                buttonObject.GetComponent<Image>().color = i==0 ? new Color(.37f,.025f,.05f,.97f) : new Color(.045f,.06f,.075f,.97f);
                var outline = buttonObject.AddComponent<Outline>();
                outline.effectColor = i==0 ? new Color(1f,.1f,.2f,.95f) : new Color(.55f, .66f, .75f, .85f);
                outline.effectDistance = new Vector2(2f, -2f);
                buttons[i] = buttonObject.GetComponent<Button>();
                var colors=buttons[i].colors;
                colors.highlightedColor=new Color(.95f,.22f,.28f);
                colors.selectedColor=colors.highlightedColor;
                buttons[i].colors=colors;
                var caption=Label(buttonObject.transform, new Vector2(36f,0f),new Vector2(295f,100f),32);
                caption.fontStyle=FontStyles.Bold;
                if(i<2)
                {
                    var art=new GameObject("CoinArt",typeof(RectTransform),typeof(RawImage));
                    art.transform.SetParent(buttonObject.transform,false);
                    var artRect=(RectTransform)art.transform;
                    artRect.anchoredPosition=new Vector2(-122f,0f);
                    artRect.sizeDelta=new Vector2(84f,84f);
                    var raw=art.GetComponent<RawImage>();
                    raw.texture=i==0?headsArt:tailsArt;
                    raw.raycastTarget=false;
                }
            }
            var camera = Camera.main;
            if (camera != null)
            {
                coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
                coin.name = "TossCoin";
                coin.SetParent(camera.transform, false);
                coin.localPosition = new Vector3(0f, 0f, 2.6f);
                coin.localScale = new Vector3(.25f, .035f, .25f);
                coin.localRotation = Quaternion.Euler(90f, 0f, 0f);
                Destroy(coin.GetComponent<Collider>());
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", new Color(.82f, .57f, .19f));
                material.SetFloat("_Metallic", .8f);
                material.SetFloat("_Smoothness", .76f);
                coin.GetComponent<Renderer>().material = material;
                coin.gameObject.SetActive(false);
            }
            sound = gameObject.AddComponent<AudioSource>();
            var samples = new float[11025];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / 22050f;
                samples[i] = Mathf.Sin(t * Mathf.PI * 2f * 1180f) * Mathf.Exp(-8f * t) * .18f;
            }
            var clip = AudioClip.Create("Coin Land", samples.Length, 1, 22050, false);
            clip.SetData(samples, 0);
            sound.clip = Resources.Load<AudioClip>("Audio/KenneyImpact/impactMetal_heavy_000") ?? clip;
            sound.volume=.28f;
        }
        private static Texture2D CoinTexture(bool heads)
        {
            const int size=128;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float dx=(x-63.5f)/60f,dy=(y-63.5f)/60f;
                float radius=Mathf.Sqrt(dx*dx+dy*dy);
                if(radius>1f){pixels[y*size+x]=new Color32(0,0,0,0);continue;}
                float rim=Mathf.Abs(radius-.87f)<.045f?.38f:0f;
                float emboss=heads ? Mathf.Exp(-((dx+.03f)*(dx+.03f)*13f+(dy-.07f)*(dy-.07f)*5f))*.13f :
                    Mathf.Abs(dx)<.24f&&Mathf.Abs(dy)<.28f?.11f:0f;
                float shade=Mathf.Clamp01(.40f+(-dx+dy)*.21f+rim+emboss);
                pixels[y*size+x]=new Color(.33f+shade*.57f,.30f+shade*.49f,.25f+shade*.35f,1f);
            }
            texture.SetPixels32(pixels);texture.Apply();
            return texture;
        }
        private void OnDestroy()
        {
            if(headsArt!=null)Destroy(headsArt);
            if(tailsArt!=null)Destroy(tailsArt);
        }
        private static void Accent(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var strip = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            strip.transform.SetParent(parent, false);
            var rect = (RectTransform)strip.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = strip.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }
        private static TMP_Text Label(Transform parent, Vector2 position, Vector2 size, float fontSize)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var label = go.GetComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(.94f, .94f, .9f);
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }
        private void SetButton(int index, string caption, UnityEngine.Events.UnityAction callback, bool visible)
        {
            var button = buttons[index];
            button.gameObject.SetActive(visible);
            button.onClick.RemoveAllListeners();
            if (callback != null) button.onClick.AddListener(callback);
            button.GetComponentInChildren<TMP_Text>().text = caption;
        }
    }
}
