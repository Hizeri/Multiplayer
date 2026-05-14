using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class DedicatedServerBuilder
{
    private const string OutputPath = "Builds/LinuxServer/practica4_server.x86_64";

    [MenuItem("Practice4/Build Linux Dedicated Server")]
    public static void BuildLinuxServer()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"Dedicated server build failed: {report.summary.result}");
    }
}
