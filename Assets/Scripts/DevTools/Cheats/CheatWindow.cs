using UnityEngine;
using UnityEngine.InputSystem;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>
    /// A draggable, resizable, collapsible IMGUI window full of cheats. A new window is a new subclass: list its cheats in
    /// <see cref="Build"/>, add anything custom in <see cref="DrawBody"/>, and add it to CheatSystem.CreateWindows.
    /// Position, size, open and collapsed state are remembered between play sessions.
    /// </summary>
    public abstract class CheatWindow
    {
        private const string XKey = ".x", YKey = ".y", WidthKey = ".w", HeightKey = ".h", OpenKey = ".open", CollapsedKey = ".collapsed";

        public abstract string Id { get; }
        public abstract string Title { get; }
        public virtual Key DefaultToggleKey => Key.None;
        public virtual Vector2 DefaultSize => new Vector2(CheatLayout.DefaultWindowWidth, CheatLayout.DefaultWindowHeight);

        public bool IsOpen { get; private set; }
        public bool Collapsed { get; private set; }
        public Rect Rect;
        public CheatList Cheats { get; private set; }

        internal int WindowId { get; private set; }

        private Vector2 scroll;
        private bool resizing;
        private bool dragged;
        private int index;

        /// <summary>The rectangle on screen right now (a collapsed window is only its title bar), in window units.</summary>
        public Rect ShownRect => Collapsed ? new Rect(Rect.x, Rect.y, Rect.width, CheatLayout.TitleHeight) : Rect;

        /// <summary>Lists this window's cheats.</summary>
        protected virtual void Build(CheatList list) { }

        /// <summary>Draws the window's content inside its scroll view. The default draws the cheat list.</summary>
        protected virtual void DrawBody() { DrawCheatList(); }

        /// <summary>Runs every frame while dev mode is on, also when the window is closed. Look up scene objects here, not while drawing.</summary>
        public virtual void OnUpdate() { }

        /// <summary>Draws over the whole game in screen pixels (not scaled), for example a selection outline.</summary>
        public virtual void DrawOverlay() { }

        /// <summary>The game is shutting down: undo anything that would outlive Play mode.</summary>
        public virtual void OnQuit() { }

        protected void DrawCheatList()
        {
            CheatGui.DrawCheats(Cheats.Items, Id, null);
        }

        internal void Init(int windowIndex)
        {
            index = windowIndex;
            WindowId = CheatLayout.WindowIdBase + windowIndex;
            Cheats = new CheatList(Id);
            Build(Cheats);
            foreach (Cheat cheat in Cheats.Items) cheat.LoadSaved();
            LoadLayout();
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
            CheatPrefs.SetBool(Id + OpenKey, open);
        }

        public void Toggle() { SetOpen(!IsOpen); }

        public void ResetLayout()
        {
            Vector2 size = DefaultSize;
            Rect = new Rect(CheatLayout.FirstWindowX + index * CheatLayout.CascadeStep, CheatLayout.FirstWindowY + index * CheatLayout.CascadeStep, size.x, size.y);
            Collapsed = false;
            SaveLayout();
        }

        private void LoadLayout()
        {
            ResetLayout();
            Rect = new Rect(
                CheatPrefs.GetFloat(Id + XKey, Rect.x),
                CheatPrefs.GetFloat(Id + YKey, Rect.y),
                CheatPrefs.GetFloat(Id + WidthKey, Rect.width),
                CheatPrefs.GetFloat(Id + HeightKey, Rect.height));
            IsOpen = CheatPrefs.GetBool(Id + OpenKey, false);
            Collapsed = CheatPrefs.GetBool(Id + CollapsedKey, false);
        }

        public void SaveLayout()
        {
            CheatPrefs.SetFloat(Id + XKey, Rect.x);
            CheatPrefs.SetFloat(Id + YKey, Rect.y);
            CheatPrefs.SetFloat(Id + WidthKey, Rect.width);
            CheatPrefs.SetFloat(Id + HeightKey, Rect.height);
            CheatPrefs.SetBool(Id + CollapsedKey, Collapsed);
        }

        /// <summary>Forgets every remembered value of this window's cheats and puts the layout back.</summary>
        public void ForgetSaved()
        {
            foreach (Cheat cheat in Cheats.Items)
            {
                cheat.ForgetSaved();
                cheat.ResetToDefault();
            }
            ResetLayout();
        }

        /// <summary>Called by the system when the mouse button is up, so a resize can never stay stuck.</summary>
        internal void EndDrag()
        {
            if (!resizing && !dragged) return;
            resizing = false;
            dragged = false;
            SaveLayout();
        }

        internal void Draw(float scale)
        {
            if (!IsOpen) return;
            ClampSize(scale);
            Rect shown = ShownRect;
            Rect result = GUI.Window(WindowId, shown, DrawContents, Title);
            if (!Mathf.Approximately(result.x, Rect.x) || !Mathf.Approximately(result.y, Rect.y)) dragged = true;
            Rect.x = result.x;
            Rect.y = result.y;
            ClampPosition(scale);
        }

        private void ClampSize(float scale)
        {
            float maxWidth = Screen.width / scale - CheatLayout.WindowPadding * 2f;
            float maxHeight = Screen.height / scale - CheatLayout.WindowPadding * 2f;
            Rect.width = Mathf.Clamp(Rect.width, CheatLayout.MinWindowWidth, Mathf.Max(CheatLayout.MinWindowWidth, maxWidth));
            Rect.height = Mathf.Clamp(Rect.height, CheatLayout.MinWindowHeight, Mathf.Max(CheatLayout.MinWindowHeight, maxHeight));
        }

        private void ClampPosition(float scale)
        {
            float screenWidth = Screen.width / scale;
            float screenHeight = Screen.height / scale;
            Rect.x = Mathf.Clamp(Rect.x, CheatLayout.MinVisibleWidth - Rect.width, screenWidth - CheatLayout.MinVisibleWidth);
            Rect.y = Mathf.Clamp(Rect.y, 0f, screenHeight - CheatLayout.TitleHeight);
        }

        private void DrawContents(int id)
        {
            Event e = Event.current;
            if (e.type == EventType.MouseDown) GUI.FocusWindow(id);

            if (!Collapsed)
            {
                scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
                DrawBody();
                GUILayout.EndScrollView();
                GUILayout.Space(CheatLayout.TooltipHeight);
                DrawTooltip();
                DrawResizeHandle(e);
            }

            DrawTitleButtons();
            GUI.DragWindow(new Rect(0f, 0f, Rect.width - 2f * (CheatLayout.TitleButtonSize + CheatLayout.TitleButtonGap), CheatLayout.TitleHeight));
        }

        private void DrawTooltip()
        {
            string tip = GUI.tooltip;
            if (string.IsNullOrEmpty(tip)) return;
            Rect line = new Rect(CheatLayout.WindowPadding, Rect.height - CheatLayout.TooltipHeight - CheatLayout.WindowPadding, Rect.width - CheatLayout.ResizeHandle - CheatLayout.WindowPadding * 2f, CheatLayout.TooltipHeight);
            GUI.Label(line, tip, CheatGui.Subtle);
        }

        private void DrawResizeHandle(Event e)
        {
            Rect handle = new Rect(Rect.width - CheatLayout.ResizeHandle, Rect.height - CheatLayout.ResizeHandle, CheatLayout.ResizeHandle, CheatLayout.ResizeHandle);
            GUI.Box(handle, CheatLayout.ResizeText);
            if (e.type == EventType.MouseDown && e.button == 0 && handle.Contains(e.mousePosition))
            {
                resizing = true;
                e.Use();
            }
            else if (resizing && e.type == EventType.MouseDrag)
            {
                Rect.width = Mathf.Max(CheatLayout.MinWindowWidth, Rect.width + e.delta.x);
                Rect.height = Mathf.Max(CheatLayout.MinWindowHeight, Rect.height + e.delta.y);
                e.Use();
            }
        }

        private void DrawTitleButtons()
        {
            float size = CheatLayout.TitleButtonSize;
            float closeX = Rect.width - size - CheatLayout.TitleButtonGap;
            float collapseX = closeX - size - CheatLayout.TitleButtonGap;
            if (GUI.Button(new Rect(collapseX, CheatLayout.TitleButtonTop, size, size), CheatLayout.CollapseText))
            {
                Collapsed = !Collapsed;
                SaveLayout();
            }
            if (GUI.Button(new Rect(closeX, CheatLayout.TitleButtonTop, size, size), CheatLayout.CloseText)) SetOpen(false);
        }

        /// <summary>Finds a scene object once and keeps it. Call from OnUpdate.</summary>
        protected static T Find<T>(ref T cache) where T : UnityEngine.Object
        {
            if (cache == null) cache = UnityEngine.Object.FindAnyObjectByType<T>();
            return cache;
        }
    }
}
