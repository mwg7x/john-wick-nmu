using System;
using System.IO;
using LastStep.AI;
using LastStep.Utilities;
using UnityEditor;
using UnityEngine;

namespace LastStep.Editor
{
    // Render the same original character geometry used in duels, so portraits match the roster.
    public static class PortraitBake
    {
        private const string Folder="Assets/Resources/OpponentPortraits";
        public static void Ensure()
        {
            var first=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Ahmed_Allam.png");
            if(first!=null && first.width>=256 &&
                File.Exists(Folder+"/framed-v4.txt") &&
                File.Exists(Folder+"/Protagonist.png")) return;
            var root=new GameObject("PortraitBakeTemporary");
            root.transform.position=new Vector3(1000,1000,1000);
            var cameraGo=new GameObject("PortraitCamera",typeof(Camera));
            cameraGo.transform.position=root.transform.position+new Vector3(0,1.85f,1.35f);
            cameraGo.transform.LookAt(root.transform.position+new Vector3(0,1.78f,0));
            var camera=cameraGo.GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.025f,.045f,.075f,1f);
            camera.fieldOfView=36f;
            camera.cullingMask=1<<2;
            var lightGo=new GameObject("PortraitKey",typeof(Light));
            lightGo.transform.position=root.transform.position+new Vector3(.8f,2.2f,1.4f);
            lightGo.transform.LookAt(root.transform.position+Vector3.up*1.5f);
            var light=lightGo.GetComponent<Light>();
            light.type=LightType.Spot;light.spotAngle=70;light.range=7;
            light.intensity=9;light.color=new Color(.94f,.90f,.84f);
            light.cullingMask=1<<2;
            var rt=new RenderTexture(256,256,24);
            camera.targetTexture=rt;
            var previous=RenderTexture.active;
            try
            {
                for(int tier=-1;tier<OpponentRoster.TierCount;tier++)
                    for(int index=0;index<(tier<0?1:OpponentRoster.Count(tier));index++)
                    {
                        bool player=tier<0;
                        var entry=player?new OpponentRoster.Entry("Protagonist",
                            new Color(.59f,.39f,.29f),new Color(.035f,.039f,.047f),1.04f,1f)
                            :OpponentRoster.Get(tier,index);
                        var actor=new GameObject("Portrait_"+entry.Name);
                        actor.transform.SetParent(root.transform,false);
                        actor.transform.localRotation=Quaternion.identity;
                        actor.AddComponent<ProceduralCharacter>().BuildPortraitPreview(entry,player);
                        var visible=actor.GetComponentsInChildren<Renderer>(true);
                        if(visible.Length>0)
                        {
                            Bounds bounds=visible[0].bounds;
                            foreach(var renderer in visible) bounds.Encapsulate(renderer.bounds);
                            float eyeLevel=bounds.min.y+bounds.size.y*.86f;
                            var focus=new Vector3(bounds.center.x,eyeLevel,bounds.center.z);
                            cameraGo.transform.position=focus+new Vector3(0,.025f,.76f);
                            cameraGo.transform.LookAt(focus);
                        }
                        var animator=actor.GetComponentInChildren<Animator>();
                        if(animator!=null && animator.avatar!=null && animator.avatar.isHuman)
                        {
                            var left=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                            var right=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                            if(left!=null) left.rotation=Quaternion.AngleAxis(68f,actor.transform.forward)*left.rotation;
                            if(right!=null) right.rotation=Quaternion.AngleAxis(-68f,actor.transform.forward)*right.rotation;
                        }
                        else
                        {
                            foreach(var bone in actor.GetComponentsInChildren<Transform>())
                            {
                                if(bone.name=="upperarm_l") bone.rotation=Quaternion.AngleAxis(68f,actor.transform.forward)*bone.rotation;
                                if(bone.name=="upperarm_r") bone.rotation=Quaternion.AngleAxis(-68f,actor.transform.forward)*bone.rotation;
                            }
                        }
                        foreach (var part in actor.GetComponentsInChildren<Transform>(true)) part.gameObject.layer=2;
                        camera.Render();
                        RenderTexture.active=rt;
                        var image=new Texture2D(256,256,TextureFormat.RGB24,false);
                        image.ReadPixels(new Rect(0,0,256,256),0,0);
                        image.Apply();
                        string path=Folder+"/"+entry.Name.Replace(' ','_')+".png";
                        File.WriteAllBytes(path,image.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(image);
                        UnityEngine.Object.DestroyImmediate(actor);
                        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                    }
                File.WriteAllText(Folder+"/framed-v4.txt","Tight portraits with lowered arms from the same imported character meshes as gameplay.");
            }
            finally
            {
                RenderTexture.active=previous;
                camera.targetTexture=null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(cameraGo);
                UnityEngine.Object.DestroyImmediate(lightGo);
            }
        }
    }
}
