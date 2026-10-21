using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Field;
using MinefieldClerk.Sound;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>
    /// Plays, stops, pauses and replays any sound: every sound effect and each of its variants, every music track and every
    /// ambience bed, plus the master levels and mix numbers. Changes to the audio library last until Play mode ends.
    /// </summary>
    public sealed class AudioCheatWindow : CheatWindow
    {
        private const string BedPrefix = "amb_";
        private const int MaxVariantButtons = 8;
        private const float SmallButtonWidth = 52f;
        private const float VariantButtonWidth = 28f;
        private const float JitterLabelWidth = 84f;
        private const float TimeLabelWidth = 90f;
        private const float SeekEndMarginSeconds = 0.05f;   // keeps a seek off the last sample, which would stop the source
        private const string ValueFormat = "0.00", TimeFormat = "0.0";
        private const string NotRunning = "The sound system is not running in this scene.";
        private const string Hint = "Music and ambience normally follow the day. Pick one here to force it (it loops until you press Auto). Edits to levels last until you leave Play mode.";

        private SoundDirector sound;
        private MusicDirector music;
        private AmbienceDirector ambience;
        private MineAudio mineAudio;
        private AudioLibrary library;
        private AudioLibrary touchedLibrary;
        private string snapshot;

        private AudioSource musicSource, ambienceSource;
        private string status = "";
        private string sfxFilter = "";
        private readonly List<string> beds = new List<string>();
        private bool bedsBuilt;

        public override string Id => "audio";
        public override string Title => "Audio";
        public override Key DefaultToggleKey => Key.F5;

        public override void OnUpdate()
        {
            sound = SoundDirector.Instance;
            music = MusicDirector.Instance;
            ambience = AmbienceDirector.Instance;
            Find(ref mineAudio);
            library = SoundDirector.CurrentLibrary;
            musicSource = music != null ? music.Source : null;
            ambienceSource = ambience != null ? ambience.Source : null;

            if (library != null && !bedsBuilt)
            {
                bedsBuilt = true;
                foreach (SoundEvent e in library.events)
                {
                    if (e.name != null && e.name.StartsWith(BedPrefix, StringComparison.Ordinal)) beds.Add(e.name);
                }
            }

            status = sound == null ? NotRunning : "Sound voices playing: " + sound.PlayingVoices;
        }

        public override void OnQuit()
        {
            if (snapshot == null || touchedLibrary == null) return;
            try
            {
                JsonUtility.FromJsonOverwrite(snapshot, touchedLibrary);
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning("[Cheats] Could not restore the audio library after cheating: " + e.Message);
            }
        }

        /// <summary>Remembers the library as it was before the first cheat edit, so it can be restored when Play mode ends.</summary>
        private void Touch()
        {
            if (snapshot != null || library == null) return;
            touchedLibrary = library;
            snapshot = JsonUtility.ToJson(library);
        }

        protected override void Build(CheatList list)
        {
            list.Section("Quick controls");
            list.Button("Stop all sounds", StopAll, "Stops every effect, silences music and ambience", null).Hot(Key.F8);
            list.Button("Stop effects only", StopEffects);
            list.Button("Resume automatic audio", ResumeAuto, "Music and ambience follow the day again");
            list.Toggle("Pause all audio", () => AudioListener.pause, v => AudioListener.pause = v);

            list.Section("Master levels");
            list.Float("Global volume", () => AudioListener.volume, v => AudioListener.volume = v, 0f, 1f, CheatLimits.UnitStep, 1f).Saved();
            list.Float("Effects master", () => library != null ? library.sfxMaster : 0f, v => { Touch(); if (library != null) library.sfxMaster = v; }, 0f, 1f, CheatLimits.UnitStep, 1f);
            list.Float("Music master", () => library != null ? library.musicMaster : 0f, v => { Touch(); if (library != null) library.musicMaster = v; }, 0f, 1f, CheatLimits.UnitStep, 1f);

            list.Section("Mix");
            list.Float("Music fade (s)", () => library != null ? library.musicFadeSeconds : 0f, v => { Touch(); if (library != null) library.musicFadeSeconds = v; }, 0f, CheatLimits.MaxFadeSeconds, CheatLimits.UnitStep, AudioLibrary.DefaultFadeSeconds);
            list.Float("Footstep gap (s)", () => library != null ? library.footstepSeconds : 0f, v => { Touch(); if (library != null) library.footstepSeconds = v; }, 0f, CheatLimits.MaxFadeSeconds, CheatLimits.UnitStep, AudioLibrary.DefaultStepSeconds);
            list.Float("Wind-up pitch", () => library != null ? library.windupPitch : 0f, v => { Touch(); if (library != null) library.windupPitch = v; }, AudioLibrary.MinWindupPitch, AudioLibrary.MaxWindupPitch, CheatLimits.UnitStep, AudioLibrary.DefaultWindupPitch, "Playback speed of the mine fuse clockwork");
            list.Float("Fuse duck level", () => library != null ? library.fuseDuckLevel : 0f, v => { Touch(); if (library != null) library.fuseDuckLevel = v; }, 0f, 1f, CheatLimits.UnitStep, AudioLibrary.DefaultFuseDuck, "Music and wind level while a fuse burns");
            list.Float("Duck fade (s)", () => library != null ? library.duckFadeSeconds : 0f, v => { Touch(); if (library != null) library.duckFadeSeconds = v; }, 0f, CheatLimits.MaxFadeSeconds, CheatLimits.UnitStep, AudioLibrary.DefaultDuckSeconds);
            list.Float("Leaves gap min (s)", () => library != null ? library.leavesGapSeconds.x : 0f, v => { Touch(); if (library != null) library.leavesGapSeconds.x = v; }, 0f, CheatLimits.MaxLeavesGapSeconds, 1f, AudioLibrary.DefaultLeavesMinSeconds);
            list.Float("Leaves gap max (s)", () => library != null ? library.leavesGapSeconds.y : 0f, v => { Touch(); if (library != null) library.leavesGapSeconds.y = v; }, 0f, CheatLimits.MaxLeavesGapSeconds, 1f, AudioLibrary.DefaultLeavesMaxSeconds);
        }

        // ------------------------------------------------------------------ quick actions

        private void StopEffects()
        {
            if (sound != null) sound.StopAllVoices();
            if (mineAudio != null) mineAudio.StopAllSounds();
        }

        private void StopAll()
        {
            StopEffects();
            if (music != null) music.Mode = PlaybackMode.Silent;
            if (ambience != null) ambience.Mode = PlaybackMode.Silent;
        }

        private void ResumeAuto()
        {
            if (music != null)
            {
                music.Mode = PlaybackMode.Auto;
                music.Paused = false;
            }
            if (ambience != null)
            {
                ambience.Mode = PlaybackMode.Auto;
                ambience.Paused = false;
            }
        }

        // ------------------------------------------------------------------ drawing

        protected override void DrawBody()
        {
            GUILayout.Label(status, CheatGui.Wrap);
            GUILayout.Label(Hint, CheatGui.Subtle);
            DrawCheatList();
            if (library == null) return;
            DrawMusic();
            DrawAmbience();
            DrawEffects();
        }

        private void DrawMusic()
        {
            CheatGui.Heading("Music");
            if (music == null)
            {
                GUILayout.Label(NotRunning);
                return;
            }
            GUILayout.Label("Mode " + music.Mode + "   Day picks: " + music.AutoTrack + "   Playing: " + (music.PlayingTrack ?? "nothing"), CheatGui.Wrap);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto")) { music.Mode = PlaybackMode.Auto; music.Paused = false; }
            if (GUILayout.Button("Silence")) music.Mode = PlaybackMode.Silent;
            if (GUILayout.Button(music.Paused ? "Resume" : "Pause")) music.Paused = !music.Paused;
            if (GUILayout.Button("Restart")) music.Restart();
            GUILayout.EndHorizontal();
            DrawSeek(musicSource);

            foreach (MusicTrack track in library.music)
            {
                GUILayout.BeginHorizontal();
                bool current = music.PlayingTrack == track.name;
                GUILayout.Label((current ? "> " : "") + track.name + (track.clip == null ? " (no clip)" : ""), GUILayout.Width(CheatLayout.LabelWidth));
                if (GUILayout.Button("Play", GUILayout.Width(SmallButtonWidth))) music.Force(track.name);
                float volume = GUILayout.HorizontalSlider(track.volume, 0f, 1f);
                GUILayout.Label(track.volume.ToString(ValueFormat), GUILayout.Width(CheatLayout.FieldWidth));
                GUILayout.EndHorizontal();
                if (!Mathf.Approximately(volume, track.volume))
                {
                    Touch();
                    track.volume = CheatGui.Round(volume);
                }
            }
        }

        private void DrawAmbience()
        {
            CheatGui.Heading("Ambience");
            if (ambience == null)
            {
                GUILayout.Label(NotRunning);
                return;
            }
            GUILayout.Label("Mode " + ambience.Mode + "   Day picks: " + ambience.AutoBed + "   Playing: " + (ambience.PlayingBed ?? "nothing"), CheatGui.Wrap);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Auto")) { ambience.Mode = PlaybackMode.Auto; ambience.Paused = false; }
            if (GUILayout.Button("Silence")) ambience.Mode = PlaybackMode.Silent;
            if (GUILayout.Button(ambience.Paused ? "Resume" : "Pause")) ambience.Paused = !ambience.Paused;
            if (GUILayout.Button("Restart")) ambience.Restart();
            if (GUILayout.Button("Other take")) ambience.NewVariant();
            GUILayout.EndHorizontal();
            DrawSeek(ambienceSource);

            ambience.LeavesEnabled = GUILayout.Toggle(ambience.LeavesEnabled, "Random leaf rustles in the field");
            if (GUILayout.Button("Rustle now") && sound != null) sound.PlayEvent(SfxEvent.AmbLeaves, 1f);

            foreach (string bed in beds)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label((ambience.PlayingBed == bed ? "> " : "") + bed, GUILayout.Width(CheatLayout.WideLabelWidth));
                if (GUILayout.Button("Play", GUILayout.Width(SmallButtonWidth))) ambience.Force(bed);
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawSeek(AudioSource source)
        {
            if (source == null || source.clip == null) return;
            GUILayout.BeginHorizontal();
            float length = source.clip.length;
            bool wasChanged = GUI.changed;
            GUI.changed = false;
            float position = GUILayout.HorizontalSlider(source.time, 0f, length);
            bool dragged = GUI.changed;
            GUI.changed |= wasChanged;
            GUILayout.Label(source.time.ToString(TimeFormat) + " / " + length.ToString(TimeFormat) + " s", GUILayout.Width(TimeLabelWidth));
            GUILayout.EndHorizontal();
            if (dragged) source.time = Mathf.Clamp(position, 0f, Mathf.Max(0f, length - SeekEndMarginSeconds));
        }

        private void DrawEffects()
        {
            CheatGui.Heading("Sound effects");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Filter", GUILayout.Width(CheatLayout.FieldWidth));
            sfxFilter = GUILayout.TextField(sfxFilter);
            GUILayout.EndHorizontal();

            foreach (SoundEvent sfx in library.events)
            {
                if (sfx.name == null || sfx.name.StartsWith(BedPrefix, StringComparison.Ordinal)) continue;
                if (!string.IsNullOrEmpty(sfxFilter) && sfx.name.IndexOf(sfxFilter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                DrawEffect(sfx);
            }
        }

        private void DrawEffect(SoundEvent sfx)
        {
            int clips = sfx.clips != null ? sfx.clips.Length : 0;
            GUILayout.BeginHorizontal();
            GUILayout.Label(sfx.name + " (" + clips + ")", GUILayout.Width(CheatLayout.WideLabelWidth));
            bool previous = GUI.enabled;
            GUI.enabled = previous && clips > 0 && sound != null;
            if (GUILayout.Button("Play", GUILayout.Width(SmallButtonWidth))) sound.PlayEvent(sfx.name, 1f);
            for (int i = 0; i < Mathf.Min(clips, MaxVariantButtons); i++)
            {
                if (GUILayout.Button((i + 1).ToString(), GUILayout.Width(VariantButtonWidth))) sound.PlayVariant(sfx.name, i, 1f);
            }
            GUI.enabled = previous;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(CheatLayout.IndentWidth);
            GUILayout.Label("level", GUILayout.Width(CheatLayout.FieldWidth));
            float volume = GUILayout.HorizontalSlider(sfx.volume, 0f, 1f);
            GUILayout.Label("pitch jitter", GUILayout.Width(JitterLabelWidth));
            float jitter = GUILayout.HorizontalSlider(sfx.pitchJitter, 0f, AudioLibrary.MaxPitchJitter);
            GUILayout.EndHorizontal();
            if (!Mathf.Approximately(volume, sfx.volume))
            {
                Touch();
                sfx.volume = CheatGui.Round(volume);
            }
            if (!Mathf.Approximately(jitter, sfx.pitchJitter))
            {
                Touch();
                sfx.pitchJitter = CheatGui.Round(jitter);
            }
        }
    }
}
