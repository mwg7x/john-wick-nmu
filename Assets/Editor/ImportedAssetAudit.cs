using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
namespace LastStep.Editor {
public static class ImportedAssetAudit {
 [MenuItem("LAST STEP/Audit Imported Models")]
 public static void Run() {
 var sb=new StringBuilder();
 foreach(var path in new[]{"Assets/Art/MakeHuman/Suited/suited.glb","Assets/Art/MakeHuman/Man/man.glb","Assets/Art/Kenney/Protagonists/Model/characterMedium.fbx","Assets/Art/Kenney/Protagonists/Animations/idle.fbx","Assets/Resources/Pistols/Pistol0.prefab","Assets/Resources/Pistols/Pistol1.prefab","Assets/Resources/Pistols/Pistol2.prefab","Assets/Art/PolyPizza/Balanced/Pistol_5.fbx","Assets/Art/PolyPizza/Fast/Pistol.fbx","Assets/Art/PolyPizza/Powerful/source/1911.fbx"}){
 var go=AssetDatabase.LoadAssetAtPath<GameObject>(path); sb.AppendLine(path+" gameobject="+(go!=null));
 if(go!=null){foreach(var t in go.GetComponentsInChildren<Transform>(true))sb.AppendLine("T "+t.name+" "+t.localPosition+" "+t.localScale); foreach(var r in go.GetComponentsInChildren<Renderer>(true)){sb.AppendLine("R "+r.name+" "+r.GetType().Name+" "+r.bounds.size);foreach(var m in r.sharedMaterials)sb.AppendLine("M "+(m?m.name:"null")+" shader="+(m&&m.shader?m.shader.name:"none"));}}
 foreach(var o in AssetDatabase.LoadAllAssetsAtPath(path)) if(o is AnimationClip c)sb.AppendLine("Clip "+c.name+" "+c.length);
 var mi=AssetImporter.GetAtPath(path) as ModelImporter; if(mi!=null) sb.AppendLine("rig="+mi.animationType+" globalScale="+mi.globalScale);
 }
 File.WriteAllText("Logs/imported-model-audit.txt",sb.ToString()); Debug.Log(sb.ToString());
 }
}}
