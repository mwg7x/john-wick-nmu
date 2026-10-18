using LastStep.AI;
using System.Collections.Generic;
using UnityEngine;

namespace LastStep.Utilities
{
    // Original low-poly tailored silhouettes built once for each duel.
    [DisallowMultipleComponent]
    public sealed class ProceduralCharacter : MonoBehaviour
    {
        [SerializeField] private bool protagonist;
        private Transform visual;
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Mesh> meshes = new List<Mesh>();

#if UNITY_EDITOR
        public void BuildPortraitPreview(OpponentRoster.Entry entry, bool player = false)
        {
            protagonist = player;
            visual = transform.Find("VisualRig");
            if (visual == null)
            {
                visual = new GameObject("VisualRig").transform;
                visual.SetParent(transform,false);
            }
            if (!BuildImported(entry)) Build(entry);
        }
#endif

        private void Start()
        {
            visual = transform.Find("VisualRig");
            if (visual == null) return;
            var standIn = transform.Find("Capsule")?.GetComponent<Renderer>();
            if (standIn != null) standIn.enabled = false;
            var entry = protagonist
                ? new OpponentRoster.Entry("Protagonist", new Color(.59f,.39f,.29f), new Color(.035f,.039f,.047f), 1.04f, 1f)
                : Core.GameManager.Instance != null ? Core.GameManager.Instance.CurrentOpponent : OpponentRoster.Get(0, 0);
            if (!BuildImported(entry)) Build(entry);
        }

        private bool BuildImported(OpponentRoster.Entry entry)
        {
            bool alternate = !protagonist &&
                (entry.Name == "Mos3d" || entry.Name == "Feqy" || entry.Name == "Tarek Amr");
            var prefab = (alternate ? Resources.Load<GameObject>("Characters/BaldVariant") : null) ??
                         Resources.Load<GameObject>("Characters/RealSuited") ??
                         Resources.Load<GameObject>("Characters/RiggedHuman");
            if (prefab == null || visual == null) return false;
            var instance = Instantiate(prefab, visual);
            instance.name = "ImportedHumanoid";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            bool realistic = prefab.name != "RiggedHuman";
            instance.transform.localScale = Vector3.one * (realistic ? entry.Height : .47f * entry.Height);
            var skinnedRenderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (prefab.name == "BaldVariant")
            {
                // This source has a single baked PBR atlas; preserve its skin and clothing map.
            }
            else if (realistic)
            {
                foreach (var renderer in skinnedRenderers)
                {
                    var source = renderer.sharedMaterial;
                    if (source == null) continue;
                    bool clothing = renderer.name.Contains("casualsuit");
                    // Preserve texture detail while correcting the source's transparency.
                    var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    // These GLBs mark solid skin/suits as double-sided BLEND. Convert
                    // to depth-writing opaque Lit to avoid transparent triangle sorting.
                    var baseTexture=source.HasProperty("baseColorTexture")?source.GetTexture("baseColorTexture"):source.mainTexture;
                    material.SetTexture("_BaseMap",baseTexture);
                    if(source.HasProperty("baseColorTexture"))
                    {material.SetTextureScale("_BaseMap",source.GetTextureScale("baseColorTexture"));material.SetTextureOffset("_BaseMap",source.GetTextureOffset("baseColorTexture"));}
                    var normalTexture=source.HasProperty("normalTexture")?source.GetTexture("normalTexture"):null;
                    if(normalTexture!=null){material.SetTexture("_BumpMap",normalTexture);material.SetTextureScale("_BumpMap",source.GetTextureScale("normalTexture"));material.SetTextureOffset("_BumpMap",source.GetTextureOffset("normalTexture"));material.SetFloat("_BumpScale",.55f);material.EnableKeyword("_NORMALMAP");}
                    material.SetFloat("_Smoothness",clothing ? .22f : .3f);
                    Color tint = clothing ? (protagonist ? new Color(.58f,.60f,.67f) :
                        new Color(Mathf.Clamp01(entry.Suit.r*2.5f),Mathf.Clamp01(entry.Suit.g*2.5f),Mathf.Clamp01(entry.Suit.b*2.5f))) :
                        renderer.name == "Human" ? new Color(Mathf.Clamp01(entry.Skin.r*1.8f),
                            Mathf.Clamp01(entry.Skin.g*1.8f),Mathf.Clamp01(entry.Skin.b*1.8f)) :
                        new Color(.20f,.22f,.26f);
                    if (clothing) tint = new Color(.22f,.24f,.28f);
                    else if (renderer.name == "Human") tint = Color.Lerp(Color.white, tint, .25f);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
                    else if (material.HasProperty("baseColorFactor")) material.SetColor("baseColorFactor", tint);
                    else material.color = tint;
                    if (clothing && material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .22f);
                    if (clothing && material.HasProperty("roughnessFactor")) material.SetFloat("roughnessFactor", .78f);
                    renderer.material = material;
                    materials.Add(material);
                }
            }
            else if (skinnedRenderers.Length > 0)
            {
                string key = protagonist ? "Protagonist" : entry.Name.Replace(' ', '_');
                var texture = Resources.Load<Texture2D>("CharacterSkins/" + key);
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                material.mainTexture = texture;
                skinnedRenderers[0].material = material;
                materials.Add(material);
            }
            var motion = instance.AddComponent<ImportedCharacterMotion>();
            motion.Initialize(transform, protagonist);
            return true;
        }

        private void Build(OpponentRoster.Entry entry)
        {
            float h = entry.Height, w = entry.Width;
            var suit = Mat(entry.Suit,.38f);
            var lapel = Mat(entry.Suit * 1.25f,.40f);
            var shirt = Mat(new Color(.77f,.81f,.84f),.22f);
            var tie = Mat(protagonist ? new Color(.08f,.09f,.12f) : new Color(.30f,.055f,.07f),.45f);
            var skin = Mat(entry.Skin,.18f);
            var skinShadow = Mat(entry.Skin*.76f,.16f);
            var hair = Mat(protagonist ? new Color(.025f,.023f,.025f) : new Color(.055f,.04f,.033f),.23f);
            var shoe = Mat(new Color(.025f,.028f,.035f),.63f);
            var eye = Mat(new Color(.017f,.021f,.028f),.18f);
            float shoulder = protagonist ? .34f : .30f;
            Lathe("Jacket",new[]{new Ring(.82f*h,.20f*w,.13f),new Ring(1.03f*h,.23f*w,.145f),
                new Ring(1.36f*h,shoulder*w,.17f),new Ring(1.53f*h,.20f*w,.12f)},suit,12);
            Lathe("ShirtFront",new[]{new Ring(1.10f*h,.09f,.012f),new Ring(1.48f*h,.12f,.013f)},
                shirt,8,new Vector3(0,0,.155f));
            foreach(int side in new[]{-1,1})
            {
                Triangle("Lapel",new Vector3(side*.028f,1.47f*h,.185f),
                    new Vector3(side*.20f*w,1.46f*h,.175f),new Vector3(side*.045f,1.17f*h,.182f),lapel);
                Box("JacketTail",new Vector3(side*.145f*w,.79f*h,-.025f),new Vector3(.24f*w,.24f*h,.17f),suit);
                Vector3 shoulderPoint=new Vector3(side*shoulder*w,1.42f*h,0);
                Vector3 elbow=side>0
                    ? new Vector3(.50f*w,1.26f*h,.23f)
                    : new Vector3(-.39f*w,1.12f*h,.02f);
                Vector3 cuff=side>0
                    ? new Vector3(.50f*w,1.29f*h,.48f)
                    : new Vector3(-.37f*w,.89f*h,.10f);
                Limb("UpperSleeve",shoulderPoint,elbow,.105f*w,.09f*w,suit);
                Limb("ForearmSleeve",elbow,cuff,.095f*w,.071f*w,suit);
                Vector3 handStart=cuff+(side>0 ? new Vector3(0,0,.03f) : new Vector3(0,-.035f*h,.01f));
                Vector3 handEnd=handStart+(side>0 ? new Vector3(0,0,.105f) : new Vector3(0,-.105f*h,.025f));
                Limb("ShirtCuff",cuff,handStart,.075f*w,.073f*w,shirt);
                Limb("Hand",handStart,handEnd,.06f*w,.045f*w,skin);
                Vector3 hip=new Vector3(side*.13f*w,.86f*h,0);
                Vector3 knee=new Vector3(side*.145f*w,.45f*h,.018f);
                Vector3 ankle=new Vector3(side*.145f*w,.12f*h,-.015f);
                Limb("TrouserUpper",hip,knee,.13f*w,.115f*w,suit);
                Limb("TrouserLower",knee,ankle,.116f*w,.09f*w,suit);
                Box("DressShoe",new Vector3(side*.145f*w,.055f*h,.09f),new Vector3(.22f*w,.115f*h,.36f),shoe);
            }
            Limb("Neck",new Vector3(0,1.49f*h,0),new Vector3(0,1.62f*h,0),.078f,.077f,skin);
            Lathe("Face",new[]{new Ring(1.61f*h,.073f,.078f),new Ring(1.68f*h,.105f,.105f),
                new Ring(1.79f*h,.125f,.115f),new Ring(1.88f*h,.099f,.096f),
                new Ring(1.92f*h,.04f,.045f)},skin,16);
            Lathe("HairCap",new[]{new Ring(1.73f*h,.088f,.078f),new Ring(1.82f*h,.128f,.113f),
                new Ring(1.91f*h,.125f,.116f),new Ring(1.99f*h,.075f,.073f)},
                hair,16,new Vector3(0,0,-.038f));
            foreach(int side in new[]{-1,1})
            {
                Oval("Ear",new Vector3(side*.125f,1.75f*h,0),new Vector3(.032f,.06f*h,.032f),skin);
                Oval("Eye",new Vector3(side*.046f,1.785f*h,.106f),new Vector3(.027f,.012f,.007f),eye);
                Box("Brow",new Vector3(side*.046f,1.81f*h,.111f),new Vector3(.052f,.012f,.012f),hair);
                if(protagonist) Box("SweptHair",new Vector3(side*.059f,1.945f*h,.033f),
                    new Vector3(.13f,.055f,.16f),hair);
            }
            Box("Nose",new Vector3(0,1.745f*h,.122f),new Vector3(.034f,.07f*h,.05f),skinShadow);
            Box("Mouth",new Vector3(0,1.685f*h,.113f),new Vector3(.058f,.009f,.009f),skinShadow);
            Box("Tie",new Vector3(0,1.28f*h,.174f),new Vector3(.045f,.31f*h,.015f),tie);
            Box("Belt",new Vector3(0,.88f*h,.151f),new Vector3(.34f*w,.04f,.02f),shoe);
        }

        private Material Mat(Color color,float smooth)
        {
            var shader=Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat=new Material(shader); mat.color=color;
            if(mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",smooth);
            materials.Add(mat); return mat;
        }
        private void Put(string name,Mesh mesh,Material material)
        {
            mesh.name=name;meshes.Add(mesh);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
            go.layer=2;go.transform.SetParent(visual,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        private void Lathe(string name,Ring[] rings,Material mat,int sides,Vector3 offset=default)
        {
            var verts=new List<Vector3>();var tris=new List<int>();
            for(int r=0;r<rings.Length;r++)for(int j=0;j<sides;j++)
            {
                float a=j*Mathf.PI*2f/sides;
                verts.Add(new Vector3(Mathf.Sin(a)*rings[r].x,rings[r].y,Mathf.Cos(a)*rings[r].z)+offset);
            }
            for(int r=0;r<rings.Length-1;r++)for(int j=0;j<sides;j++)
            {
                int a=r*sides+j,b=r*sides+(j+1)%sides,c=(r+1)*sides+j,d=(r+1)*sides+(j+1)%sides;
                tris.Add(a);tris.Add(b);tris.Add(c);tris.Add(b);tris.Add(d);tris.Add(c);
            }
            var mesh=new Mesh{vertices=verts.ToArray(),triangles=tris.ToArray()};
            mesh.RecalculateNormals();Put(name,mesh,mat);
        }
        private void Limb(string name,Vector3 from,Vector3 to,float r0,float r1,Material mat)
        {
            Vector3 axis=(to-from).normalized,right=Vector3.Cross(axis,Vector3.forward).normalized;
            if(right.sqrMagnitude<.01f)right=Vector3.right;
            Vector3 forward=Vector3.Cross(right,axis).normalized;
            const int sides=10;var v=new Vector3[sides*2];var t=new int[sides*6];
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2f/sides;Vector3 radial=right*Mathf.Cos(a)+forward*Mathf.Sin(a);
                v[i]=from+radial*r0;v[i+sides]=to+radial*r1;
                int n=(i+1)%sides,k=i*6;
                t[k]=i;t[k+1]=i+sides;t[k+2]=n;t[k+3]=n;t[k+4]=i+sides;t[k+5]=n+sides;
            }
            var mesh=new Mesh{vertices=v,triangles=t};mesh.RecalculateNormals();Put(name,mesh,mat);
        }
        private void Box(string name,Vector3 center,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=2;
            go.transform.SetParent(visual,false);go.transform.localPosition=center;go.transform.localScale=scale;
            var collider=go.GetComponent<Collider>();
            if(Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            go.GetComponent<Renderer>().sharedMaterial=mat;
        }
        private void Oval(string name,Vector3 center,Vector3 scale,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.layer=2;
            go.transform.SetParent(visual,false);go.transform.localPosition=center;go.transform.localScale=scale;
            var collider=go.GetComponent<Collider>();
            if(Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            go.GetComponent<Renderer>().sharedMaterial=mat;
        }
        private void Triangle(string name,Vector3 a,Vector3 b,Vector3 c,Material mat)
        {
            var mesh=new Mesh{vertices=new[]{a,b,c},triangles=new[]{0,1,2,2,1,0}};
            mesh.RecalculateNormals();Put(name,mesh,mat);
        }
        private void OnDestroy()
        {
            foreach(var mesh in meshes)if(mesh!=null)
            {if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
            foreach(var material in materials)if(material!=null)
            {if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
        }
        private struct Ring
        {
            internal readonly float y,x,z;
            internal Ring(float y,float x,float z){this.y=y;this.x=x;this.z=z;}
        }
    }
}
