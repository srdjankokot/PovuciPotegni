using System;
using System.IO;
using PovuciPotegni.Core;
using PovuciPotegni.Game;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PovuciPotegni.EditorTools
{
    /// <summary>The game's builds. Test builds run on the sample questions; a store build refuses until the content is ready.</summary>
    public static class BuildTool
    {
        private const string OutputFolder = "Builds";
        private const string GameConfigPath = "Assets/_Project/Settings/GameConfig.asset";
        private const string AndroidTestPath = OutputFolder + "/Android/PovuciPotegni-test.apk";
        private const string AndroidStorePath = OutputFolder + "/Android/PovuciPotegni.aab";
        private const string IosPath = OutputFolder + "/iOS";

        [MenuItem("Povuci-Potegni/Build/Android test APK")]
        public static void AndroidTest()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, AndroidTestPath, BuildOptions.Development);
        }

        [MenuItem("Povuci-Potegni/Build/Android AAB za prodavnicu")]
        public static void AndroidStore()
        {
            RequireReleaseContent();
            if (!PlayerSettings.Android.useCustomKeystore)
            {
                throw new InvalidOperationException("Nema ključa za potpisivanje. Koraci su u docs/IZDANJE.md.");
            }

            EditorUserBuildSettings.buildAppBundle = true;
            Build(BuildTarget.Android, AndroidStorePath, BuildOptions.None);
        }

        [MenuItem("Povuci-Potegni/Build/iOS test (Xcode projekat)")]
        public static void IosTest()
        {
            Build(BuildTarget.iOS, IosPath, BuildOptions.Development);
        }

        [MenuItem("Povuci-Potegni/Build/iOS za TestFlight (Xcode projekat)")]
        public static void IosStore()
        {
            RequireReleaseContent();
            Build(BuildTarget.iOS, IosPath, BuildOptions.None);
        }

        private static void RequireReleaseContent()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath).Config;
            if (config.AllowUnverified)
            {
                throw new InvalidOperationException("AllowUnverified je uključen u GameConfig. Izdanje ne sme da prikazuje neproverena pitanja.");
            }

            QuestionAudit audit = QuestionAudit.Check(File.ReadAllText(QuestionImporter.GameFilePath));
            if (!audit.IsReadyForRelease)
            {
                throw new InvalidOperationException($"Pitanja nisu spremna za izdanje:\n{audit.ToReport()}");
            }
        }

        /// <summary>Only the Game scene ships; Sandbox stays a development tool.</summary>
        private static void Build(BuildTarget target, string path, BuildOptions options)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? OutputFolder);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { SceneBuilder.GameScenePath },
                target = target,
                locationPathName = path,
                options = options,
            });

            BuildSummary summary = report.summary;
            Debug.Log($"Povuci-Potegni build {target}: {summary.result}, {summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalMinutes:F1} min, greške: {summary.totalErrors}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Build nije uspeo: {summary.result}, grešaka {summary.totalErrors}.");
            }
        }
    }
}
