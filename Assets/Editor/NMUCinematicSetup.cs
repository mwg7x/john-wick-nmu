using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastStep.Editor
{
    // Additive, repeatable dressing pass: never replaces gameplay objects or stage markers.
    public static class NMUCinematicSetup
    {
        const string Folder = "Assets/Art/NMU/Cinematic";
        static Material ivory, navy, metal, rubber, chrome, glazing, warm, red, green, bark, paving;
        static TMP_FontAsset font;
        public static void BuildReviewPlayer() { Run(); WindowsBuild.RunMono(); }
        [MenuItem("LAST STEP/NMU/Apply Cinematic Campus")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var normalImporter=AssetImporter.GetAtPath(Folder+"/asphalt_normal.jpg") as TextureImporter;
            if(normalImporter!=null){normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.SaveAndReimport();}
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            ivory=Mat("CoachPearl",new Color(.76f,.79f,.79f),.52f,.22f);
            navy=Mat("CoachNavy",new Color(.018f,.06f,.10f),.48f,.3f);
            metal=Mat("PowderCoatedMetal",new Color(.035f,.045f,.055f),.3f,.7f);
            rubber=Mat("TyreRubber",new Color(.018f,.021f,.023f),.12f);
            chrome=Mat("BrushedAlloy",new Color(.42f,.47f,.49f),.72f,.85f);
            glazing=Mat("SmokedCoachGlass",new Color(.025f,.075f,.10f),.9f,.45f);
            warm=Mat("WarmDiffuser",new Color(.85f,.64f,.34f),.4f,0,new Color(1,.62f,.27f)*3);
            red=Mat("RearLamp",new Color(.35f,.018f,.012f),.5f,0,new Color(1,.03f,.01f));
            green=Mat("PalmLeaf",new Color(.045f,.12f,.065f),.23f);
            green.SetFloat("_Cull",0);
            bark=Mat("PalmBark",new Color(.19f,.14f,.09f),.15f);
            paving=Mat("Paving",new Color(.28f,.29f,.28f),.28f);
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Duel_NMU.unity");
            var old=GameObject.Find("NMU_CinematicCampus"); if(old!=null) UnityEngine.Object.DestroyImmediate(old);
            var root=new GameObject("NMU_CinematicCampus").transform;
            var buses=Group(root,"WestTransportCourt");
            var bus=BuildCoach();
            for(int i=0;i<3;i++)
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(bus,buses);
                instance.name="NMU_Coach_"+(i+1); instance.transform.localPosition=new Vector3(-18.5f-i*1.3f,0,-15+i*13);
                instance.transform.localRotation=Quaternion.Euler(0,180,0);
            }
            var landscape=Group(root,"LandscapeAndStreetFurniture");
            for(int side=-1;side<=1;side+=2) for(int i=0;i<5;i++)
            {
                float z=-21+i*12;
                Lamp(landscape,new Vector3(side*13.6f,0,z),side);
                Palm(landscape,new Vector3(side*(i%2==0?24:28),0,z+3),i+7*(side+1));
                for(int b=0;b<3;b++)
                {
                    var p=new Vector3(side*15.1f,.65f,z+b*3);
                    Cylinder(landscape,"SecurityBollard",p,new Vector3(.18f,.65f,.18f),metal);
                    Cylinder(landscape,"ReflectiveCollar",p+Vector3.up*.43f,new Vector3(.19f,.045f,.19f),warm);
                }
                // Concrete paving modules give the sidewalk a visible joint rhythm.
                for(int j=0;j<8;j++) Box(landscape,"PavingSlab",new Vector3(side*16,.252f,z+j*1.5f),new Vector3(2.8f,.025f,1.46f),paving);
                var banner=Box(landscape,"CampusBanner",new Vector3(side*14.25f,4.2f,z+.2f),new Vector3(1.1f,2.2f,.045f),navy);
                Label(banner.transform,"NMU\nCAMPUS",new Vector3(0,0,-.53f),new Vector2(.9f,1.8f),1.3f,Color.white);
            }
            // Gate lighting, reveals, entrance controls; everything stays beyond the duel corridor.
            foreach(int side in new[]{-1,1})
            {
                for(int bay=-2;bay<=2;bay++)
                {
                    float x=side*21+bay*2.65f;
                    Box(root,"InteriorLightStrip",new Vector3(x,6.7f,30.72f),new Vector3(1.5f,.06f,.04f),warm);
                    Box(root,"WindowSpandrel",new Vector3(x,3.5f,30.7f),new Vector3(1.7f,.22f,.08f),metal);
                }
                for(int j=0;j<3;j++)
                {
                    var light=Spot(root,"FacadeWash",new Vector3(side*21+(j-1)*5,.6f,27.8f),new Vector3(side*21+(j-1)*5,6,31),12,14,55);
                    Box(root,"FacadeLightHousing",light.transform.localPosition,new Vector3(.32f,.22f,.3f),metal);
                }
                Box(root,"GateAccent",new Vector3(side*10.84f,6.7f,30.65f),new Vector3(.06f,12.8f,.05f),warm);
                Box(root,"SecurityPedestal",new Vector3(side*9, .65f,35),new Vector3(.5f,1.3f,.5f),ivory);
                Box(root,"RaisedBarrier",new Vector3(side*9,3.7f,35),new Vector3(.12f,5.3f,.12f),ivory);
                Box(root,"CardReader",new Vector3(side*8.7f,1.1f,34.8f),new Vector3(.12f,.22f,.1f),navy);
                for(int i=0;i<4;i++)
                {
                    Box(root,"WoodBenchSlat",new Vector3(side*18,.65f, -7+i*.17f),new Vector3(2.4f,.09f,.14f),bark);
                    Box(root,"BenchLeg",new Vector3(side*18+(i%2==0?-.85f:.85f),.32f,-6.75f),new Vector3(.09f,.64f,.6f),metal);
                }
            }
            Box(root,"GateCorniceLight",new Vector3(0,11.67f,30.63f),new Vector3(21.5f,.06f,.06f),warm);
            var sign=GameObject.Find("UniversitySign")?.GetComponent<TMP_Text>();
            if(sign!=null) { sign.color=new Color(1,.84f,.55f); sign.fontSize=8.4f; }
            for(int x=-8;x<=8;x+=8) Spot(root,"SignWash",new Vector3(x,10.6f,27),new Vector3(x,12.6f,31),8,9,75);
            UpgradeRoad();
            foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if(light.type==LightType.Directional) { light.intensity=.65f;light.color=new Color(.48f,.62f,1);light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(42,-30,0); }
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.16f,.21f,.31f);
            RenderSettings.ambientEquatorColor=new Color(.09f,.12f,.18f);
            RenderSettings.ambientGroundColor=new Color(.035f,.045f,.055f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.035f,.055f,.085f);RenderSettings.fogDensity=.006f;
            var sky=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/EveningSky.mat");
            if(sky==null){sky=new Material(Shader.Find("Skybox/Panoramic"));AssetDatabase.CreateAsset(sky,Folder+"/EveningSky.mat");}
            sky.shader=Shader.Find("Skybox/Panoramic");
            sky.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sky/MoonlitClouds.png"));
            sky.SetFloat("_Exposure",.65f);sky.SetFloat("_Rotation",110);
            sky.SetColor("_Tint",new Color(.5f,.5f,.5f));
            RenderSettings.skybox=sky;Camera.main.clearFlags=CameraClearFlags.Skybox;
            var volume=Group(root,"EveningGrade").gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=1;
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder+"/EveningGrade.asset");
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Folder+"/EveningGrade.asset");}
            profile.components.RemoveAll(c=>c==null);
            if(!profile.TryGet<Bloom>(out var bloom)) bloom=profile.Add<Bloom>();bloom.intensity.Override(.22f);bloom.threshold.Override(1.1f);
            if(!profile.TryGet<ColorAdjustments>(out var grade))grade=profile.Add<ColorAdjustments>();grade.contrast.Override(12);grade.saturation.Override(-12);
            foreach(var component in profile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);
            volume.sharedProfile=profile;
            var probe=Group(root,"CampusReflection").gameObject.AddComponent<ReflectionProbe>();
            probe.transform.position=new Vector3(0,4,10);probe.size=new Vector3(85,25,100);probe.resolution=128;
            probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;probe.boxProjection=true;
            probe.cullingMask=~(1<<2);probe.intensity=.8f;
            // Low-cost fill keeps faces/aim targets legible throughout 50/30/10m rounds.
            Spot(root,"DuelLaneFill",new Vector3(0,13,-18),new Vector3(0,1,12),6,70,95,new Color(.55f,.68f,1));
            EditorUtility.SetDirty(profile);EditorUtility.SetDirty(sky);
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            PortraitBake.Ensure();
            MotionPolishSetup.Run();
            Debug.Log("NMU cinematic campus saved, with three left-side coaches and clear central duel lane.");
        }
        public static void RepairGradeAsset()
        {
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder+"/EveningGrade.asset");
            if(profile==null)return;
            profile.components.RemoveAll(c=>c==null);
            if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>();
            bloom.intensity.Override(.22f);bloom.threshold.Override(1.1f);
            if(!profile.TryGet<ColorAdjustments>(out var grade))grade=profile.Add<ColorAdjustments>();
            grade.contrast.Override(12);grade.saturation.Override(-12);
            foreach(var component in profile.components)
            {if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);EditorUtility.SetDirty(component);}
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();
        }
        static void UpgradeRoad()
        {
            var road=GameObject.Find("Courtyard"); if(road==null)throw new InvalidOperationException("NMU road missing");
            var m=Mat("WetAsphalt",new Color(.34f,.36f,.38f),.7f,.12f);
            var diffuse=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/asphalt_diff.jpg");
            var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/asphalt_normal.jpg");
            if(diffuse!=null)m.SetTexture("_BaseMap",diffuse);
            if(normal!=null){m.SetTexture("_BumpMap",normal);m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.5f);}
            m.SetTextureScale("_BaseMap",new Vector2(30,33));m.SetTextureScale("_BumpMap",new Vector2(30,33));
            road.GetComponent<Renderer>().sharedMaterial=m;
            // Tileable multi-scale wetness mask; smooth patches, rough aggregate, no painted fake reflections.
            const int size=256;var mask=new Texture2D(size,size,TextureFormat.RGBA32,true,true);
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float a=x/(float)size*Mathf.PI*2,b=y/(float)size*Mathf.PI*2;
                float wet=Mathf.Clamp01(.5f+.24f*Mathf.Sin(a*2+Mathf.Sin(b*3))+.22f*Mathf.Cos(b*2+Mathf.Sin(a*5)));
                pixels[y*size+x]=new Color(.06f,0,0,Mathf.Lerp(.27f,.95f,wet));
            }
            mask.SetPixels(pixels);mask.Apply();File.WriteAllBytes(Folder+"/Wetness.png",mask.EncodeToPNG());UnityEngine.Object.DestroyImmediate(mask);
            AssetDatabase.ImportAsset(Folder+"/Wetness.png");var importer=(TextureImporter)AssetImporter.GetAtPath(Folder+"/Wetness.png");importer.sRGBTexture=false;importer.SaveAndReimport();
            m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Wetness.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.SetTextureScale("_MetallicGlossMap",new Vector2(4,5));EditorUtility.SetDirty(m);
            foreach(var name in new[]{"CharcoalGranite","Limestone"})
            {var facade=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/NMU/"+name+".mat");if(facade!=null){facade.SetTextureScale("_BaseMap",new Vector2(3,5));facade.SetTextureScale("_BumpMap",new Vector2(3,5));EditorUtility.SetDirty(facade);}}
        }
        static GameObject BuildCoach()
        {
            var root=new GameObject("NMU_UniversityCoach").transform;
            Box(root,"Chassis",new Vector3(0,.67f,0),new Vector3(2.45f,.45f,10.8f),metal);
            Box(root,"PearlLowerBody",new Vector3(0,1.24f,0),new Vector3(2.5f,.9f,11),ivory);
            Box(root,"ContinuousGlazing",new Vector3(0,2.35f,0),new Vector3(2.46f,1.35f,10.9f),glazing);
            Box(root,"Roof",new Vector3(0,3.08f,0),new Vector3(2.5f,.18f,10.9f),ivory);
            Box(root,"RoofAC",new Vector3(0,3.26f,-1),new Vector3(1.7f,.2f,2.5f),ivory);
            foreach(int side in new[]{-1,1})
            {
                Box(root,"UniversityStripe",new Vector3(side*1.257f,1.53f,0),new Vector3(.02f,.22f,10.75f),navy);
                Box(root,"LowerSill",new Vector3(side*1.26f,.83f,0),new Vector3(.035f,.12f,10.8f),chrome);
                for(int i=0;i<8;i++)Box(root,"WindowPillar",new Vector3(side*1.24f,2.35f,-4.7f+i*1.35f),new Vector3(.05f,1.4f,.07f),metal);
                for(int i=0;i<5;i++)Box(root,"LuggageDoorJoint",new Vector3(side*1.26f,1.15f,-3.2f+i*1.45f),new Vector3(.02f,.55f,.016f),metal);
                foreach(float z in new[]{-3.5f,3.4f})
                {
                    var tyre=Cylinder(root,"Tyre",new Vector3(side*1.24f,.59f,z),new Vector3(.59f,.19f,.59f),rubber);tyre.transform.localRotation=Quaternion.Euler(0,0,90);
                    var rim=Cylinder(root,"WheelHub",new Vector3(side*1.44f,.59f,z),new Vector3(.34f,.025f,.34f),chrome);rim.transform.localRotation=Quaternion.Euler(0,0,90);
                    for(int bolt=0;bolt<8;bolt++)
                    {float a=bolt*Mathf.PI/4;var hub=Cylinder(root,"HubBolt",new Vector3(side*1.47f,.59f+Mathf.Sin(a)*.23f,z+Mathf.Cos(a)*.23f),new Vector3(.035f,.02f,.035f),metal);hub.transform.localRotation=Quaternion.Euler(0,0,90);}
                }
                var label=Label(root,"NEW MANSOURA UNIVERSITY   /   #NMU",new Vector3(side*1.275f,1.17f,-.2f),new Vector2(7.2f,.45f),1.8f,new Color(.04f,.09f,.13f));label.transform.localRotation=Quaternion.Euler(0,side<0?90:-90,0);
                Box(root,"MirrorArm",new Vector3(side*1.43f,2.6f,5.15f),new Vector3(.4f,.05f,.05f),metal);
                Box(root,"Mirror",new Vector3(side*1.63f,2.4f,5.15f),new Vector3(.12f,.45f,.22f),metal);
                Box(root,"Headlamp",new Vector3(side*.86f,1.04f,5.51f),new Vector3(.44f,.16f,.035f),warm);
                Box(root,"RearLamp",new Vector3(side*1.02f,1.25f,-5.51f),new Vector3(.18f,.45f,.035f),red);
            }
            Box(root,"FrontBumper",new Vector3(0,.75f,5.5f),new Vector3(2.45f,.18f,.12f),navy);
            for(int i=0;i<5;i++)Box(root,"GrilleSlat",new Vector3(0,1.13f+i*.075f,5.515f),new Vector3(.95f,.025f,.025f),metal);
            var front=Label(root,"NMU  |  CAMPUS SHUTTLE",new Vector3(0,2.82f,5.49f),new Vector2(2.2f,.25f),.75f,new Color(1,.83f,.48f));front.transform.localRotation=Quaternion.Euler(0,180,0);
            for(int i=0;i<7;i++)foreach(int side in new[]{-1,1})
                Box(root,"SeatBack",new Vector3(side*.65f,1.95f,-4+i*1.22f),new Vector3(.48f,.65f,.18f),navy);
            // One intentional simple hull, rather than hundreds of tiny decoration colliders.
            var hull=root.gameObject.AddComponent<BoxCollider>();hull.center=new Vector3(0,1.65f,0);hull.size=new Vector3(2.6f,3.1f,11.1f);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Prefabs/Environment/NMUUniversityCoach.prefab");
            UnityEngine.Object.DestroyImmediate(root.gameObject);return prefab;
        }
        static void Lamp(Transform parent,Vector3 p,int side)
        {
            var root=Group(parent,"CampusLamp");root.localPosition=p;
            Cylinder(root,"MetalPost",new Vector3(0,3.6f,0),new Vector3(.09f,3.6f,.09f),metal);
            Cylinder(root,"PostBase",new Vector3(0,.3f,0),new Vector3(.2f,.3f,.2f),metal);
            Box(root,"Arm",new Vector3(-side*.65f,7.12f,0),new Vector3(1.4f,.1f,.1f),metal);
            Box(root,"Luminaire",new Vector3(-side*1.25f,7.08f,0),new Vector3(.65f,.14f,.35f),metal);
            Box(root,"Diffuser",new Vector3(-side*1.25f,6.99f,0),new Vector3(.55f,.03f,.27f),warm);
            Spot(root,"RoadPool",new Vector3(-side*1.25f,6.9f,0),new Vector3(-side*4,0,0),19,16,105);
        }
        static void Palm(Transform parent,Vector3 p,int seed)
        {
            var root=Group(parent,"DatePalm");root.localPosition=p;
            float height=7.5f+(seed%3)*.55f;
            Cylinder(root,"Trunk",new Vector3(0,height/2,0),new Vector3(.23f,height/2,.23f),bark);
            for(int k=0;k<20;k++)Cylinder(root,"BarkRing",new Vector3(0,k*height/20,0),new Vector3(.25f,.045f,.25f),bark);
            for(int f=0;f<10;f++)
            {
                var frond=Group(root,"PalmFrond");frond.localPosition=new Vector3(0,height,0);frond.localRotation=Quaternion.Euler(0,f*36+seed*17,0);
                // Tapered curved leaflets along a drooping rachis, modeled geometry with no alpha overdraw.
                for(int j=0;j<9;j++)
                {
                    float t=j/8f;float z=.25f+t*3.2f;float y=Mathf.Sin(t*Mathf.PI)*.8f-t*1.2f;
                    var stem=Box(frond,"Rachis",new Vector3(0,y,z),new Vector3(.04f,.035f,.48f),green);
                    stem.transform.localRotation=Quaternion.Euler(10+t*35,0,0);
                    foreach(int s in new[]{-1,1})
                    {
                        var leaf=Box(frond,"Leaflet",new Vector3(s*(.18f+.24f*(1-t)),y-.08f,z),new Vector3(.75f*(1-t)+.1f,.018f,.12f),green);
                        leaf.transform.localRotation=Quaternion.Euler(0,s*35,s*-12);
                    }
                }
            }
            // Collapse thousands of leaf pieces into two persistent meshes per palm.
            var filters=root.GetComponentsInChildren<MeshFilter>();
            var combined=new System.Collections.Generic.List<(Mesh mesh,Material material)>();
            foreach(var material in new[]{bark,green})
            {
                var parts=filters.Where(x=>x.GetComponent<Renderer>().sharedMaterial==material)
                    .Select(x=>new CombineInstance {mesh=x.sharedMesh,transform=root.worldToLocalMatrix*x.transform.localToWorldMatrix}).ToArray();
                var mesh=new Mesh {name="Palm_"+seed+"_"+material.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(parts);
                string path=Folder+"/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing==null)AssetDatabase.CreateAsset(mesh,path);
                else {EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
                combined.Add((mesh,material));
            }
            foreach(Transform child in root.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach(var part in combined)
            {
                var go=Group(root,part.material==green?"Fronds":"Trunk").gameObject;
                go.AddComponent<MeshFilter>().sharedMesh=part.mesh;go.AddComponent<MeshRenderer>().sharedMaterial=part.material;go.isStatic=true;
            }
        }
        static Transform Group(Transform parent,string name){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static GameObject Box(Transform p,string n,Vector3 pos,Vector3 size,Material m)=>Shape(p,n,pos,size,m,PrimitiveType.Cube);
        static GameObject Cylinder(Transform p,string n,Vector3 pos,Vector3 size,Material m)=>Shape(p,n,pos,size,m,PrimitiveType.Cylinder);
        static GameObject Shape(Transform p,string n,Vector3 pos,Vector3 size,Material m,PrimitiveType type)
        {var o=GameObject.CreatePrimitive(type);o.name=n;o.transform.SetParent(p,false);o.transform.localPosition=pos;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());o.isStatic=true;return o;}
        static Material Mat(string name,Color color,float smooth,float metallic=0,Color emission=default)
        {var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metallic);if(emission.maxColorComponent>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);}EditorUtility.SetDirty(m);return m;}
        static TMP_Text Label(Transform parent,string text,Vector3 p,Vector2 size,float fontSize,Color color)
        {var label=Group(parent,"CampusLettering").gameObject.AddComponent<TextMeshPro>();label.font=font;label.text=text;label.fontSize=fontSize;label.alignment=TextAlignmentOptions.Center;label.color=color;label.rectTransform.sizeDelta=size;label.transform.localPosition=p;return label;}
        static Light Spot(Transform parent,string name,Vector3 p,Vector3 target,float intensity,float range,float angle,Color color=default)
        {var l=Group(parent,name).gameObject.AddComponent<Light>();l.transform.localPosition=p;l.transform.LookAt(parent.TransformPoint(target));l.type=LightType.Spot;l.range=range;l.spotAngle=angle;l.intensity=intensity*6;l.color=color.maxColorComponent>0?color:new Color(1,.72f,.42f);l.shadows=LightShadows.None;return l;}
    }
}
