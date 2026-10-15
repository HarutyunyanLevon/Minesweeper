using System;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace MinefieldClerk.EditorTools
{
    [Serializable] public class FetchItem { public string url; public string file; }
    [Serializable] public class FetchQueue { public FetchItem[] items; }

    /// <summary>
    /// Downloads files listed in RawDownloads/queue.json (outside Assets, so Unity does not import them)
    /// into RawDownloads/. Used to move SpriteCook assets onto this machine. Menu: Minefield/Assets/Fetch Queue
    /// </summary>
    public static class SpriteCookFetch
    {
        [MenuItem("Minefield/Assets/Fetch Queue")]
        public static void Fetch()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "RawDownloads"));
            string queuePath = Path.Combine(root, "queue.json");
            if (!File.Exists(queuePath))
            {
                Debug.LogError("[Fetch] No queue at " + queuePath);
                return;
            }

            FetchQueue queue = JsonUtility.FromJson<FetchQueue>(File.ReadAllText(queuePath));
            int ok = 0, failed = 0;
            foreach (FetchItem item in queue.items)
            {
                string target = Path.Combine(root, item.file);
                using (UnityWebRequest req = UnityWebRequest.Get(item.url))
                {
                    UnityWebRequestAsyncOperation op = req.SendWebRequest();
                    while (!op.isDone) Thread.Sleep(30);
                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        File.WriteAllBytes(target, req.downloadHandler.data);
                        ok++;
                    }
                    else
                    {
                        failed++;
                        Debug.LogError("[Fetch] " + item.file + ": " + req.error);
                    }
                }
            }
            Debug.Log("[Fetch] Done: " + ok + " downloaded, " + failed + " failed.");
        }
    }
}
