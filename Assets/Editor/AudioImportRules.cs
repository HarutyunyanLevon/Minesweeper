using System;
using UnityEditor;
using UnityEngine;

namespace MinefieldClerk.EditorTools
{
    /// <summary>
    /// First-import settings for audio under Assets/Audio (later manual changes in the Inspector are kept):
    /// music streams from disk, ambience beds stay compressed in memory, short sounds are decoded on load for instant playback.
    /// </summary>
    public class AudioImportRules : AssetPostprocessor
    {
        private const string Root = "Assets/Audio/";
        private const string MusicFolder = "Assets/Audio/Music/";
        private const string AmbienceFolder = "Assets/Audio/Amb/";
        private const string BedPrefix = "amb_";
        private const float MusicQuality = 0.7f;
        private const float BedQuality = 0.7f;
        private const float SfxQuality = 0.8f;

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal)) return;
            var importer = (AudioImporter)assetImporter;
            if (!importer.importSettingsMissing) return;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;

            if (assetPath.StartsWith(MusicFolder, StringComparison.Ordinal))
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.quality = MusicQuality;
                importer.loadInBackground = true;
            }
            else if (assetPath.StartsWith(AmbienceFolder + BedPrefix, StringComparison.Ordinal))
            {
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.quality = BedQuality;
            }
            else
            {
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.quality = SfxQuality;
            }
            importer.defaultSampleSettings = settings;
        }
    }
}
