using UnityEditor;
using UnityEditor.Build.Reporting;

// Produces the standalone Windows build that the browser hand-off (see
// game-app's murdermystery:// protocol handler) launches. Run via
// Tools > MurderMystery > Build Windows Player, or in batch mode with
// -executeMethod BuildScript.BuildWindows.
public static class BuildScript
{
    private const string OutputPath = "Builds/Windows/MurderMystery.exe";

    [MenuItem("MurderMystery/Build Windows Player")]
    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = OutputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception($"Build failed: {report.summary.result}, {report.summary.totalErrors} errors");
        }
    }
}
