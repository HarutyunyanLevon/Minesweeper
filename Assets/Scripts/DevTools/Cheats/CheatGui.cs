using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>Small IMGUI building blocks shared by every cheat window.</summary>
    public static class CheatGui
    {
        private const string FloatFormat = "0.###";
        private const string OpenMark = "v  ", ClosedMark = ">  ";

        private static readonly Dictionary<string, string> buffers = new Dictionary<string, string>();
        private static readonly HashSet<string> collapsed = new HashSet<string>();
        private static GUIStyle wrapStyle, subtleStyle, selectedStyle, headingStyle;

        public static float Round(float value)
        {
            return Mathf.Round(value * CheatLimits.FloatPrecision) / CheatLimits.FloatPrecision;
        }

        // ------------------------------------------------------------------ styles

        public static GUIStyle Wrap
        {
            get
            {
                if (wrapStyle == null) wrapStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
                return wrapStyle;
            }
        }

        public static GUIStyle Subtle
        {
            get
            {
                if (subtleStyle == null)
                {
                    subtleStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
                    subtleStyle.normal.textColor = CheatLayout.SubtleTextColor;
                }
                return subtleStyle;
            }
        }

        public static GUIStyle Selected
        {
            get
            {
                if (selectedStyle == null)
                {
                    selectedStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
                    selectedStyle.normal.textColor = CheatLayout.SelectedRowColor;
                }
                return selectedStyle;
            }
        }

        // ------------------------------------------------------------------ layout helpers

        public static void Space() { GUILayout.Space(CheatLayout.SectionGap); }

        public static void Heading(string text)
        {
            if (headingStyle == null)
            {
                headingStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                headingStyle.normal.textColor = CheatLayout.SelectedRowColor;
            }
            GUILayout.Space(CheatLayout.SectionGap);
            GUILayout.Label(text, headingStyle);
        }

        /// <summary>A collapsible section. Returns true while it is open. The open state is kept for the whole session.</summary>
        public static bool Foldout(string key, string title)
        {
            bool open = !collapsed.Contains(key);
            bool next = GUILayout.Toggle(open, (open ? OpenMark : ClosedMark) + title, GUI.skin.button);
            if (next != open)
            {
                if (next) collapsed.Remove(key);
                else collapsed.Add(key);
            }
            return next;
        }

        public static void Indent(Action body)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(CheatLayout.IndentWidth);
            GUILayout.BeginVertical();
            body();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------ number fields

        public static int IntField(string control, int value, float width)
        {
            GUI.SetNextControlName(control);
            bool focused = GUI.GetNameOfFocusedControl() == control;
            if (focused) ReleaseOnEnter();
            string shown = focused && buffers.TryGetValue(control, out string buffered) ? buffered : value.ToString(CultureInfo.InvariantCulture);
            string edited = GUILayout.TextField(shown, GUILayout.Width(width));
            if (!focused)
            {
                buffers.Remove(control);
                return value;
            }
            buffers[control] = edited;
            return int.TryParse(edited, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : value;
        }

        public static float FloatField(string control, float value, float width)
        {
            GUI.SetNextControlName(control);
            bool focused = GUI.GetNameOfFocusedControl() == control;
            if (focused) ReleaseOnEnter();
            string shown = focused && buffers.TryGetValue(control, out string buffered) ? buffered : value.ToString(FloatFormat, CultureInfo.InvariantCulture);
            string edited = GUILayout.TextField(shown, GUILayout.Width(width));
            if (!focused)
            {
                buffers.Remove(control);
                return value;
            }
            buffers[control] = edited;
            return float.TryParse(edited, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : value;
        }

        /// <summary>Enter or Escape leaves the text field, so the game keys work again.</summary>
        private static void ReleaseOnEnter()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.keyCode != KeyCode.Return && e.keyCode != KeyCode.KeypadEnter && e.keyCode != KeyCode.Escape) return;
            GUI.FocusControl(null);
            e.Use();
        }

        // ------------------------------------------------------------------ one-off rows (not registered cheats)

        public static float FloatRow(string id, string label, float value, float min, float max, float step)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(CheatLayout.LabelWidth));
            float next = GUILayout.HorizontalSlider(value, min, max);
            next = FloatField(id, next, CheatLayout.FieldWidth);
            if (GUILayout.Button(CheatLayout.StepDownText, GUILayout.Width(CheatLayout.StepButtonWidth))) next -= step;
            if (GUILayout.Button(CheatLayout.StepUpText, GUILayout.Width(CheatLayout.StepButtonWidth))) next += step;
            GUILayout.EndHorizontal();
            return Round(Mathf.Clamp(next, min, max));
        }

        public static int IntRow(string id, string label, int value, int min, int max, int step)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(CheatLayout.LabelWidth));
            int next = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max));
            next = IntField(id, next, CheatLayout.FieldWidth);
            if (GUILayout.Button(CheatLayout.StepDownText, GUILayout.Width(CheatLayout.StepButtonWidth))) next -= step;
            if (GUILayout.Button(CheatLayout.StepUpText, GUILayout.Width(CheatLayout.StepButtonWidth))) next += step;
            GUILayout.EndHorizontal();
            return Mathf.Clamp(next, min, max);
        }

        // ------------------------------------------------------------------ cheat lists

        /// <summary>Draws one cheat with its hotkey button.</summary>
        public static void DrawCheat(Cheat cheat)
        {
            GUILayout.BeginHorizontal();
            cheat.Draw();
            if (cheat.CanBindKey) HotkeyButton(cheat);
            GUILayout.EndHorizontal();
        }

        /// <summary>Draws cheats grouped under foldouts. A non-empty filter shows only the matching cheats and opens every group.</summary>
        public static void DrawCheats(IReadOnlyList<Cheat> cheats, string scope, string filter)
        {
            bool filtering = !string.IsNullOrEmpty(filter);
            string lastGroup = null;
            bool groupOpen = true;
            foreach (Cheat cheat in cheats)
            {
                if (filtering && !cheat.Matches(filter)) continue;
                if (cheat.Group != lastGroup)
                {
                    lastGroup = cheat.Group;
                    groupOpen = filtering || string.IsNullOrEmpty(cheat.Group) || Foldout(scope + "/" + cheat.Group, cheat.Group);
                }
                if (groupOpen) DrawCheat(cheat);
            }
        }

        private static void HotkeyButton(Cheat cheat)
        {
            CheatSystem system = CheatSystem.Instance;
            bool waiting = system != null && system.BindingTarget == cheat;
            string text = waiting
                ? CheatLayout.HotkeyWaiting
                : (cheat.Hotkey == UnityEngine.InputSystem.Key.None ? CheatLayout.HotkeyNone : CheatLayout.HotkeyOpen + cheat.Hotkey + CheatLayout.HotkeyClose);
            if (GUILayout.Button(new GUIContent(text, "Click, then press a key to bind it. Esc clears the binding."), GUILayout.Width(CheatLayout.HotkeyButtonWidth)) && system != null)
            {
                system.BindingTarget = waiting ? null : cheat;
            }
        }

        // ------------------------------------------------------------------ plain-object editor

        /// <summary>
        /// Draws an editable row for every public int, float, bool, string and enum field of an object (or only the named ones).
        /// Used for the rule files and the difficulty settings. Returns true when something changed.
        /// </summary>
        public static bool EditFields(object target, string scope, string[] only)
        {
            bool changed = false;
            foreach (FieldInfo field in target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (only != null && Array.IndexOf(only, field.Name) < 0) continue;
                changed |= EditField(target, field, scope + "." + field.Name);
            }
            return changed;
        }

        private static bool EditField(object target, FieldInfo field, string control)
        {
            Type type = field.FieldType;
            object current = field.GetValue(target);
            if (type == typeof(int))
            {
                int value = (int)current;
                GUILayout.BeginHorizontal();
                GUILayout.Label(field.Name, GUILayout.Width(CheatLayout.WideLabelWidth));
                int next = IntField(control, value, CheatLayout.FieldWidth);
                GUILayout.EndHorizontal();
                if (next == value) return false;
                field.SetValue(target, next);
                return true;
            }
            if (type == typeof(float))
            {
                float value = (float)current;
                GUILayout.BeginHorizontal();
                GUILayout.Label(field.Name, GUILayout.Width(CheatLayout.WideLabelWidth));
                float next = FloatField(control, value, CheatLayout.FieldWidth);
                GUILayout.EndHorizontal();
                if (Mathf.Approximately(next, value)) return false;
                field.SetValue(target, next);
                return true;
            }
            if (type == typeof(bool))
            {
                bool value = (bool)current;
                bool next = GUILayout.Toggle(value, field.Name);
                if (next == value) return false;
                field.SetValue(target, next);
                return true;
            }
            if (type == typeof(string))
            {
                string value = (string)current ?? "";
                GUILayout.Label(field.Name);
                string next = GUILayout.TextArea(value, GUILayout.MinHeight(CheatLayout.TextAreaHeight));
                if (next == value) return false;
                field.SetValue(target, next);
                return true;
            }
            if (type.IsEnum)
            {
                string[] names = Enum.GetNames(type);
                int value = Array.IndexOf(names, current.ToString());
                GUILayout.Label(field.Name);
                int next = GUILayout.SelectionGrid(value, names, Mathf.Max(1, Mathf.Min(names.Length, CheatLayout.ChoiceColumns)));
                if (next == value) return false;
                field.SetValue(target, Enum.Parse(type, names[next]));
                return true;
            }
            return false;
        }
    }
}
