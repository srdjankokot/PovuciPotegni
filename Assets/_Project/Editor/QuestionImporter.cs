using System.IO;
using PovuciPotegni.Core;
using UnityEditor;
using UnityEngine;

namespace PovuciPotegni.EditorTools
{
    /// <summary>Checks the question file and brings a new one into the game, only if it can load.</summary>
    public static class QuestionImporter
    {
        public const string GameFilePath = "Assets/StreamingAssets/questions/sr-Latn.json";

        [MenuItem("Povuci-Potegni/Proveri pitanja")]
        public static void CheckCurrent()
        {
            Debug.Log(QuestionAudit.Check(File.ReadAllText(GameFilePath)).ToReport());
        }

        [MenuItem("Povuci-Potegni/Uvezi pitanja…")]
        public static void ImportWithDialog()
        {
            string source = EditorUtility.OpenFilePanel("Izaberi fajl sa pitanjima", "", "json");
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            QuestionAudit audit = Import(source);
            EditorUtility.DisplayDialog(audit.CanLoad ? "Pitanja su uvezena" : "Pitanja NISU uvezena", audit.ToReport(), "U redu");
        }

        /// <summary>Copies the file into the game only when it can load, and logs the report either way.</summary>
        public static QuestionAudit Import(string sourcePath)
        {
            QuestionAudit audit = QuestionAudit.Check(File.ReadAllText(sourcePath));
            Debug.Log(audit.ToReport());

            if (audit.CanLoad)
            {
                File.Copy(sourcePath, GameFilePath, true);
                AssetDatabase.ImportAsset(GameFilePath);
            }

            return audit;
        }
    }
}
