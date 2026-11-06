#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BuildWindows
{
    const string Flag = "build.flag";

    static BuildWindows()
    {
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!File.Exists(Flag)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        File.Delete(Flag);
        Build();
    }

    [MenuItem("Tools/Build Windows")]
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        Directory.CreateDirectory("Build/Windows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Build/Windows/Battleship.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        File.WriteAllText("Build/build_result.txt", report.summary.result + " errors=" + report.summary.totalErrors);
    }
}
#endif
