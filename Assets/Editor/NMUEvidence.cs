using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastStep.Editor
{
    public static class NMUEvidence
    {
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Duel_NMU.unity");
            var camera=Camera.main;
            camera.transform.position=new Vector3(0,2.6f,1);
            camera.transform.LookAt(new Vector3(0,6.3f,33));
            camera.fieldOfView=60;
            var target=new RenderTexture(1440,1080,24);
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1440,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1440,1080),0,0);image.Apply();
            Directory.CreateDirectory("Documentation/Previews");
            File.WriteAllBytes("Documentation/Previews/nmu-reference-view.png",image.EncodeToPNG());
            camera.targetTexture=null;RenderTexture.active=null;
            Object.DestroyImmediate(image);Object.DestroyImmediate(target);
        }
    }
}
