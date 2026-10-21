using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using MinefieldClerk.Sound;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// Builds Assets/Resources/Audio/AudioLibrary.asset from the audio folders:
    /// every sub-folder of Assets/Audio/Sfx and Assets/Audio/Amb is a sound event (its files are the random variants),
    /// every file in Assets/Audio/Music is a music track named after the file.
    /// Runs by itself when audio files change, or from the menu. Volumes already set in the asset are kept.
    /// </summary>
    public static class AudioLibraryBuilder
    {
        private const string MenuPath = "Minefield/Audio/Rebuild Library";
        internal const string AudioRoot = "Assets/Audio/";
        private const string MusicFolder = "Assets/Audio/Music";
        private const string ResourcesFolder = "Assets/Resources";
        private const string LibraryFolderName = "Audio";
        private const string ClipFilter = "t:AudioClip";
        private static readonly string[] EventFolders = { "Assets/Audio/Sfx", "Assets/Audio/Amb" };

        private static bool rebuildQueued;

        [InitializeOnLoadMethod]
        private static void CreateIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioLibrary.AssetPath) == null) Rebuild();
            };
        }

        /// <summary>Called by the importer when anything under Assets/Audio changed.</summary>
        public static void QueueRebuild()
        {
            if (rebuildQueued) return;
            rebuildQueued = true;
            EditorApplication.delayCall += () =>
            {
                rebuildQueued = false;
                Rebuild();
            };
        }

        [MenuItem(MenuPath)]
        public static void Rebuild()
        {
            AudioLibrary library = AssetDatabase.LoadAssetAtPath<AudioLibrary>(AudioLibrary.AssetPath);
            if (library == null)
            {
                if (!AssetDatabase.IsValidFolder(AudioLibrary.AssetFolder)) AssetDatabase.CreateFolder(ResourcesFolder, LibraryFolderName);
                library = ScriptableObject.CreateInstance<AudioLibrary>();
                AssetDatabase.CreateAsset(library, AudioLibrary.AssetPath);
            }

            library.events = BuildEvents(library);
            library.music = BuildMusic(library);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sound] Audio library rebuilt: " + library.events.Count + " sound events, " + library.music.Count + " music tracks.");
        }

        private static List<SoundEvent> BuildEvents(AudioLibrary library)
        {
            var old = new Dictionary<string, SoundEvent>();
            foreach (SoundEvent e in library.events) old[e.name] = e;

            var result = new List<SoundEvent>();
            foreach (string root in EventFolders)
            {
                if (!AssetDatabase.IsValidFolder(root)) continue;
                foreach (string folder in AssetDatabase.GetSubFolders(root))
                {
                    AudioClip[] clips = LoadClips(folder);
                    if (clips.Length == 0) continue;

                    string eventName = Path.GetFileName(folder);
                    SoundEvent entry = old.TryGetValue(eventName, out SoundEvent kept) ? kept : new SoundEvent { name = eventName };
                    entry.clips = clips;
                    result.Add(entry);
                }
            }
            result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return result;
        }

        private static List<MusicTrack> BuildMusic(AudioLibrary library)
        {
            var old = new Dictionary<string, MusicTrack>();
            foreach (MusicTrack t in library.music) old[t.name] = t;

            var result = new List<MusicTrack>();
            if (AssetDatabase.IsValidFolder(MusicFolder))
            {
                foreach (string guid in AssetDatabase.FindAssets(ClipFilter, new[] { MusicFolder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string trackName = Path.GetFileNameWithoutExtension(path);
                    MusicTrack entry = old.TryGetValue(trackName, out MusicTrack kept) ? kept : new MusicTrack { name = trackName };
                    entry.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    result.Add(entry);
                }
            }
            result.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return result;
        }

        private static AudioClip[] LoadClips(string folder)
        {
            string[] guids = AssetDatabase.FindAssets(ClipFilter, new[] { folder });
            var paths = new List<string>();
            foreach (string guid in guids) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);

            var clips = new List<AudioClip>();
            foreach (string path in paths)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) clips.Add(clip);
            }
            return clips.ToArray();
        }
    }

    /// <summary>Watches the audio folders so the library follows new, changed and deleted files.</summary>
    public class AudioFolderWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (Touches(imported) || Touches(deleted) || Touches(moved) || Touches(movedFrom)) AudioLibraryBuilder.QueueRebuild();
        }

        private static bool Touches(string[] paths)
        {
            foreach (string path in paths)
            {
                if (path.StartsWith(AudioLibraryBuilder.AudioRoot, StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
