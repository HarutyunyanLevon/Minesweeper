using UnityEngine;

namespace MinefieldClerk.Sound
{
    /// <summary>One line of the audio log: a sound that was played, or a marker line between sequences.</summary>
    public struct AudioLogEntry
    {
        public float Time;          // Time.unscaledTime when it played
        public string Name;         // the sound event ("mine_windup"), or the marker text
        public AudioClip Clip;      // the chosen recording; null for built-in sounds and markers
        public float Volume;
        public float Pitch;
        public string Note;         // optional extra ("built-in sound", "variant 3")
        public bool Marker;
    }

    /// <summary>
    /// A rolling record of every sound effect the game plays and which recording it picked (the last 300).
    /// It never allocates (it keeps references only), so it is always on. The Audio Log cheat window reads it.
    /// </summary>
    public static class AudioLog
    {
        public const int Capacity = 300;

        private static readonly AudioLogEntry[] entries = new AudioLogEntry[Capacity];
        private static int next;
        private static int count;

        /// <summary>How many entries are kept right now.</summary>
        public static int Count => count;

        /// <summary>Goes up on every change, so a viewer can tell when to rebuild its text.</summary>
        public static int Serial { get; private set; }

        public static void Add(string name, AudioClip clip, float volume, float pitch, string note = null)
        {
            Push(new AudioLogEntry { Time = Time.unscaledTime, Name = name, Clip = clip, Volume = volume, Pitch = pitch, Note = note });
        }

        /// <summary>A text line in the log, to separate one test from the next.</summary>
        public static void Mark(string text)
        {
            Push(new AudioLogEntry { Time = Time.unscaledTime, Name = text, Marker = true });
        }

        /// <summary>The entry that many places back from the newest one (0 is the newest).</summary>
        public static AudioLogEntry Get(int age)
        {
            return entries[(next - 1 - age + Capacity) % Capacity];
        }

        public static void Clear()
        {
            System.Array.Clear(entries, 0, entries.Length);
            next = 0;
            count = 0;
            Serial++;
        }

        private static void Push(AudioLogEntry entry)
        {
            entries[next] = entry;
            next = (next + 1) % Capacity;
            if (count < Capacity) count++;
            Serial++;
        }
    }
}
