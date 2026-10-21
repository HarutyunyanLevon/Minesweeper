using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MinefieldClerk.Day;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>
    /// Every in-game window (HUD, rulebook, report, family page, work bar...) on the spot: show or hide it, open it with real content,
    /// and adjust any element inside it live (position, size, scale, rotation, anchors, alpha, colour, text). The selected element is
    /// outlined on screen. "Copy values" puts the numbers on the clipboard so they can be typed into the prefab in Unity, because
    /// changes made during Play mode are lost when it ends.
    /// </summary>
    public sealed class UiCheatWindow : CheatWindow
    {
        private const int MaxTreeRows = 400;
        private const int RectCornerCount = 4;   // GetWorldCorners always returns four corners
        private const int MinReferenceSize = 200, MaxReferenceSize = 4000, ReferenceStep = 10;
        private const float PathPlaceholderWidth = 4f;
        private const string PathSeparator = "/";
        private const string Hint = "Pick Inspect on a window, then click any element in its list. Changes last until Play mode ends: use Copy values to keep them.";
        private const string NothingSelected = "Nothing selected.";
        private const string InactiveMark = "  (off)";
        private const string OpenMark = "-", ClosedMark = "+";

        private sealed class Snapshot
        {
            public Vector2 AnchorMin, AnchorMax, Pivot, AnchoredPosition, SizeDelta;
            public Vector3 Scale, Euler;
            public bool Active;
            public Color Color;
            public bool HasText;
            public string Text;
            public int FontSize;
        }

        private readonly List<Canvas> canvases = new List<Canvas>();
        private readonly Dictionary<RectTransform, Snapshot> originals = new Dictionary<RectTransform, Snapshot>();
        private readonly HashSet<RectTransform> expanded = new HashSet<RectTransform>();
        private readonly Vector3[] corners = new Vector3[RectCornerCount];

        private DayManager day;
        private RectTransform selected, treeRoot;
        private Vector2 treeScroll;
        private int rowsDrawn;

        public override string Id => "ui";
        public override string Title => "UI Windows";
        public override Key DefaultToggleKey => Key.F6;

        public override void OnUpdate()
        {
            day = DayManager.Instance;
            canvases.Clear();
            foreach (Canvas canvas in UiCanvas.Canvases()) canvases.Add(canvas);
        }

        protected override void Build(CheatList list)
        {
            Func<bool> has = () => day != null;
            list.Section("Open the real screens");
            list.Button("Rulebook, current day", () => day.CheatPreviewRules(day.Day), "The Book of Regulations with today's pages", has);
            list.Button("State report", () => day.CheatShowReport(), "Ends the shift and opens the report", has);
            list.Button("Family expenses page", () => day.CheatShowExpenses(), null, has);
            list.Button("Toast", () => day.Notify("Cheat toast: the Ministry is watching."), null, has);
        }

        protected override void DrawBody()
        {
            DrawCheatList();
            GUILayout.Label(Hint, CheatGui.Subtle);
            DrawCanvases();
            DrawWindowList();
            DrawTree();
            DrawInspector();
        }

        // ------------------------------------------------------------------ canvases and windows

        private void DrawCanvases()
        {
            CheatGui.Heading("Canvases");
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null) continue;
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) continue;
                if (!CheatGui.Foldout("ui/canvas/" + canvas.name, canvas.name + " (reference size)")) continue;
                Vector2 reference = scaler.referenceResolution;
                int width = CheatGui.IntRow("ui.ref.w." + canvas.name, "Width", Mathf.RoundToInt(reference.x), MinReferenceSize, MaxReferenceSize, ReferenceStep);
                int height = CheatGui.IntRow("ui.ref.h." + canvas.name, "Height", Mathf.RoundToInt(reference.y), MinReferenceSize, MaxReferenceSize, ReferenceStep);
                if (width != Mathf.RoundToInt(reference.x) || height != Mathf.RoundToInt(reference.y)) scaler.referenceResolution = new Vector2(width, height);
            }
        }

        private void DrawWindowList()
        {
            CheatGui.Heading("Windows");
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null) continue;
                GUILayout.Label(canvas.name, CheatGui.Subtle);
                Transform root = canvas.transform;
                for (int i = 0; i < root.childCount; i++)
                {
                    var window = root.GetChild(i) as RectTransform;
                    if (window == null) continue;
                    GUILayout.BeginHorizontal();
                    bool active = window.gameObject.activeSelf;
                    bool next = GUILayout.Toggle(active, window.name, GUILayout.Width(CheatLayout.WideLabelWidth));
                    if (next != active) window.gameObject.SetActive(next);
                    if (GUILayout.Button("Inspect"))
                    {
                        treeRoot = window;
                        Select(window);
                    }
                    if (GUILayout.Button("Reset")) ResetUnder(window);
                    GUILayout.EndHorizontal();
                }
            }
        }

        // ------------------------------------------------------------------ element tree

        private void DrawTree()
        {
            if (treeRoot == null) return;
            CheatGui.Heading("Elements of " + treeRoot.name);
            rowsDrawn = 0;
            treeScroll = GUILayout.BeginScrollView(treeScroll, GUILayout.Height(CheatLayout.TreeHeight));
            DrawNode(treeRoot, 0);
            GUILayout.EndScrollView();
        }

        private void DrawNode(RectTransform node, int depth)
        {
            if (rowsDrawn >= MaxTreeRows) return;
            rowsDrawn++;

            bool hasChildren = node.childCount > 0;
            bool open = expanded.Contains(node);

            GUILayout.BeginHorizontal();
            GUILayout.Space(depth * CheatLayout.IndentWidth);
            if (hasChildren)
            {
                if (GUILayout.Button(open ? OpenMark : ClosedMark, GUILayout.Width(CheatLayout.StepButtonWidth)))
                {
                    if (open) expanded.Remove(node);
                    else expanded.Add(node);
                }
            }
            else
            {
                GUILayout.Space(CheatLayout.StepButtonWidth + PathPlaceholderWidth);
            }
            string label = node.name + (node.gameObject.activeSelf ? "" : InactiveMark);
            if (GUILayout.Button(label, node == selected ? CheatGui.Selected : GUI.skin.button)) Select(node);
            GUILayout.EndHorizontal();

            if (!hasChildren || !open) return;
            for (int i = 0; i < node.childCount; i++)
            {
                var child = node.GetChild(i) as RectTransform;
                if (child != null) DrawNode(child, depth + 1);
            }
        }

        private void Select(RectTransform node)
        {
            selected = node;
            Capture(node);
        }

        // ------------------------------------------------------------------ inspector

        private void DrawInspector()
        {
            CheatGui.Heading("Selected element");
            if (selected == null)
            {
                GUILayout.Label(NothingSelected);
                return;
            }

            GUILayout.Label(PathOf(selected), CheatGui.Subtle);
            bool changed = false;

            bool active = selected.gameObject.activeSelf;
            bool nextActive = GUILayout.Toggle(active, "Active");
            if (nextActive != active) selected.gameObject.SetActive(nextActive);

            Vector2 position = selected.anchoredPosition;
            float px = CheatGui.FloatRow("ui.px", "Position X", position.x, -CheatLimits.PositionRange, CheatLimits.PositionRange, CheatLimits.PixelStep);
            float py = CheatGui.FloatRow("ui.py", "Position Y", position.y, -CheatLimits.PositionRange, CheatLimits.PositionRange, CheatLimits.PixelStep);
            if (!Mathf.Approximately(px, position.x) || !Mathf.Approximately(py, position.y))
            {
                selected.anchoredPosition = new Vector2(px, py);
                changed = true;
            }

            Vector2 size = selected.sizeDelta;
            float sw = CheatGui.FloatRow("ui.sw", "Size W", size.x, -CheatLimits.SizeRange, CheatLimits.SizeRange, CheatLimits.PixelStep);
            float sh = CheatGui.FloatRow("ui.sh", "Size H", size.y, -CheatLimits.SizeRange, CheatLimits.SizeRange, CheatLimits.PixelStep);
            if (!Mathf.Approximately(sw, size.x) || !Mathf.Approximately(sh, size.y))
            {
                selected.sizeDelta = new Vector2(sw, sh);
                changed = true;
            }

            Vector3 scale = selected.localScale;
            float scx = CheatGui.FloatRow("ui.scx", "Scale X", scale.x, 0f, CheatLimits.ScaleRange, CheatLimits.UnitStep);
            float scy = CheatGui.FloatRow("ui.scy", "Scale Y", scale.y, 0f, CheatLimits.ScaleRange, CheatLimits.UnitStep);
            if (!Mathf.Approximately(scx, scale.x) || !Mathf.Approximately(scy, scale.y))
            {
                selected.localScale = new Vector3(scx, scy, scale.z);
                changed = true;
            }

            Vector3 euler = selected.localEulerAngles;
            float angle = euler.z > CheatLimits.RotationRange ? euler.z - 2f * CheatLimits.RotationRange : euler.z;
            float nextAngle = CheatGui.FloatRow("ui.rot", "Rotation", angle, -CheatLimits.RotationRange, CheatLimits.RotationRange, CheatLimits.PixelStep);
            if (!Mathf.Approximately(nextAngle, angle))
            {
                euler.z = nextAngle;
                selected.localEulerAngles = euler;
                changed = true;
            }

            changed |= DrawAnchors();
            changed |= DrawGroupAlpha();
            changed |= DrawGraphic();

            if (changed)
            {
                LayoutRebuilder.MarkLayoutForRebuild(selected);
                if (selected.parent is RectTransform parent) LayoutRebuilder.MarkLayoutForRebuild(parent);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset element")) Restore(selected);
            if (GUILayout.Button("Select parent") && selected.parent is RectTransform up) Select(up);
            if (GUILayout.Button("Copy values")) GUIUtility.systemCopyBuffer = Describe(selected);
            GUILayout.EndHorizontal();
        }

        private bool DrawAnchors()
        {
            if (!CheatGui.Foldout("ui/anchors", "Anchors and pivot")) return false;
            Vector2 min = selected.anchorMin, max = selected.anchorMax, pivot = selected.pivot;
            float minX = CheatGui.FloatRow("ui.amx", "Anchor min X", min.x, 0f, 1f, CheatLimits.UnitStep);
            float minY = CheatGui.FloatRow("ui.amy", "Anchor min Y", min.y, 0f, 1f, CheatLimits.UnitStep);
            float maxX = CheatGui.FloatRow("ui.aMx", "Anchor max X", max.x, 0f, 1f, CheatLimits.UnitStep);
            float maxY = CheatGui.FloatRow("ui.aMy", "Anchor max Y", max.y, 0f, 1f, CheatLimits.UnitStep);
            float pivotX = CheatGui.FloatRow("ui.pvx", "Pivot X", pivot.x, 0f, 1f, CheatLimits.UnitStep);
            float pivotY = CheatGui.FloatRow("ui.pvy", "Pivot Y", pivot.y, 0f, 1f, CheatLimits.UnitStep);
            bool changed = !Mathf.Approximately(minX, min.x) || !Mathf.Approximately(minY, min.y) || !Mathf.Approximately(maxX, max.x) ||
                !Mathf.Approximately(maxY, max.y) || !Mathf.Approximately(pivotX, pivot.x) || !Mathf.Approximately(pivotY, pivot.y);
            if (!changed) return false;
            selected.anchorMin = new Vector2(minX, minY);
            selected.anchorMax = new Vector2(maxX, maxY);
            selected.pivot = new Vector2(pivotX, pivotY);
            return true;
        }

        private bool DrawGroupAlpha()
        {
            CanvasGroup group = selected.GetComponent<CanvasGroup>();
            float alpha = group != null ? group.alpha : 1f;
            float next = CheatGui.FloatRow("ui.alpha", "Fade (group alpha)", alpha, 0f, 1f, CheatLimits.UnitStep);
            if (Mathf.Approximately(next, alpha)) return false;
            if (group == null) group = selected.gameObject.AddComponent<CanvasGroup>();
            group.alpha = next;
            return true;
        }

        private bool DrawGraphic()
        {
            Graphic graphic = selected.GetComponent<Graphic>();
            if (graphic == null) return false;
            bool changed = false;

            Color c = graphic.color;
            float r = CheatGui.FloatRow("ui.cr", "Colour R", c.r, 0f, 1f, CheatLimits.UnitStep);
            float g = CheatGui.FloatRow("ui.cg", "Colour G", c.g, 0f, 1f, CheatLimits.UnitStep);
            float b = CheatGui.FloatRow("ui.cb", "Colour B", c.b, 0f, 1f, CheatLimits.UnitStep);
            float a = CheatGui.FloatRow("ui.ca", "Colour A", c.a, 0f, 1f, CheatLimits.UnitStep);
            if (!Mathf.Approximately(r, c.r) || !Mathf.Approximately(g, c.g) || !Mathf.Approximately(b, c.b) || !Mathf.Approximately(a, c.a))
            {
                graphic.color = new Color(r, g, b, a);
                changed = true;
            }

            bool enabled = graphic.enabled;
            bool nextEnabled = GUILayout.Toggle(enabled, "Graphic enabled");
            if (nextEnabled != enabled) graphic.enabled = nextEnabled;
            bool raycast = graphic.raycastTarget;
            bool nextRaycast = GUILayout.Toggle(raycast, "Blocks clicks (raycast target)");
            if (nextRaycast != raycast) graphic.raycastTarget = nextRaycast;

            if (graphic is Text label)
            {
                GUILayout.Label("Text");
                string next = GUILayout.TextArea(label.text ?? "", GUILayout.MinHeight(CheatLayout.TextAreaHeight));
                if (next != label.text)
                {
                    label.text = next;
                    changed = true;
                }
                int size = CheatGui.IntRow("ui.font", "Font size", label.fontSize, 1, CheatLimits.MaxFontSize, 1);
                if (size != label.fontSize)
                {
                    label.fontSize = size;
                    changed = true;
                }
            }
            return changed;
        }

        // ------------------------------------------------------------------ snapshots

        private void Capture(RectTransform node)
        {
            if (originals.ContainsKey(node)) return;
            var snapshot = new Snapshot
            {
                AnchorMin = node.anchorMin, AnchorMax = node.anchorMax, Pivot = node.pivot,
                AnchoredPosition = node.anchoredPosition, SizeDelta = node.sizeDelta,
                Scale = node.localScale, Euler = node.localEulerAngles, Active = node.gameObject.activeSelf,
            };
            Graphic graphic = node.GetComponent<Graphic>();
            if (graphic != null) snapshot.Color = graphic.color;
            if (graphic is Text label)
            {
                snapshot.HasText = true;
                snapshot.Text = label.text;
                snapshot.FontSize = label.fontSize;
            }
            originals[node] = snapshot;
        }

        private void Restore(RectTransform node)
        {
            if (node == null || !originals.TryGetValue(node, out Snapshot s)) return;
            node.anchorMin = s.AnchorMin;
            node.anchorMax = s.AnchorMax;
            node.pivot = s.Pivot;
            node.anchoredPosition = s.AnchoredPosition;
            node.sizeDelta = s.SizeDelta;
            node.localScale = s.Scale;
            node.localEulerAngles = s.Euler;
            node.gameObject.SetActive(s.Active);
            CanvasGroup group = node.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1f;
            Graphic graphic = node.GetComponent<Graphic>();
            if (graphic != null) graphic.color = s.Color;
            if (s.HasText && graphic is Text label)
            {
                label.text = s.Text;
                label.fontSize = s.FontSize;
            }
            LayoutRebuilder.MarkLayoutForRebuild(node);
        }

        private void ResetUnder(RectTransform root)
        {
            foreach (RectTransform node in new List<RectTransform>(originals.Keys))
            {
                if (node != null && node.IsChildOf(root)) Restore(node);
            }
        }

        // ------------------------------------------------------------------ text and outline

        private static string PathOf(Transform node)
        {
            var parts = new List<string>();
            for (Transform t = node; t != null; t = t.parent) parts.Insert(0, t.name);
            return string.Join(PathSeparator, parts);
        }

        private static string Describe(RectTransform node)
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            var text = new StringBuilder();
            text.AppendLine(PathOf(node));
            text.AppendLine("anchorMin " + node.anchorMin.ToString("0.###", ci) + "   anchorMax " + node.anchorMax.ToString("0.###", ci) + "   pivot " + node.pivot.ToString("0.###", ci));
            text.AppendLine("anchoredPosition " + node.anchoredPosition.ToString("0.###", ci) + "   sizeDelta " + node.sizeDelta.ToString("0.###", ci));
            text.AppendLine("localScale " + node.localScale.ToString("0.###", ci) + "   rotationZ " + node.localEulerAngles.z.ToString("0.###", ci));
            Graphic graphic = node.GetComponent<Graphic>();
            if (graphic != null) text.AppendLine("color " + graphic.color.ToString("0.###", ci));
            if (graphic is Text label) text.AppendLine("fontSize " + label.fontSize + "   text \"" + label.text + "\"");
            return text.ToString();
        }

        public override void DrawOverlay()
        {
            if (selected == null || Event.current.type != EventType.Repaint) return;
            selected.GetWorldCorners(corners);
            float left = float.MaxValue, right = float.MinValue, top = float.MaxValue, bottom = float.MinValue;
            foreach (Vector3 corner in corners)
            {
                float y = Screen.height - corner.y;
                left = Mathf.Min(left, corner.x);
                right = Mathf.Max(right, corner.x);
                top = Mathf.Min(top, y);
                bottom = Mathf.Max(bottom, y);
            }

            float t = CheatLayout.OutlineThickness;
            Color previous = GUI.color;
            GUI.color = CheatLayout.OutlineColor;
            Texture2D white = Texture2D.whiteTexture;
            GUI.DrawTexture(new Rect(left, top, right - left, t), white);
            GUI.DrawTexture(new Rect(left, bottom - t, right - left, t), white);
            GUI.DrawTexture(new Rect(left, top, t, bottom - top), white);
            GUI.DrawTexture(new Rect(right - t, top, t, bottom - top), white);
            GUI.color = previous;
        }
    }
}
