using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;

namespace ChronosEscape.EditorTools
{
    public static class BuildPipelinePA4
    {
        [MenuItem("PA4 Game/Compilar Build Ejecutable (.exe)", false, 2)]
        public static void BuildStandaloneWindows()
        {
            Debug.Log("[BuildPipeline] Iniciando compilación de Build Ejecutable Windows Standalone...");

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string buildDirectory = Path.Combine(projectRoot, "..", "Build");
            if (!Directory.Exists(buildDirectory))
            {
                Directory.CreateDirectory(buildDirectory);
            }

            string buildPath = Path.Combine(buildDirectory, "ChronosEscape.exe");

            // Asegurar que la escena MainLevel esté guardada y configurada
            string scenePath = "Assets/Scenes/MainLevel.unity";
            if (!File.Exists(Path.Combine(Application.dataPath, "Scenes/MainLevel.unity")))
            {
                Debug.Log("[BuildPipeline] La escena MainLevel no existe. Generando automáticamente...");
                LevelBuilder.GenerateCompleteLevel();
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { scenePath };
            buildPlayerOptions.locationPathName = buildPath;
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.None;

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildPipeline] ¡COMPILACIÓN EXITOSA! Tamaño: {summary.totalSize / (1024 * 1024)} MB en {buildPath}");
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[BuildPipeline] Error en la compilación: {summary.totalErrors} errores encontrados.");
            }
        }

        [MenuItem("PA4 Game/Regenerar Nivel y Compilar Build (.exe)", false, 3)]
        public static void RebuildLevelAndBuild()
        {
            Debug.Log("[BuildPipeline] Regenerando nivel completo con assets Sci-Fi y 2 NPCs optimizados...");
            LevelBuilder.GenerateCompleteLevel();
            BuildStandaloneWindows();
        }
    }
}
