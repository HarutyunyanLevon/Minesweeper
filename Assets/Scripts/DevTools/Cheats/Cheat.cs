using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>
    /// One adjustable cheat: a button, a switch, a number or a choice. Windows only list cheats and draw them,
    /// so every cheat gets the same abilities: search, a rebindable hotkey, a reset to its default and (when marked Saved) memory between sessions.
    /// </summary>
    public abstract class Cheat
    {
        private const string HotkeyPrefix = "hk.";
        private const string ValuePrefix = "val.";

        public string Id { get; }
        public string Label { get; }
        public string Group { get; }
        public string Tip { get; }
        public Key DefaultHotkey { get; private set; }
        public Key Hotkey { get; private set; }
        /// <summary>The value is remembered between play sessions.</summary>
        public bool Persisted { get; private set; }

        protected string ValueKey => ValuePrefix + Id;

        protected Cheat(string id, string label, string group, string tip)
        {
            Id = id;
            Label = label;
            Group = group ?? "";
            Tip = tip ?? "";
        }

        /// <summary>Buttons and switches can be fired from a key.</summary>
        public virtual bool CanBindKey => false;

        /// <summary>Draws the control in the current layout row.</summary>
        public abstract void Draw();

        /// <summary>What the hotkey does (a button presses, a switch flips).</summary>
        public virtual void Trigger() { }

        /// <summary>Puts the cheat back to the value it starts with.</summary>
        public virtual void ResetToDefault() { }

        /// <summary>Applies the remembered value (called once at start for Saved cheats).</summary>
        protected virtual void Load() { }

        /// <summary>Gives the cheat a default hotkey (the player can rebind it in the window).</summary>
        public Cheat Hot(Key key)
        {
            DefaultHotkey = key;
            Hotkey = key;
            return this;
        }

        /// <summary>Remembers the cheat's value between play sessions.</summary>
        public Cheat Saved()
        {
            Persisted = true;
            return this;
        }

        internal void LoadSaved()
        {
            string hotkeyKey = HotkeyPrefix + Id;
            if (CheatPrefs.Has(hotkeyKey)) Hotkey = (Key)CheatPrefs.GetInt(hotkeyKey, (int)DefaultHotkey);
            if (Persisted) Load();
        }

        public void SetHotkey(Key key)
        {
            Hotkey = key;
            CheatPrefs.SetInt(HotkeyPrefix + Id, (int)key);
        }

        /// <summary>Forgets every remembered setting of this cheat.</summary>
        public void ForgetSaved()
        {
            CheatPrefs.Delete(HotkeyPrefix + Id);
            CheatPrefs.Delete(ValueKey);
            Hotkey = DefaultHotkey;
        }

        public bool Matches(string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            return Contains(Label, filter) || Contains(Group, filter) || Contains(Tip, filter);
        }

        private static bool Contains(string text, string filter)
        {
            return text.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected GUIContent Content()
        {
            return new GUIContent(Label, Tip);
        }
    }

    /// <summary>A button that runs an action. It can be greyed out while the action makes no sense.</summary>
    public sealed class ButtonCheat : Cheat
    {
        private readonly Action action;
        private readonly Func<bool> available;

        public ButtonCheat(string id, string label, string group, string tip, Action action, Func<bool> available)
            : base(id, label, group, tip)
        {
            this.action = action;
            this.available = available;
        }

        public override bool CanBindKey => true;

        public override void Trigger()
        {
            if (available == null || available()) action();
        }

        public override void Draw()
        {
            bool previous = GUI.enabled;
            GUI.enabled = previous && (available == null || available());
            if (GUILayout.Button(Content())) Trigger();
            GUI.enabled = previous;
        }
    }

    /// <summary>An on/off switch.</summary>
    public sealed class ToggleCheat : Cheat
    {
        private readonly Func<bool> get;
        private readonly Action<bool> set;
        private readonly bool defaultValue;

        public ToggleCheat(string id, string label, string group, string tip, Func<bool> get, Action<bool> set, bool defaultValue)
            : base(id, label, group, tip)
        {
            this.get = get;
            this.set = set;
            this.defaultValue = defaultValue;
        }

        public override bool CanBindKey => true;

        public override void Trigger() { Apply(!get()); }
        public override void ResetToDefault() { Apply(defaultValue); }
        protected override void Load() { set(CheatPrefs.GetBool(ValueKey, get())); }

        public override void Draw()
        {
            bool value = get();
            bool next = GUILayout.Toggle(value, Content());
            if (next != value) Apply(next);
        }

        private void Apply(bool value)
        {
            set(value);
            if (Persisted) CheatPrefs.SetBool(ValueKey, value);
        }
    }

    /// <summary>A whole number with a slider, a typed field, - / + buttons and a reset.</summary>
    public sealed class IntCheat : Cheat
    {
        private readonly Func<int> get;
        private readonly Action<int> set;
        private readonly int min, max, step, defaultValue;

        public IntCheat(string id, string label, string group, string tip, Func<int> get, Action<int> set, int min, int max, int step, int defaultValue)
            : base(id, label, group, tip)
        {
            this.get = get;
            this.set = set;
            this.min = min;
            this.max = max;
            this.step = step;
            this.defaultValue = defaultValue;
        }

        public override void ResetToDefault() { Apply(defaultValue); }
        protected override void Load() { Apply(CheatPrefs.GetInt(ValueKey, get())); }

        public override void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Content(), GUILayout.Width(CheatLayout.LabelWidth));
            int value = get();
            int next = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max));
            next = CheatGui.IntField(Id, next, CheatLayout.FieldWidth);
            if (GUILayout.Button(CheatLayout.StepDownText, GUILayout.Width(CheatLayout.StepButtonWidth))) next -= step;
            if (GUILayout.Button(CheatLayout.StepUpText, GUILayout.Width(CheatLayout.StepButtonWidth))) next += step;
            if (GUILayout.Button(CheatLayout.ResetText, GUILayout.Width(CheatLayout.StepButtonWidth))) next = defaultValue;
            GUILayout.EndHorizontal();
            next = Mathf.Clamp(next, min, max);
            if (next != value) Apply(next);
        }

        private void Apply(int value)
        {
            value = Mathf.Clamp(value, min, max);
            set(value);
            if (Persisted) CheatPrefs.SetInt(ValueKey, value);
        }
    }

    /// <summary>A decimal number with a slider, a typed field, - / + buttons and a reset.</summary>
    public sealed class FloatCheat : Cheat
    {
        private readonly Func<float> get;
        private readonly Action<float> set;
        private readonly float min, max, step, defaultValue;
        private readonly Func<float> defaultSource;

        public FloatCheat(string id, string label, string group, string tip, Func<float> get, Action<float> set, float min, float max, float step, float defaultValue, Func<float> defaultSource)
            : base(id, label, group, tip)
        {
            this.get = get;
            this.set = set;
            this.min = min;
            this.max = max;
            this.step = step;
            this.defaultValue = defaultValue;
            this.defaultSource = defaultSource;
        }

        /// <summary>The value the reset button restores: read live when the default belongs to a scene object, fixed otherwise.</summary>
        private float DefaultValue => defaultSource != null ? defaultSource() : defaultValue;

        public override void ResetToDefault() { Apply(DefaultValue); }
        protected override void Load() { Apply(CheatPrefs.GetFloat(ValueKey, get())); }

        public override void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Content(), GUILayout.Width(CheatLayout.LabelWidth));
            float value = get();
            float next = GUILayout.HorizontalSlider(value, min, max);
            next = CheatGui.FloatField(Id, next, CheatLayout.FieldWidth);
            if (GUILayout.Button(CheatLayout.StepDownText, GUILayout.Width(CheatLayout.StepButtonWidth))) next -= step;
            if (GUILayout.Button(CheatLayout.StepUpText, GUILayout.Width(CheatLayout.StepButtonWidth))) next += step;
            if (GUILayout.Button(CheatLayout.ResetText, GUILayout.Width(CheatLayout.StepButtonWidth))) next = DefaultValue;
            GUILayout.EndHorizontal();
            next = CheatGui.Round(Mathf.Clamp(next, min, max));
            if (!Mathf.Approximately(next, value)) Apply(next);
        }

        private void Apply(float value)
        {
            value = Mathf.Clamp(value, min, max);
            set(value);
            if (Persisted) CheatPrefs.SetFloat(ValueKey, value);
        }
    }

    /// <summary>One pick out of a few named options.</summary>
    public sealed class ChoiceCheat : Cheat
    {
        private readonly string[] names;
        private readonly Func<int> get;
        private readonly Action<int> set;
        private readonly int defaultValue;

        public ChoiceCheat(string id, string label, string group, string tip, string[] names, Func<int> get, Action<int> set, int defaultValue)
            : base(id, label, group, tip)
        {
            this.names = names;
            this.get = get;
            this.set = set;
            this.defaultValue = defaultValue;
        }

        public override void ResetToDefault() { Apply(defaultValue); }
        protected override void Load() { Apply(CheatPrefs.GetInt(ValueKey, get())); }

        public override void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Content(), GUILayout.Width(CheatLayout.LabelWidth));
            int value = get();
            int columns = Mathf.Max(1, Mathf.Min(names.Length, CheatLayout.ChoiceColumns));
            int next = GUILayout.SelectionGrid(value, names, columns);
            GUILayout.EndHorizontal();
            if (next != value) Apply(next);
        }

        private void Apply(int value)
        {
            set(value);
            if (Persisted) CheatPrefs.SetInt(ValueKey, value);
        }
    }

    /// <summary>Collects the cheats of one window while it is built. Every cheat id starts with the window id so ids never clash.</summary>
    public sealed class CheatList
    {
        private readonly string prefix;
        private string group = "";

        public readonly List<Cheat> Items = new List<Cheat>();

        public CheatList(string windowId)
        {
            prefix = windowId + ".";
        }

        /// <summary>The foldout that the next cheats belong to.</summary>
        public CheatList Section(string name)
        {
            group = name;
            return this;
        }

        public Cheat Button(string label, Action action, string tip = null, Func<bool> available = null)
        {
            return Add(new ButtonCheat(MakeId(label), label, group, tip, action, available));
        }

        public Cheat Toggle(string label, Func<bool> get, Action<bool> set, string tip = null, bool defaultValue = false)
        {
            return Add(new ToggleCheat(MakeId(label), label, group, tip, get, set, defaultValue));
        }

        public Cheat Int(string label, Func<int> get, Action<int> set, int min, int max, int step = 1, int defaultValue = 0, string tip = null)
        {
            return Add(new IntCheat(MakeId(label), label, group, tip, get, set, min, max, step, defaultValue));
        }

        public Cheat Float(string label, Func<float> get, Action<float> set, float min, float max, float step, float defaultValue, string tip = null, Func<float> defaultSource = null)
        {
            return Add(new FloatCheat(MakeId(label), label, group, tip, get, set, min, max, step, defaultValue, defaultSource));
        }

        public Cheat Choice(string label, string[] names, Func<int> get, Action<int> set, int defaultValue = 0, string tip = null)
        {
            return Add(new ChoiceCheat(MakeId(label), label, group, tip, names, get, set, defaultValue));
        }

        private Cheat Add(Cheat cheat)
        {
            Items.Add(cheat);
            return cheat;
        }

        private string MakeId(string label)
        {
            return prefix + label.ToLowerInvariant().Replace(' ', '-');
        }
    }
}
