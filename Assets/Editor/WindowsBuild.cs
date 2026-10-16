using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LastStep.Editor
{
    public static class WindowsBuild
    {
        [MenuItem("LAST STEP/Build/Windows PC (IL2CPP)")]
        public static void Run() { Build(ScriptingImplementation.IL2CPP, "Builds/Windows", "windows-build.txt"); }

        [MenuItem("LAST STEP/Build/Windows PC (Mono fallback)")]
        public static void RunMono() { Build(ScriptingImplementation.Mono2x, "Builds/WindowsMono", "windows-mono-build.txt"); }

        private static void Build(ScriptingImplementation backend, string folder, string reportName)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before building.");
            string[] scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            if (scenes.Length != 4 || !scenes[0].EndsWith("Boot.unity") ||
                !scenes[1].EndsWith("MainMenu.unity") || !scenes[2].EndsWith("Duel_Staircase.unity") ||
                !scenes[3].EndsWith("Duel_NMU.unity"))
                throw new InvalidOperationException("Expected Boot, MainMenu, Duel_Staircase, Duel_NMU in build order.");
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory("Logs");
            var previous = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = folder + "/LAST STEP.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            try
            {
                BuildReport report = BuildPipeline.BuildPlayer(options);
                string result = report.summary.result.ToString();
                File.WriteAllText("Logs/" + reportName, "Unity: " + Application.unityVersion + "\nBackend: " + backend +
                    "\nResult: " + result + "\nErrors: " + report.summary.totalErrors +
                    "\nWarnings: " + report.summary.totalWarnings +
                    "\nOutput: " + Path.GetFullPath(options.locationPathName) + "\n");
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Windows build " + result + "; see Logs/" + reportName + " and Unity Console.");
                Debug.Log("LAST STEP WINDOWS BUILD PASSED: " + options.locationPathName);
            }
            finally { PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, previous); }
        }
    }
}
