using UnityEditor;
using UnityEngine;
using System.IO;
namespace LastStep.Editor {
public static class ImportedRigSetup {
 public static void Run(){
  foreach(var path in new[]{"Assets/Art/Kenney/Protagonists/Model/characterMedium.fbx","Assets/Art/Kenney/Protagonists/Animations/idle.fbx","Assets/Art/Kenney/Protagonists/Animations/run.fbx","Assets/Art/Kenney/Protagonists/Animations/jump.fbx"}){
   var importer=(ModelImporter)AssetImporter.GetAtPath(path); importer.animationType=ModelImporterAnimationType.Human; importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel; importer.SaveAndReimport();
   var go=AssetDatabase.LoadAssetAtPath<GameObject>(path); var a=go.GetComponent<Animator>(); Debug.Log(path+" avatarValid="+(a&&a.avatar&&a.avatar.isValid)+" human="+(a&&a.avatar&&a.avatar.isHuman));
  }
  AssetDatabase.SaveAssets();
 }
}}
