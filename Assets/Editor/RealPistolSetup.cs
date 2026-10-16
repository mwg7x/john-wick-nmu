using UnityEditor;
using UnityEngine;
namespace LastStep.Editor {
public static class RealPistolSetup {
 public static void Run(){
  if(!AssetDatabase.IsValidFolder("Assets/Resources/Pistols")) AssetDatabase.CreateFolder("Assets/Resources","Pistols");
  foreach(var (source,index,turn) in new[]{
   ("Assets/Art/PolyPizza/BalancedAlt/Flat Guns East/Pistol_Full_East.fbx",0,false),
   ("Assets/Art/PolyPizza/Fast/Pistol.fbx",1,false),
   ("Assets/Art/PolyPizza/Powerful/source/1911.fbx",2,true)}){
   var original=AssetDatabase.LoadAssetAtPath<GameObject>(source);
   if(original==null){Debug.LogError("Missing "+source);continue;}
   var root=new GameObject("LicensedPistol"+index);var model=(GameObject)PrefabUtility.InstantiatePrefab(original);model.transform.SetParent(root.transform,false);
   // The powerful source mesh points along +X. -90 degrees maps its barrel
   // to the shared +Z muzzle direction used by the rig and shot effects.
   model.transform.localRotation=turn?Quaternion.Euler(0,-90,0):Quaternion.identity;
   var renderers=model.GetComponentsInChildren<Renderer>();
   var palette=new Material[3];
   for(int i=0;i<palette.Length;i++){
    string materialPath="Assets/Resources/Pistols/Pistol"+index+"_Material"+i+".mat";
    var m=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"));AssetDatabase.CreateAsset(m,materialPath);}
    m.color=i==0?new Color(.15f,.17f,.20f):i==1?new Color(.045f,.052f,.06f):new Color(.33f,.36f,.39f);
    m.SetFloat("_Smoothness",.55f);EditorUtility.SetDirty(m);palette[i]=m;
   }
   foreach(var r in renderers){var mats=new Material[r.sharedMaterials.Length];for(int i=0;i<mats.Length;i++){
    mats[i]=palette[Mathf.Min(i,palette.Length-1)];
   }r.sharedMaterials=mats;}
   AssetDatabase.SaveAssets();
   Bounds b=new Bounds();bool first=true;foreach(var r in renderers){if(first){b=r.bounds;first=false;}else b.Encapsulate(r.bounds);}
   float scale=.32f/Mathf.Max(b.size.x,b.size.y,b.size.z);
   model.transform.localScale=Vector3.one*scale;
   model.transform.localPosition=-b.center*scale;
   PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Pistols/Pistol"+index+".prefab");
   Debug.Log("Pistol"+index+" source="+source+" bounds="+b.size+" scale="+scale);
   Object.DestroyImmediate(root);
  }
  AssetDatabase.SaveAssets();
 }
}}
