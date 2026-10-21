using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// Housekeeping tools. "Audit Unused Assets" lists textures and data files under Art and Resources that no scene or prefab uses
    /// and whose name no script mentions (written to audit.txt in the project root). "Delete Files Listed In delete_list.txt" removes
    /// the Assets paths (one per line) listed in that file, through the AssetDatabase so the .meta files go too.
    /// </summary>
    public static class AssetAudit
    {
        private const string AuditFile = "audit.txt", DeleteFile = "delete_list.txt";
        private static readonly string[] Roots = { "Assets/Art", "Assets/Resources" };
        private static readonly string[] Skipped = { ".meta", ".cs" };

        [MenuItem("Minefield/Tools/Audit Unused Assets")]
        public static void Audit()
        {
            var used = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene t:Prefab"))
            {
                foreach (string dep in AssetDatabase.GetDependencies(AssetDatabase.GUIDToAssetPath(guid), true)) used.Add(dep);
            }

            var code = new StringBuilder();
            foreach (string file in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)) code.Append(File.ReadAllText(file));
            string source = code.ToString();

            var report = new StringBuilder();
            foreach (string guid in AssetDatabase.FindAssets("", Roots))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path) || Skipped.Contains(Path.GetExtension(path)) || used.Contains(path)) continue;
                if (source.Contains(Path.GetFileNameWithoutExtension(path))) continue;
                report.AppendLine(path);
            }
            File.WriteAllText(AuditFile, report.ToString());
            Debug.Log("[Audit] Wrote " + AuditFile);
        }

        [MenuItem("Minefield/Tools/Delete Files Listed In delete_list.txt")]
        public static void DeleteListed()
        {
            if (!File.Exists(DeleteFile)) { Debug.LogError("[Audit] No " + DeleteFile); return; }
            foreach (string line in File.ReadAllLines(DeleteFile))
            {
                string path = line.Trim();
                if (path.Length == 0 || !path.StartsWith("Assets/")) continue;
                Debug.Log(AssetDatabase.DeleteAsset(path) ? "[Audit] Deleted " + path : "[Audit] Could not delete " + path);
            }
            AssetDatabase.Refresh();
        }

        private static bool Contains(this string[] list, string value)
        {
            return System.Array.IndexOf(list, value) >= 0;
        }
    }
}
