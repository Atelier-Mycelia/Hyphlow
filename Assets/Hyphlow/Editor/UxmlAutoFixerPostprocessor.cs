using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AtMycelia.Hyphlow.EditorExt
{
    public class HyphlowUxmlAutoFixerPostprocessor : AssetPostprocessor
    {
        private static bool hasRun = false;

        // Matches src="...guid=xxxxxxxx..."
        private static readonly Regex StyleSrcRegex =
            new Regex(
                @"src\s*=\s*[""'](?<url>[^""']*guid=[a-fA-F0-9]{32}[^""']*)[""']",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromPaths)
        {
            if (hasRun)
                return;

            // Trigger only when Hyphlow package assets are imported
            foreach (var path in importedAssets)
            {
                if (path.StartsWith("Packages/com.ateliermycelia.hyphlow"))
                {
                    hasRun = true;
                    RunFixer();
                    break;
                }
            }
        }

        private static void RunFixer()
        {
            Debug.Log("[Hyphlow] Running automatic UXML path fixer...");

            // Find all UXMLs inside the Hyphlow package
            string[] guids = AssetDatabase.FindAssets("t:VisualTreeAsset",
                new[] { "Packages/com.ateliermycelia.hyphlow" });

            int fixedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".uxml"))
                    continue;

                string text = File.ReadAllText(path);
                string newText = RewriteUxml(text);

                if (newText != text)
                {
                    File.WriteAllText(path, newText);
                    AssetDatabase.ImportAsset(path);
                    fixedCount++;
                    Debug.Log($"[Hyphlow] Updated UXML: {path}");
                }
            }

            Debug.Log($"[Hyphlow] UXML auto-fix complete. Updated {fixedCount} files.");
        }

        private static string RewriteUxml(string text)
        {
            return StyleSrcRegex.Replace(text, match =>
            {
                string url = match.Groups["url"].Value;

                // Extract guid
                var guidMatch = Regex.Match(url, @"guid=(?<guid>[a-fA-F0-9]{32})", 
                    RegexOptions.IgnoreCase);
                if (!guidMatch.Success)
                    return match.Value;

                string guid = guidMatch.Groups["guid"].Value;

                // Resolve correct asset path
                string correctFullPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(correctFullPath))
                    return match.Value;

                // Build Unity-style project://database/ URI
                string databaseUri = "project://database/" + correctFullPath;

                // Extract filename (for fragment)
                string fileName = Path.GetFileNameWithoutExtension(correctFullPath);

                // Split URL into query and fragment
                string query = "";
                string fragment = "#" + fileName;

                int qIndex = url.IndexOf('?');
                if (qIndex >= 0)
                {
                    int hashIndex = url.IndexOf('#');
                    int endOfQuery = (hashIndex >= 0 ? hashIndex : url.Length);
                    query = url.Substring(qIndex, endOfQuery - qIndex);
                }

                // Update guid inside query
                if (!string.IsNullOrEmpty(query))
                {
                    query = Regex.Replace(
                        query,
                        @"guid=([a-fA-F0-9]{32})",
                        $"guid={guid}",
                        RegexOptions.IgnoreCase);
                }

                // Rebuild final URL
                string newUrl = databaseUri + query + fragment;

                return $"src=\"{newUrl}\"";
            });
        }
    }


}