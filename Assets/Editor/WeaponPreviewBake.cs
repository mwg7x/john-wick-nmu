using System.IO;
using UnityEditor;
using UnityEngine;
namespace LastStep.Editor {
public static class WeaponPreviewBake {
 public static void Run(){
  const string folder="Assets/Resources/WeaponPreviews";
  if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Resources","WeaponPreviews");
  var cameraGo=new GameObject("WeaponPreviewCamera",typeof(Camera));var camera=cameraGo.GetComponent<Camera>();
  camera.orthographic=true;camera.orthographicSize=.23f;camera.clearFlags=CameraClearFlags.SolidColor;
  camera.backgroundColor=new Color(.035f,.055f,.083f);camera.cullingMask=1<<2;
  var lightGo=new GameObject("WeaponPreviewLight",typeof(Light));lightGo.transform.position=new Vector3(.5f,.5f,-.4f);
  var light=lightGo.GetComponent<Light>();light.type=LightType.Point;light.intensity=5;light.range=3;light.cullingMask=1<<2;
  var rt=new RenderTexture(256,144,24);camera.targetTexture=rt;var old=RenderTexture.active;
  try{
   for(int index=0;index<3;index++){
    var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Pistols/Pistol"+index+".prefab");
    if(source==null)throw new System.Exception("Missing licensed pistol "+index);
    var model=(GameObject)PrefabUtility.InstantiatePrefab(source);
    model.transform.position=Vector3.zero;
    foreach(var part in model.GetComponentsInChildren<Transform>())part.gameObject.layer=2;
    cameraGo.transform.position=new Vector3(.58f,.26f,-.35f);cameraGo.transform.LookAt(Vector3.zero);
    camera.Render();RenderTexture.active=rt;
    var image=new Texture2D(256,144,TextureFormat.RGB24,false);
    image.ReadPixels(new Rect(0,0,256,144),0,0);image.Apply();
    string path=folder+"/Pistol"+index+".png";
    File.WriteAllBytes(path,image.EncodeToPNG());
    Object.DestroyImmediate(image);Object.DestroyImmediate(model);
    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
   }
  }finally{RenderTexture.active=old;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(cameraGo);Object.DestroyImmediate(lightGo);}
  Debug.Log("Weapon model previews baked.");
 }
}}
