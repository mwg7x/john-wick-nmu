using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;
namespace LastStep.Editor {
public static class BaldManSetup {
 public static void Run(){
  const string fbx="Assets/Art/OpenGameArt/BaldMan/BaldMan_Painted_Rigged.fbx";
  const string folder="Assets/Resources/Characters";
  var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);
  importer.animationType=ModelImporterAnimationType.Human;
  importer.SaveAndReimport();
  var source=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
  var sourceAnimator=source.GetComponent<Animator>();
  if(sourceAnimator==null||sourceAnimator.avatar==null||!sourceAnimator.avatar.isValid||!sourceAnimator.avatar.isHuman)
   throw new System.Exception("Bald Man Humanoid avatar invalid.");
  string normalPath="Assets/Art/OpenGameArt/BaldMan/BaldMan_Default_Normal.png";
  var normalImporter=(TextureImporter)AssetImporter.GetAtPath(normalPath);
  if(normalImporter.textureType!=TextureImporterType.NormalMap){normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.SaveAndReimport();}
  string matPath="Assets/Art/OpenGameArt/BaldMan/BaldMan_URP.mat";
  var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
  if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,matPath);}
  mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/OpenGameArt/BaldMan/BaldMan_Default_AlbedoTransparency.png"));
  mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));mat.EnableKeyword("_NORMALMAP");
  mat.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(mat);
  string controllerPath=folder+"/BaldHuman.controller";AssetDatabase.DeleteAsset(controllerPath);
  var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
  var layers=controller.layers;layers[0].iKPass=true;controller.layers=layers;
  var idle=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Kenney/Protagonists/Animations/idle.fbx").OfType<AnimationClip>().First(c=>c.name=="Root|Idle");
  var run=AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Kenney/Protagonists/Animations/run.fbx").OfType<AnimationClip>().First(c=>c.name=="Root|Run");
  var sm=controller.layers[0].stateMachine;var idleState=sm.AddState("Idle");idleState.motion=idle;sm.defaultState=idleState;
  var runState=sm.AddState("Run");runState.motion=run;
  var toRun=idleState.AddTransition(runState);toRun.hasExitTime=false;toRun.duration=.18f;toRun.AddCondition(AnimatorConditionMode.Greater,.1f,"Speed");
  var toIdle=runState.AddTransition(idleState);toIdle.hasExitTime=false;toIdle.duration=.18f;toIdle.AddCondition(AnimatorConditionMode.Less,.1f,"Speed");
  var root=new GameObject("BaldVariant");var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
  foreach(var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())renderer.sharedMaterial=mat;
  var animator=model.GetComponent<Animator>();if(animator==null)animator=model.AddComponent<Animator>();
  animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
  var rs=model.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
  model.transform.localScale*=1.84f/Mathf.Max(.01f,b.size.y);
  b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
  model.transform.localPosition=new Vector3(-b.center.x,-b.min.y,-b.center.z);
  PrefabUtility.SaveAsPrefabAsset(root,folder+"/BaldVariant.prefab");Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
  Debug.Log("Bald Man Humanoid prefab ready height="+b.size.y+" avatar="+sourceAnimator.avatar.isValid);
 }
}}
