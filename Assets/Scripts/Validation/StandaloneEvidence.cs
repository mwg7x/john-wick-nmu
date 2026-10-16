using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LastStep.AI;
using LastStep.Core;
using LastStep.Player;
using LastStep.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace LastStep.Validation
{
    // Opt-in deterministic standalone evidence harness. Normal launches do not
    // create it. The report labels scripted movement and disabled enemy attacks.
    public sealed class StandaloneEvidence : MonoBehaviour
    {
        string folder;
        int map;
        Keyboard keyboard;
        bool recording;
        int frame;
        readonly List<float> frameTimes = new List<float>();
        bool measuring;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            var args=Environment.GetCommandLineArgs();
            if(!Array.Exists(args,a=>a=="-duel-evidence")) return;
            var go=new GameObject("StandaloneEvidence");DontDestroyOnLoad(go);
            var harness=go.AddComponent<StandaloneEvidence>();
            harness.map=Array.Exists(args,a=>a=="-nmu")?1:0;
        }
        IEnumerator Start()
        {
            Application.runInBackground=true; QualitySettings.vSyncCount=0; Application.targetFrameRate=-1;
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            folder=Path.Combine(Application.dataPath,"../Evidence",map==1?"NMU":"Staircase");Directory.CreateDirectory(folder);
            keyboard=InputSystem.AddDevice<Keyboard>("EvidenceKeyboard");
            yield return Until(()=>GameManager.Instance!=null&&SceneManager.GetActiveScene().name=="MainMenu");
            var game=GameManager.Instance;game.SetMap(map);game.SetQuickDuel(false);game.SetDifficulty(0);game.SetPistol(0);
            UnityEngine.Random.InitState(4617);game.StartDuel();
            yield return Until(()=>!game.IsLoading&&FindAnyObjectByType<CoinTossFlow>()?.IsAwaitingSide==true);
            var coin=FindAnyObjectByType<CoinTossFlow>();coin.ChooseSide(0);
            yield return Until(()=>coin.IsAwaitingWeapon);coin.ChooseWeapon(0);if(coin.IsAwaitingWeapon)coin.ChooseWeapon(1);
            var markers=FindAnyObjectByType<DuelPositionMarkers>();var duel=FindAnyObjectByType<DuelManager>();
            var weapon=markers.PlayerStandIn.GetComponent<WeaponController>();
            markers.EnemyStandIn.GetComponent<EnemyAI>().enabled=false;
            yield return Until(()=>duel.IsFireAllowed);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"round1.png"));
            if(map==1)
            {
                var cam=Camera.main;var follow=cam.GetComponent<ThirdPersonCamera>();
                var position=cam.transform.position;var rotation=cam.transform.rotation;
                follow.enabled=false;cam.transform.position=new Vector3(0,2.6f,-15);cam.transform.LookAt(new Vector3(0,6,32));
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(folder,"reference-view.png"));
                yield return new WaitForEndOfFrame();
                cam.transform.SetPositionAndRotation(position,rotation);follow.enabled=true;
            }
            measuring=true;float end=Time.realtimeSinceStartup+8f;
            while(Time.realtimeSinceStartup<end) yield return null;
            measuring=false;
            for(int i=0;i<3;i++) {weapon.TryFire(weapon.Muzzle.position,Vector3.up);yield return new WaitForSeconds(weapon.Data.FireCooldown+.05f);}
            yield return Until(()=>duel.CurrentState==GameState.Transition);
            recording=true;StartCoroutine(CaptureFrames());
            Keys(Key.W);
            yield return Until(()=>duel.CurrentState!=GameState.Transition);
            Keys();
            yield return Until(()=>duel.IsFireAllowed);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"round2.png"));
            float started=Time.time;
            while(Time.time-started<5f&&duel.IsFireAllowed)
            {
                float t=Time.time-started;
                Keys(t<1.1f?Key.D:t<2.2f?Key.A:t<3.3f?Key.W:Key.S);
                if(t>.8f&&weapon.Ammunition==3) weapon.TryFire(weapon.Muzzle.position,Vector3.up);
                if(t>2.2f&&weapon.Ammunition==2) weapon.TryFire(weapon.Muzzle.position,Vector3.up);
                if(t>3.6f&&weapon.Ammunition==1) weapon.TryFire(weapon.Muzzle.position,Vector3.up);
                yield return null;
            }
            Keys();recording=false;
            frameTimes.Sort();
            float total=0;foreach(float t in frameTimes)total+=t;
            string report="Standalone smoke: reached round 2 on "+SceneManager.GetActiveScene().name+
                "\nResolution: "+Screen.width+"x"+Screen.height+"\nGPU: "+SystemInfo.graphicsDeviceName+
                "\nCPU: "+SystemInfo.processorType+"\nRAM MB: "+SystemInfo.systemMemorySize+
                "\nUncapped rendered frames (capture disabled): "+frameTimes.Count+
                "\nMean FPS: "+(frameTimes.Count/Mathf.Max(.001f,total)).ToString("F1")+
                "\n95th percentile frame ms: "+(frameTimes[Mathf.Min(frameTimes.Count-1,(int)(frameTimes.Count*.95f))]*1000f).ToString("F2")+
                "\nVideo JPEG frames: "+frame+" at approximately 20fps, frame capture has overhead."+
                "\nScripted evidence: virtual keyboard uses real PlayerMovement; enemy attacks disabled; shots deliberately aimed upward. This is not manual gameplay acceptance or a worst-case combat benchmark.";
            File.WriteAllText(Path.Combine(folder,"smoke-performance.txt"),report);
            InputSystem.RemoveDevice(keyboard);keyboard=null;
            game.ReturnToMainMenu();yield return Until(()=>!game.IsLoading&&SceneManager.GetActiveScene().name=="MainMenu");
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"menu.png"));
            yield return new WaitForSecondsRealtime(.5f);Application.Quit();
        }
        void Update() { if(measuring) frameTimes.Add(Time.unscaledDeltaTime); }
        void Keys(params Key[] keys) {InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));}
        IEnumerator Until(Func<bool> condition)
        {
            float deadline=Time.realtimeSinceStartup+60;
            while(!condition()) { if(Time.realtimeSinceStartup>deadline) {File.WriteAllText(Path.Combine(folder,"FAILED.txt"),"Timeout at "+SceneManager.GetActiveScene().name);Application.Quit(1);yield break;}yield return null; }
        }
        IEnumerator CaptureFrames()
        {
            Directory.CreateDirectory(Path.Combine(folder,"Frames"));
            while(recording)
            {
                yield return new WaitForEndOfFrame();
                var texture=ScreenCapture.CaptureScreenshotAsTexture();
                File.AppendAllText(Path.Combine(folder,"frame-times.txt"),frame+","+Time.realtimeSinceStartup.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+"\n");
                File.WriteAllBytes(Path.Combine(folder,"Frames",(frame++).ToString("D5")+".jpg"),texture.EncodeToJPG(80));Destroy(texture);
                yield return new WaitForSecondsRealtime(.05f);
            }
        }
        void OnDestroy() {if(keyboard!=null)InputSystem.RemoveDevice(keyboard);}
    }
}
