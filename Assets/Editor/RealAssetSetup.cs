using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace LastStep.Editor {
public static class RealAssetSetup {
 public static void Run(){
  const string root="Assets/Resources/Characters";
  if(!AssetDatabase.IsValidFolder("Assets/Resources/Characters")) AssetDatabase.CreateFolder("Assets/Resources","Characters");
  AssetDatabase.DeleteAsset(root+"/Human.controller");
  var controller=AnimatorController.CreateAnimatorControllerAtPath(root+"/Human.controller");
  controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
  var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Kenney/Protagonists/Animations/idle.fbx");
  var run=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Kenney/Protagonists/Animations/run.fbx");
  foreach(var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Kenney/Protagonists/Animations/idle.fbx")) if(obj is AnimationClip c && c.name!="__preview__") idle=c;
  foreach(var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Kenney/Protagonists/Animations/run.fbx")) if(obj is AnimationClip c && c.name!="__preview__") run=c;
  var sm=controller.layers[0].stateMachine;
  var layers=controller.layers;layers[0].iKPass=true;controller.layers=layers;
  var idleState=sm.AddState("Idle");idleState.motion=idle; sm.defaultState=idleState;
  var runState=sm.AddState("Run");runState.motion=run;
  var toRun=idleState.AddTransition(runState);toRun.hasExitTime=false;toRun.duration=.15f;toRun.AddCondition(AnimatorConditionMode.Greater,.08f,"Speed");
  var toIdle=runState.AddTransition(idleState);toIdle.hasExitTime=false;toIdle.duration=.15f;toIdle.AddCondition(AnimatorConditionMode.Less,.08f,"Speed");
  var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Kenney/Protagonists/Model/characterMedium.fbx");
  var instance=(GameObject)PrefabUtility.InstantiatePrefab(source);instance.name="RiggedHuman";instance.transform.localScale=Vector3.one*.47f;
  var animator=instance.GetComponent<Animator>();if(animator==null) animator=instance.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
  PrefabUtility.SaveAsPrefabAsset(instance,root+"/RiggedHuman.prefab");Object.DestroyImmediate(instance);
  AssetDatabase.SaveAssets(); Debug.Log("Real character prefab ready; idle="+(idle?idle.name:"missing")+", run="+(run?run.name:"missing"));
 }
}}
