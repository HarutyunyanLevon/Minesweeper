using UnityEngine;
using UnityEngine.InputSystem;

namespace MinefieldClerk.DevTools
{
    /// <summary>Master switch for every debug readout and cheat. Toggled with F10 or the small DEV button.</summary>
    public static class DevMode
    {
        public const Key ToggleKey = Key.F10;

        /// <summary>True while debug texts and cheats are active.</summary>
        public static bool Enabled { get; private set; } = true;

        public static void Toggle()
        {
            Enabled = !Enabled;
        }

        /// <summary>Call every frame from one place to listen for the toggle key.</summary>
        public static void PollKey()
        {
            var kb = Keyboard.current;
            if (kb != null && kb[ToggleKey].wasPressedThisFrame) Toggle();
        }
    }
}
