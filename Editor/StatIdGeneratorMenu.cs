using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StatForge.Editor
{
    /// <summary>
    /// Tools > StatForge > Generate StatIds: scans every StatDefinition asset (derived types included)
    /// and writes <see cref="DefaultOutputPath"/>. Nothing is written when there is any error.
    /// </summary>
    public static class StatIdGeneratorMenu
    {
        public const string DefaultOutputPath = "Assets/Generated/StatIds.cs";

        [MenuItem("Tools/StatForge/Generate StatIds")]
        private static void GenerateMenu()
        {
            try
            {
                bool changed = WriteIfChanged(DefaultOutputPath);
                Debug.Log(changed
                    ? $"StatForge: generated {DefaultOutputPath}"
                    : $"StatForge: {DefaultOutputPath} is already up to date");
            }
            catch (StatIdGenerationException e)
            {
                Debug.LogError(e.Message);
                EditorUtility.DisplayDialog("StatForge", e.Message, "OK");
            }
        }

        /// <summary>Collects (Id, asset path) of every StatDefinition in the project, in asset path order.</summary>
        public static List<StatIdSource> FindSources()
        {
            var sources = new List<StatIdSource>();
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(StatDefinition));

            var paths = new List<string>(guids.Length);
            foreach (string guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            paths.Sort(System.StringComparer.Ordinal);

            foreach (string path in paths)
            {
                // One .asset file can hold several sub-assets; check each.
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is StatDefinition definition)
                    {
                        sources.Add(new StatIdSource(definition.Id.Value, path));
                    }
                }
            }

            return sources;
        }

        /// <summary>Generated text for the current assets.</summary>
        /// <exception cref="StatIdGenerationException"/>
        public static string GenerateFromProject()
        {
            return StatIdCodeGenerator.Generate(FindSources());
        }

        /// <summary>
        /// For a project test: true when the file at <paramref name="outputPath"/> matches what the current assets generate.
        /// </summary>
        public static bool IsUpToDate(string outputPath = DefaultOutputPath)
        {
            return File.Exists(outputPath) && Normalize(File.ReadAllText(outputPath)) == GenerateFromProject();
        }

        /// <returns>True if the file was created or changed.</returns>
        /// <exception cref="StatIdGenerationException"/>
        public static bool WriteIfChanged(string outputPath = DefaultOutputPath)
        {
            string code = GenerateFromProject();

            if (File.Exists(outputPath) && Normalize(File.ReadAllText(outputPath)) == code)
            {
                return false;
            }

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outputPath, code);
            AssetDatabase.ImportAsset(outputPath);
            return true;
        }

        // Git on Windows may check the file out with CRLF; that is not a real difference.
        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n");
        }
    }
}
