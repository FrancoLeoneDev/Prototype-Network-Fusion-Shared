using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless build entry points for CI and the Unity CLI.
/// </summary>
/// <example>
/// unity build . --target WebGL --execute-method Builder.BuildWebGL
/// </example>
public static class Builder
{
    private const string DefaultWebGLOutput = "Builds/WebGL";
    private const string OutputArgument = "-buildOutput";

    [MenuItem("Build/WebGL")]
    public static void BuildWebGL()
    {
        // itch.io serves builds as static files without the headers Unity's
        // compressed loader expects, so shipping uncompressed avoids the
        // "failed to download" error that Brotli/Gzip builds hit there.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

        Build(BuildTarget.WebGL, ResolveOutputPath(DefaultWebGLOutput));
    }

    private static void Build(BuildTarget target, string outputPath)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Fail("No enabled scenes in Build Settings.");
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {outputPath} " +
                      $"({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime})");
            return;
        }

        Fail($"Build {summary.result} with {summary.totalErrors} error(s).");
    }

    /// <summary>Reads <c>-buildOutput &lt;path&gt;</c> from the command line, if present.</summary>
    private static string ResolveOutputPath(string fallback)
    {
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == OutputArgument)
            {
                return args[i + 1];
            }
        }

        return fallback;
    }

    private static void Fail(string message)
    {
        Debug.LogError(message);

        // Non-zero exit so CI and the Unity CLI report the failure.
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(1);
        }
    }
}
