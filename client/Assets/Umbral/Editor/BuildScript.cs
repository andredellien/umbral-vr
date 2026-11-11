using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Umbral.EditorTools
{
    public static class BuildScript
    {
        /// <summary>Build de Quest. Uso: -executeMethod Umbral.EditorTools.BuildScript.Android [-output ruta.apk]</summary>
        public static void Android()
        {
            var output = "Builds/Umbral.apk";
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == "-output") output = args[i + 1];

            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(System.Array.FindAll(EditorBuildSettings.scenes, x => x.enabled), x => x.path),
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
