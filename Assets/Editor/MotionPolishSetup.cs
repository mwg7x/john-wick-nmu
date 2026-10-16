using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastStep.Editor
{
    public static class MotionPolishSetup
    {
        [MenuItem("LAST STEP/Playable/Polish Character Locomotion")]
        public static void Run()
        {
            const string path = "Assets/Resources/CharacterS/RealHuman.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null) throw new System.InvalidOperationException("RealHuman controller missing.");
            var machine = controller.layers[0].stateMachine;
            var idle = machine.states.FirstOrDefault(x => x.state.name == "Idle").state;
            var run = machine.states.FirstOrDefault(x => x.state.name == "Run").state;
            if (idle == null) throw new System.InvalidOperationException("Idle state missing.");
            var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/MakeHuman/Suited/suited.glb").OfType<AnimationClip>().ToArray();
            var idleClip = clips.FirstOrDefault(c => c.name == "idle");
            var walkClip = clips.FirstOrDefault(c => c.name == "walk");
            var runClip = clips.FirstOrDefault(c => c.name == "run");
            if (idleClip == null || walkClip == null || runClip == null)
                throw new System.InvalidOperationException("Expected existing idle/walk/run source clips.");
            var tree = idle.motion as BlendTree;
            bool created = tree == null;
            if (created) tree = new BlendTree
            {
                name = "Grounded Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            if (created) AssetDatabase.AddObjectToAsset(tree, controller);
            tree.children = new[] {
                new ChildMotion { motion=idleClip, threshold=0f, timeScale=1f },
                new ChildMotion { motion=walkClip, threshold=2.8f, timeScale=1.6f },
                new ChildMotion { motion=runClip, threshold=4.8f, timeScale=1f }
            };
            foreach (var transition in idle.transitions.ToArray()) idle.RemoveTransition(transition);
            idle.motion = tree;
            machine.defaultState = idle;
            if (run != null) machine.RemoveState(run);
            EditorUtility.SetDirty(tree);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("LAST STEP: smooth idle-to-movement blend installed.");
        }
    }
}
