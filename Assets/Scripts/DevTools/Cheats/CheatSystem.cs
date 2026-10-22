using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using MinefieldClerk.Field;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>
    /// The cheat host. Creates itself in any scene that has a FieldSession and draws the toolbar and every cheat window
    /// while dev mode (F10) is on. It also owns the cheat hotkeys, the key-binding mode and the UI scale / opacity.
    /// To add a window: write a CheatWindow subclass and add it to <see cref="CreateWindows"/>.
    /// </summary>
    public sealed class CheatSystem : MonoBehaviour
    {
        private const string ObjectName = "Cheats";
        private const string ScaleKey = "ui.scale", OpacityKey = "ui.opacity";

        public static CheatSystem Instance { get; private set; }

        /// <summary>True while a text field has the keyboard or a hotkey is being bound: the game ignores its keys.</summary>
        public static bool Typing { get; private set; }

        private readonly List<CheatWindow> windows = new List<CheatWindow>();
        private GUISkin skin;
        private float uiScale = CheatLayout.DefaultUiScale;
        private float opacity = CheatLayout.DefaultOpacity;

        public IReadOnlyList<CheatWindow> Windows => windows;

        /// <summary>The cheat waiting for its new hotkey (set by the hotkey buttons).</summary>
        public Cheat BindingTarget { get; set; }

        public float UiScale
        {
            get => uiScale;
            set
            {
                uiScale = Mathf.Clamp(value, CheatLayout.MinUiScale, CheatLayout.MaxUiScale);
                CheatPrefs.SetFloat(ScaleKey, uiScale);
            }
        }

        public float Opacity
        {
            get => opacity;
            set
            {
                opacity = Mathf.Clamp(value, CheatLayout.MinOpacity, 1f);
                CheatPrefs.SetFloat(OpacityKey, opacity);
            }
        }

        /// <summary>Every cheat of every window.</summary>
        public IEnumerable<Cheat> AllCheats
        {
            get
            {
                foreach (CheatWindow window in windows)
                {
                    foreach (Cheat cheat in window.Cheats.Items) yield return cheat;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Instance != null || FindAnyObjectByType<FieldSession>() == null) return;
            new GameObject(ObjectName).AddComponent<CheatSystem>();
        }

        /// <summary>Every cheat window, in toolbar order.</summary>
        private static CheatWindow[] CreateWindows()
        {
            return new CheatWindow[]
            {
                new GameplayCheatWindow(),
                new DayCheatWindow(),
                new RulesCheatWindow(),
                new AudioCheatWindow(),
                new AudioLogCheatWindow(),
                new UiCheatWindow(),
                new OverviewCheatWindow(),
            };
        }

        private void Awake()
        {
            Instance = this;
            uiScale = Mathf.Clamp(CheatPrefs.GetFloat(ScaleKey, CheatLayout.DefaultUiScale), CheatLayout.MinUiScale, CheatLayout.MaxUiScale);
            opacity = Mathf.Clamp(CheatPrefs.GetFloat(OpacityKey, CheatLayout.DefaultOpacity), CheatLayout.MinOpacity, 1f);
            windows.AddRange(CreateWindows());
            for (int i = 0; i < windows.Count; i++) windows[i].Init(i);
        }

        private void OnDestroy()
        {
            foreach (CheatWindow window in windows)
            {
                window.SaveLayout();
                window.OnQuit();
            }
            if (CheatState.GameSpeedTouched) Time.timeScale = CheatState.NormalFactor;
            PlayerPrefs.Save();
            if (Instance == this) Instance = null;
            Typing = false;
        }

        /// <summary>Resets every window to its starting place and size.</summary>
        public void ResetLayouts()
        {
            foreach (CheatWindow window in windows) window.ResetLayout();
            UiScale = CheatLayout.DefaultUiScale;
            Opacity = CheatLayout.DefaultOpacity;
        }

        /// <summary>Forgets every remembered cheat setting, hotkey and layout.</summary>
        public void ForgetEverything()
        {
            foreach (CheatWindow window in windows) window.ForgetSaved();
            ResetLayouts();
        }

        /// <summary>True when a screen position (origin bottom-left, like the mouse) is over the toolbar or a cheat window.</summary>
        public static bool IsOverUi(Vector2 screen)
        {
            return Instance != null && Instance.OverUi(screen);
        }

        private bool OverUi(Vector2 screen)
        {
            if (!DevMode.Enabled) return false;
            var point = new Vector2(screen.x, Screen.height - screen.y);
            if (ToolbarRect().Contains(point)) return true;
            Vector2 scaled = point / uiScale;
            foreach (CheatWindow window in windows)
            {
                if (window.IsOpen && window.ShownRect.Contains(scaled)) return true;
            }
            return false;
        }

        private Rect ToolbarRect()
        {
            float width = windows.Count * (CheatLayout.ToolbarButtonWidth + CheatLayout.ToolbarGap);
            return new Rect(CheatLayout.ToolbarLeft, Screen.height - CheatLayout.ToolbarHeight - CheatLayout.ToolbarBottom, width, CheatLayout.ToolbarHeight);
        }

        private void Update()
        {
            ApplyGameSpeed();
            if (!DevMode.Enabled) return;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                HandleBinding(kb);
                if (!Typing) PollHotkeys(kb);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && !mouse.leftButton.isPressed)
            {
                foreach (CheatWindow window in windows) window.EndDrag();
            }

            foreach (CheatWindow window in windows) window.OnUpdate();
        }

        private static void ApplyGameSpeed()
        {
            if (!CheatState.GameSpeedTouched) return;
            float wanted = DevMode.Enabled ? CheatState.GameSpeed : CheatState.NormalFactor;
            if (!Mathf.Approximately(Time.timeScale, wanted)) Time.timeScale = wanted;
        }

        private void PollHotkeys(Keyboard kb)
        {
            for (int w = 0; w < windows.Count; w++)
            {
                List<Cheat> items = windows[w].Cheats.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    Cheat cheat = items[i];
                    if (!cheat.CanBindKey || cheat.Hotkey == Key.None) continue;
                    if (kb[cheat.Hotkey].wasPressedThisFrame) cheat.Trigger();
                }
            }
        }

        private void HandleBinding(Keyboard kb)
        {
            if (BindingTarget == null) return;
            if (kb.escapeKey.wasPressedThisFrame)
            {
                BindingTarget.SetHotkey(Key.None);
                BindingTarget = null;
                return;
            }
            foreach (KeyControl key in kb.allKeys)
            {
                if (!key.wasPressedThisFrame) continue;
                BindingTarget.SetHotkey(key.keyCode);
                BindingTarget = null;
                return;
            }
        }

        private void OnGUI()
        {
            if (!DevMode.Enabled)
            {
                Typing = false;
                return;
            }

            GUI.depth = CheatLayout.GuiDepth;
            if (skin == null) skin = BuildSkin();

            GUISkin previousSkin = GUI.skin;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.skin = skin;

            // A click leaves any text field; a click on a text field takes the keyboard again during the same event.
            if (Event.current.type == EventType.MouseDown) GUIUtility.keyboardControl = 0;

            foreach (CheatWindow window in windows)
            {
                if (window.IsOpen) window.DrawOverlay();
            }

            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));
            GUI.color = new Color(1f, 1f, 1f, opacity);
            foreach (CheatWindow window in windows) window.Draw(uiScale);

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            DrawToolbar();

            Typing = GUIUtility.keyboardControl != 0 || BindingTarget != null;
            GUI.skin = previousSkin;
        }

        private void DrawToolbar()
        {
            GUILayout.BeginArea(ToolbarRect());
            GUILayout.BeginHorizontal();
            foreach (CheatWindow window in windows)
            {
                bool open = GUILayout.Toggle(window.IsOpen, window.Title, GUI.skin.button, GUILayout.Width(CheatLayout.ToolbarButtonWidth), GUILayout.Height(CheatLayout.ToolbarHeight));
                if (open != window.IsOpen) window.SetOpen(open);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private GUISkin BuildSkin()
        {
            GUISkin built = Instantiate(GUI.skin);
            GUIStyle[] styles = { built.label, built.button, built.toggle, built.textField, built.textArea, built.box, built.window };
            foreach (GUIStyle style in styles) style.fontSize = CheatLayout.FontSize;
            built.window.padding = new RectOffset(
                Mathf.RoundToInt(CheatLayout.WindowPadding),
                Mathf.RoundToInt(CheatLayout.WindowPadding),
                Mathf.RoundToInt(CheatLayout.TitleHeight),
                Mathf.RoundToInt(CheatLayout.WindowPadding));
            built.window.alignment = TextAnchor.UpperCenter;
            built.window.fontStyle = FontStyle.Bold;
            built.box.alignment = TextAnchor.MiddleCenter;
            return built;
        }
    }
}
