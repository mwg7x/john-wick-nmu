using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace LastStep.Editor {
public static class MakeHumanSetup {
 public static void Run(){
  const string sourcePath="Assets/Art/MakeHuman/Suited/suited.glb";
  const string folder="Assets/Resources/Characters";
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
  if(source==null)throw new System.Exception("MakeHuman GLB did not import.");
  var clips=AssetDatabase.LoadAllAssetsAtPath(sourcePath).OfType<AnimationClip>().ToArray();
  var idle=clips.FirstOrDefault(c=>c.name=="idle");var run=clips.FirstOrDefault(c=>c.name=="run");
  if(idle==null||run==null)throw new System.Exception("MakeHuman animations missing.");
  string controllerPath=folder+"/RealHuman.controller";
  AssetDatabase.DeleteAsset(controllerPath);
  var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
  controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
  var sm=controller.layers[0].stateMachine;var idleState=sm.AddState("Idle");idleState.motion=idle;sm.defaultState=idleState;
  var runState=sm.AddState("Run");runState.motion=run;
  var toRun=idleState.AddTransition(runState);toRun.hasExitTime=false;toRun.duration=.18f;toRun.AddCondition(AnimatorConditionMode.Greater,.10f,"Speed");
  var toIdle=runState.AddTransition(idleState);toIdle.hasExitTime=false;toIdle.duration=.18f;toIdle.AddCondition(AnimatorConditionMode.Less,.10f,"Speed");
  var root=new GameObject("RealSuited");
  var orientation=new GameObject("Orientation").transform;orientation.SetParent(root.transform,false);
  orientation.localRotation=Quaternion.identity;
  var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(orientation,false);model.transform.localRotation=Quaternion.identity;
  var rs=model.GetComponentsInChildren<SkinnedMeshRenderer>();
  Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
  float scale=1.86f/Mathf.Max(.01f,b.size.y);model.transform.localScale=Vector3.one*scale;
  b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
  model.transform.localPosition=new Vector3(-b.center.x,-b.min.y,-b.center.z);
  var animator=model.GetComponent<Animator>();if(animator==null)animator=model.AddComponent<Animator>();
  animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
  PrefabUtility.SaveAsPrefabAsset(root,folder+"/RealSuited.prefab");
  Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
  Debug.Log("MakeHuman suited prefab ready height="+b.size.y+" renderers="+rs.Length+" clips="+clips.Length);
 }
}}
