using System;
using System.IO;
using System.Linq;
using LastStep.Core;
using LastStep.UI;
using LastStep.Utilities;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastStep.Editor
{
    // Original editable geometry. Dimensions are estimates from one photograph,
    // not a surveyed reconstruction. Shared gameplay is inherited unchanged.
    public static class NMUSetup
    {
        private const string Folder = "Assets/Art/NMU";
        private static Material stone, cream, glass, red, asphalt, yellow, white, soil;
        [MenuItem("LAST STEP/NMU/Build Map and Selector")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            stone = Material("CharcoalGranite", new Color(.16f,.17f,.16f), .22f);
            cream = Material("Limestone", new Color(.73f,.66f,.47f), .18f);
            glass = Material("BlueGlazing", new Color(.035f,.28f,.39f), .82f, .45f);
            red = Material("Terracotta", new Color(.48f,.075f,.035f), .18f);
            asphalt = Material("Asphalt", new Color(.12f,.13f,.12f), .07f);
            yellow = Material("CurbYellow", new Color(.92f,.68f,.06f), .22f);
            white = Material("Markings", new Color(.83f,.84f,.79f), .2f);
            soil = Material("PlantingSoil", new Color(.17f,.13f,.07f), .04f);
            foreach(var material in new[]{stone,cream})
            {
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/PolyHaven/StoneTiles/albedo.jpg"));
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/PolyHaven/StoneTiles/normal.png"));
                material.SetFloat("_BumpScale",.25f);material.EnableKeyword("_NORMALMAP");
                EditorUtility.SetDirty(material);
            }
            BuildEnvironmentPrefab();
            BuildScene();
            BuildSelector();
            EditorBuildSettings.scenes = new[] { "Boot", "MainMenu", "Duel_Staircase", "Duel_NMU" }
                .Select(n => new EditorBuildSettingsScene("Assets/Scenes/" + n + ".unity", true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("NMU map and selector generated; 50/30/10m markers validated.");
        }
        private static Material Material(string name, Color color, float smooth, float metallic = 0f)
        {
            string path = Folder + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
            m.color = color; m.SetFloat("_Smoothness",smooth); m.SetFloat("_Metallic",metallic);
            EditorUtility.SetDirty(m); return m;
        }
        private static Transform Group(string name, Transform parent)
        { var go = new GameObject(name); go.transform.SetParent(parent,false); return go.transform; }
        private static GameObject Box(Transform root, string name, Vector3 p, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name;
            go.transform.SetParent(root,false); go.transform.localPosition=p; go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=material; go.isStatic=true; return go;
        }
        private static void BuildEnvironmentPrefab()
        {
            var root = new GameObject("NMU_Entrance");
            Box(root.transform,"Courtyard",new Vector3(0,-.2f,0),new Vector3(90,.4f,100),asphalt);
            var gate=Group("EntranceGate",root.transform);
            // The gate is behind the enemy, leaving the full measured duel lane clear.
            foreach (int side in new[]{-1,1})
            {
                Box(gate,"GatePier",new Vector3(side*12,6.8f,32),new Vector3(2.2f,13.6f,2.6f),stone);
                var pavilion=Group(side<0?"WestPavilion":"EastPavilion",root.transform);
                pavilion.localPosition=new Vector3(side*21,0,32);
                Box(pavilion,"PavilionBody",new Vector3(0,4.6f,3),new Vector3(14,9.2f,8),cream);
                Box(pavilion,"DeepRoofCornice",new Vector3(0,9.4f,2.3f),new Vector3(15.5f,.65f,10),stone);
                Box(pavilion,"RoofCap",new Vector3(0,9.85f,2.3f),new Vector3(16,.23f,10.4f),stone);
                Box(pavilion,"Plinth",new Vector3(0,.4f,-1.2f),new Vector3(14,.8f,1.5f),stone);
                for(int bay=-2;bay<=2;bay++)
                {
                    float x=bay*2.65f;
                    Box(pavilion,"BlueGlassBay",new Vector3(x,4.8f,-1.06f),new Vector3(1.7f,7.4f,.16f),glass);
                    Box(pavilion,"FacadePilaster",new Vector3(x-1.08f,4.6f,-1.35f),new Vector3(.5f,9.2f,.7f),stone);
                    for(int level=1;level<=4;level++)
                        Box(pavilion,"WindowMullion",new Vector3(x,level*1.65f+.3f,-1.18f),new Vector3(1.75f,.07f,.1f),stone);
                }
                for(int step=0;step<3;step++)
                    Box(pavilion,"EntranceStep",new Vector3(0,.075f+step*.15f,-2.6f+step*.38f),new Vector3(8,.15f,.4f),cream);
                Box(root.transform,"RaisedWalk",new Vector3(side*16,.12f,0),new Vector3(3,.24f,58),cream);
                for(int i=0;i<29;i++)
                    Box(root.transform,"AlternatingCurb",new Vector3(side*14.4f,.17f,-28+i*2),new Vector3(.28f,.34f,2),i%2==0?yellow:stone);
                // Flower beds stay outside the central corridor.
                Box(root.transform,"PlanterRim",new Vector3(side*20,.32f,5),new Vector3(6,.64f,18),cream);
                Box(root.transform,"PlantingBed",new Vector3(side*20,.66f,5),new Vector3(5.5f,.1f,17.5f),soil);
                var wing=Group("CampusWing",root.transform); wing.localPosition=new Vector3(side*33,0,44);
                Box(wing,"WingMass",new Vector3(0,6,0),new Vector3(18,12,18),cream);
                for(int column=-2;column<=2;column++) for(int floor=0;floor<3;floor++)
                {
                    Box(wing,"WindowReveal",new Vector3(column*3,3+floor*3.8f,-9.08f),new Vector3(2.2f,2.8f,.3f),stone);
                    Box(wing,"WindowPane",new Vector3(column*3,3+floor*3.8f,-9.26f),new Vector3(1.7f,2.4f,.08f),glass);
                }
            }
            Box(gate,"SignedLintel",new Vector3(0,12.6f,32),new Vector3(26.2f,2,2.6f),stone);
            var sign=Group("UniversitySign",gate).gameObject.AddComponent<TextMeshPro>();
            sign.text="NEW MANSOURA UNIVERSITY   /   #NMU";
            sign.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            sign.fontSize=8f; sign.alignment=TextAlignmentOptions.Center; sign.color=Color.white;
            sign.rectTransform.sizeDelta=new Vector2(24,1.4f); sign.transform.localPosition=new Vector3(0,12.6f,30.65f);
            foreach(int side in new[]{-1,1})
            {
                // Paired red tapered pylons flank the glazed security kiosk in the reference.
                Pylon(root.transform,side);
            }
            Box(root.transform,"SecurityKiosk",new Vector3(0,2.6f,43),new Vector3(7,5.2f,4),glass);
            for(int i=-3;i<=3;i++) Box(root.transform,"KioskMullion",new Vector3(i,2.6f,40.95f),new Vector3(.08f,5.2f,.12f),stone);
            for(int i=-4;i<=4;i++)
                Box(root.transform,"RoadDash",new Vector3(7,.012f,i*6),new Vector3(.1f,.02f,2.5f),white);
            Directory.CreateDirectory("Assets/Prefabs/Environment");
            PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Environment/NMUEntrance.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }
        private static void Pylon(Transform parent,int side)
        {
            var go=new GameObject("TaperedRedPylon"); go.transform.SetParent(parent,false); go.transform.localPosition=new Vector3(side*5.5f,0,41);
            var mesh=new Mesh { name="NMUPylon" };
            Vector3[] corners={new Vector3(-2,0,-1),new Vector3(2,0,-1),new Vector3(2,0,1),new Vector3(-2,0,1),new Vector3(-1.2f,7,-1),new Vector3(1.2f,7,-1),new Vector3(1.2f,7,1),new Vector3(-1.2f,7,1)};
            int[] faces={0,4,5,1,1,5,6,2,2,6,7,3,3,7,4,0,4,7,6,5,3,0,1,2};
            var vertices=new Vector3[24]; var uv=new Vector2[24]; var triangles=new int[36];
            for(int f=0;f<6;f++) { for(int v=0;v<4;v++) {vertices[f*4+v]=corners[faces[f*4+v]]; uv[f*4+v]=new Vector2(v==2||v==3?1:0,v==1||v==2?1:0);} int b=f*4,t=f*6; triangles[t]=b;triangles[t+1]=b+1;triangles[t+2]=b+2;triangles[t+3]=b;triangles[t+4]=b+2;triangles[t+5]=b+3; }
            mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=Folder+"/Pylon"+side+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null) AssetDatabase.CreateAsset(mesh,path); else {EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=red;
            go.AddComponent<MeshCollider>().sharedMesh=mesh;go.isStatic=true;
        }
        private static void BuildScene()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/Duel_Staircase.unity");
            var markers=UnityEngine.Object.FindAnyObjectByType<DuelPositionMarkers>();
            var original=GameObject.Find("StaircaseGreybox");
            if(markers==null||original==null) throw new InvalidOperationException("Original map structure missing.");
            if(PrefabUtility.IsPartOfPrefabInstance(original)) PrefabUtility.UnpackPrefabInstance(original,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            markers.transform.SetParent(null,true); markers.PlayerStandIn.SetParent(null,true); markers.EnemyStandIn.SetParent(null,true);
            UnityEngine.Object.DestroyImmediate(original);
            foreach(var sky in UnityEngine.Object.FindObjectsByType<SkyPresetController>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(sky);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Environment/NMUEntrance.prefab"));
            for(int i=0;i<3;i++)
            {
                if(!markers.TryGetStage(i,out var pair)) throw new InvalidOperationException("Stage missing");
                pair.Player.SetPositionAndRotation(new Vector3(0,0,-pair.Distance/2),Quaternion.identity);
                pair.Enemy.SetPositionAndRotation(new Vector3(0,0,pair.Distance/2),Quaternion.Euler(0,180,0));
                if(Mathf.Abs(pair.MeasuredDistance-new[]{50f,30f,10f}[i])>.001f) throw new InvalidOperationException("Incorrect NMU checkpoint distance");
            }
            Phase2Setup.PositionPreview(markers,Camera.main,0);
            var sun=new GameObject("NMU_Daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2.2f;sun.color=new Color(1,.94f,.83f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(42,-32,0);
            RenderSettings.skybox=null;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.5f,.64f,.8f);RenderSettings.ambientEquatorColor=new Color(.45f,.48f,.5f);RenderSettings.ambientGroundColor=new Color(.25f,.23f,.19f);
            Camera.main.backgroundColor=new Color(.5f,.66f,.8f);Camera.main.clearFlags=CameraClearFlags.SolidColor;
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/Duel_NMU.unity");
        }
        private static void BuildSelector()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var menu=UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
            var source=menu.transform.Find("Background/SettingsPanel/SkyPresetButton");
            if(source==null) throw new InvalidOperationException("Settings sky button missing");
            var parent=source.parent;
            var existing=parent.Find("MapButton");
            var button=existing!=null?existing.GetComponent<Button>():UnityEngine.Object.Instantiate(source.gameObject,parent).GetComponent<Button>();
            button.name="MapButton"; ((RectTransform)button.transform).anchoredPosition=new Vector2(0,-150);
            button.onClick=new Button.ButtonClickedEvent();
            var label=button.GetComponentInChildren<TMP_Text>();label.text="MAP: STAIRCASE";
            var serialized=new SerializedObject(menu);serialized.FindProperty("mapButton").objectReferenceValue=button;
            serialized.FindProperty("mapLabel").objectReferenceValue=label;serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
