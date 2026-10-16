using System;
using System.IO;
using System.Reflection;
using LastStep.Core;
using LastStep.Player;
using LastStep.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LastStep.Editor
{
    // Focused Play Mode fixtures use real state/weapon/input components and cue
    // state, never fixed sleeps. No production balance assets are mutated.
    [InitializeOnLoad]
    public static class DuelInvariantValidation
    {
        const string Key="LastStep.Invariants.";
        static GameObject fixture;
        static DuelSettings settings;
        static DuelManager duel;
        static CountdownController countdown;
        static WeaponController weapon;
        static PCInputController input;
        static int shots, fires, checks;
        static int uiFrame;
        static DuelInvariantValidation() { EditorApplication.update+=Tick; }
        public static void Run()
        {
            SessionState.SetBool(Key+"running",true);SessionState.SetInt(Key+"case",0);
            SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+90);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
        static void Set(object obj,string field,object value) => obj.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
        static void Check(bool value,string message) { if(!value) throw new Exception(message); checks++; }
        static void Create()
        {
            fixture=new GameObject("InvariantFixture");fixture.SetActive(false);
            settings=ScriptableObject.CreateInstance<DuelSettings>();Set(settings,"countdownDuration",.08f);
            duel=fixture.AddComponent<DuelManager>();Set(duel,"settings",settings);
            countdown=fixture.AddComponent<CountdownController>();Set(countdown,"duel",duel);Set(countdown,"prepareFirstRound",false);
            var health=fixture.AddComponent<Health>();health.ResetHealth(500);
            var falseStart=fixture.AddComponent<FalseStartManager>();Set(falseStart,"duel",duel);
            weapon=fixture.AddComponent<WeaponController>();Set(weapon,"duel",duel);Set(weapon,"owner",health);Set(weapon,"playerWeapon",true);Set(weapon,"data",PistolCatalog.Load(0));
            input=fixture.AddComponent<PCInputController>();Set(input,"duel",duel);Set(input,"falseStarts",falseStart);
            Set(input,"actions",AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/LastStep.inputactions"));
            shots=0;fires=0;weapon.ShotFired+=(hit,point)=>shots++;input.FirePressed+=()=>fires++;
            fixture.SetActive(true);
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key+"running",false)) return;
            if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0)) { Finish("Timeout");return; }
            if(!EditorApplication.isPlaying) return;
            try
            {
                if(fixture==null) {Create();return;}
                if(!duel.IsInitialized) return;
                int test=SessionState.GetInt(Key+"case",0);
                if(test==7)
                {
                    if(Time.frameCount==uiFrame) return;
                    Canvas.ForceUpdateCanvases();
                    var ui=GameObject.Find("TestUI");
                    var rect=ui.transform.Find("Button").GetComponent<RectTransform>();
                    Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.position);
                    Check(input.IsPointerOverUI(point),"UI click not filtered");
                    ui.GetComponent<CanvasGroup>().blocksRaycasts=false;
                    Check(!input.IsPointerOverUI(point),"Hidden UI blocks gameplay");
                    UnityEngine.Object.DestroyImmediate(ui);UnityEngine.Object.DestroyImmediate(GameObject.Find("Events"));
                    Finish("");return;
                }
                if(test<6)
                {
                    if(duel.CurrentState==GameState.Intro) {Check(duel.TryPrepareRound(1,0),"prepare early-fire fixture");return;}
                    var cue=new[]{CountdownCue.Three,CountdownCue.Two,CountdownCue.One}[test%3];
                    if(duel.CurrentState!=GameState.Countdown||countdown.CurrentCue!=cue) return;
                    if(test>=3) typeof(DuelManager).GetProperty("PlayerRoundAmmo").SetValue(duel,0);
                    Set(weapon,"nextShotTime",Time.time+100f);
                    Check(!weapon.TryFire(Vector3.zero,Vector3.zero),"Countdown accepted invalid-direction shot");
                    Check(duel.CurrentState==GameState.Defeat,"Early fire did not defeat at "+cue+" zeroAmmo="+(test>=3));
                    Check(shots==0,"Early fire emitted accepted-shot event");
                    Cleanup();SessionState.SetInt(Key+"case",test+1);return;
                }
                if(duel.CurrentState!=GameState.Intro) return;
                countdown.enabled=false;
                input.SubmitGameplayFire();Check(fires==0,"Intro forwarded fire");
                Check(!weapon.TryFire(Vector3.zero,Vector3.forward),"Intro fired");
                for(int round=1;round<=3;round++)
                {
                    Check(!duel.TryPrepareRound(round,round%3),"Mismatched checkpoint accepted");
                    Check(duel.TryPrepareRound(round,round-1),"Valid round rejected");
                    Check(duel.PlayerRoundAmmo==3&&duel.EnemyRoundAmmo==3,"Fresh allocation missing");
                    Check(!duel.TryPrepareRound(round,round-1),"Repeated preparation accepted");
                    Check(duel.TryStartCountdown(),"Countdown rejected");
                    Check(duel.TryAllowFire(),"Fire rejected");
                    Check(fires==0,"Earlier input queued across state boundary");
                    int beforeShots=shots;
                    for(int shot=0;shot<3;shot++)
                    {
                        weapon.Equip(shot); // exercises equip without refilling, resets cooldown for fixture
                        Check(weapon.Ammunition==3-shot,"Equip refilled ammunition");
                        Check(!weapon.TryReload(),"Reload was allowed");
                        Check(weapon.TryFire(new Vector3(0,100,0),Vector3.up),"Legal shot rejected");
                        Check(weapon.Ammunition==2-shot&&shots==beforeShots+shot+1,"Shot/event count diverged");
                    }
                    weapon.Equip(0);
                    Check(!weapon.TryFire(Vector3.zero,Vector3.up)&&duel.PlayerRoundAmmo==0,"Fourth shot accepted");
                    Check(duel.TryBeginResolution(),"Resolution rejected");
                    input.SubmitGameplayFire();Check(fires==0,"Resolution forwarded fire");
                    Check(duel.TryBeginTransition(),"Transition rejected");
                    input.SubmitGameplayFire();Check(fires==0,"Transition forwarded fire");
                }
                Check(!duel.TryPrepareRound(4,2),"Fourth round accepted");
                Check(!duel.TryPrepareRound(4,3),"Fourth checkpoint accepted");
                // Actual GraphicRaycaster hit filtering, including an invisible panel.
                var events=new GameObject("Events",typeof(EventSystem));
                var panel=new GameObject("TestUI",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(CanvasGroup));
                panel.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
                var button=new GameObject("Button",typeof(RectTransform),typeof(Image),typeof(Button));
                button.transform.SetParent(panel.transform,false);button.GetComponent<RectTransform>().sizeDelta=new Vector2(200,200);
                Canvas.ForceUpdateCanvases();
                uiFrame=Time.frameCount;SessionState.SetInt(Key+"case",7);
            }
            catch(Exception e) {Finish(e.ToString());}
        }
        static void Cleanup() { if(fixture!=null) UnityEngine.Object.DestroyImmediate(fixture);if(settings!=null) UnityEngine.Object.DestroyImmediate(settings);fixture=null; }
        static void Finish(string error)
        {
            SessionState.SetBool(Key+"running",false);Cleanup();Time.timeScale=1;
            Directory.CreateDirectory("Documentation/Validation");
            File.WriteAllText("Documentation/Validation/duel-invariants.txt",(error.Length==0?"PASS":"FAIL")+"\nChecks: "+checks+"\n"+error);
            if(Application.isBatchMode) EditorApplication.Exit(error.Length==0?0:1);else EditorApplication.ExitPlaymode();
        }
    }
}
