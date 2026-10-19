using UnityEngine;

namespace MinefieldClerk.Day
{
    /// <summary>What the HUD shows this frame.</summary>
    public struct HudState
    {
        public int Day;
        public string Difficulty;
        public int Money;
        public bool CollectMode;
        public int Collected, Flags, Quota;
        public float Battery01;
        public bool BatteryEmpty;
        public bool Playing;
        public bool HasProbeLimit;
        public int Probes, ProbeCap;
        public bool HasKits;
        public int KitsLeft, KitsTotal;
        public string KeyHint, WorkHint;
    }

    /// <summary>
    /// The in-shift HUD drawn with the UI kit: three cream plates (day, money, quota), the detector battery bar,
    /// key hints top right, and a hotbar of slots with counters at the bottom. IMGUI, sized from the screen height.
    /// </summary>
    public class DayHud
    {
        private const float RefHeight = 1080f, MinScale = 0.6f;
        private const float Margin = 18f, GapY = 6f;
        private const float PlateW = 250f, PlateAspect = 368f / 86f, PlateBorderScale = 0.68f;
        private const float PlateIconH = 30f, PlateIconX = 40f, PlateTextX = 82f, PlateTextRight = 26f;
        private const float BarH = 40f, BarFrameArtH = 46f, BarFrameLeft = 16f, BarFrameRight = 21f, BarFrameTop = 12f, BarIconW = 32f, BarIconH = 38f;
        private const float HintH = 22f, HintGap = 4f;
        private const float SlotH = 66f, SlotGap = 40f, SlotBottom = 52f, CaptionH = 20f, CaptionGap = 2f;
        private const float CounterLeft = 0.70f, CounterTop = 0.66f, CounterRight = 0.96f, CounterBottom = 0.94f;   // the dark square on a slot
        // The battery fill is one solid bar whose colour slides green -> amber -> red as the charge drops.
        private const float BarFillMinCaps = 2f;   // narrowest fill, in end caps, so the rounded ends never overlap
        private const float BarFillCap = 7f;       // art pixels of each rounded end of bar_fill_solid
        private static readonly Color BarFull = new Color(0.45f, 0.78f, 0.2f);
        private static readonly Color BarMid = new Color(0.95f, 0.75f, 0.18f);
        private static readonly Color BarLow = new Color(0.86f, 0.2f, 0.12f);
        private const float FontPlate = 19f, FontHint = 15f, FontCaption = 14f, FontCounter = 14f, FontBar = 15f;
        private const int MinFont = 9;
        private const string BarFrame = "bar_battery_frame", BarFillSprite = "bar_fill_solid", PlateSprite = "panel_mid";
        // The kit extraction swapped two names: the file icon_coins holds the calendar art and icon_calendar the coins.
        private const string IconDay = "icon_coins", IconMoney = "icon_calendar", IconQuota = "icon_star", IconBattery = "icon_lightning";
        private const string SlotFlag = "slot_flag", SlotDetector = "slot_detector", SlotKit = "slot_defuse_kit";
        private const string DeadLabel = "DETECTOR DEAD";

        private static readonly Color Ink = new Color(0.14f, 0.10f, 0.08f);
        private static readonly Color Light = new Color(1f, 0.94f, 0.78f);
        private static readonly Color Shadow = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Color Warn = new Color(0.95f, 0.35f, 0.25f);
        private static readonly Vector2 ShadowOffset = new Vector2(1.5f, 1.5f);

        private GUIStyle plate, hint, caption, counter, bar;

        public void Draw(HudState s)
        {
            EnsureStyles();
            float k = Mathf.Max(MinScale, Screen.height / RefHeight);

            float x = Screen.width - (Margin + PlateW) * k, y = Margin * k;
            float plateH = PlateW / PlateAspect * k;
            DrawPlate(IconDay, "Day " + s.Day + " (" + s.Difficulty + ")", x, y, k, plateH);
            y += plateH + GapY * k;
            DrawPlate(IconMoney, "Money " + s.Money, x, y, k, plateH);
            y += plateH + GapY * k;
            DrawPlate(IconQuota, (s.CollectMode ? "Collected " + s.Collected : "Flags " + s.Flags) + " / " + s.Quota, x, y, k, plateH);
            y += plateH + GapY * k;
            DrawBattery(s, x, y, k);
            y += BarH * k + HintGap * k;

            if (s.Playing)
            {
                DrawHint(s.KeyHint, x, y, k);
                y += HintH * k;
                if (s.HasKits) DrawHint(s.WorkHint, x, y, k);
                DrawHotbar(s, k);
            }
        }

        private void DrawPlate(string icon, string text, float x, float y, float k, float height)
        {
            UiKit.NineSlice(PlateSprite, new Rect(x, y, PlateW * k, height), PlateBorderScale * k);
            float iconH = PlateIconH * k;
            UiKit.DrawFitHeight(icon, x + PlateIconX * k - iconH * UiKit.Aspect(icon) * Num.Half, y + (height - iconH) * Num.Half, iconH);
            plate.fontSize = Font(FontPlate, k);
            GUI.Label(new Rect(x + PlateTextX * k, y, (PlateW - PlateTextX - PlateTextRight) * k, height), text, plate);
        }

        private void DrawBattery(HudState s, float x, float y, float k)
        {
            float h = BarH * k;
            float iconH = BarIconH * k;
            UiKit.DrawFitHeight(IconBattery, x, y + (h - iconH) * Num.Half, iconH);

            var frame = new Rect(x + BarIconW * k, y, (PlateW - BarIconW) * k, h);
            float art = h / BarFrameArtH;   // screen pixels per art pixel of the frame
            UiKit.NineSlice(BarFrame, frame, art);
            var inner = new Rect(frame.x + BarFrameLeft * art, frame.y + BarFrameTop * art, frame.width - (BarFrameLeft + BarFrameRight) * art, frame.height - BarFrameTop * 2f * art);
            float charge = Mathf.Clamp01(s.Battery01);
            if (charge > 0f)
            {
                float minWidth = BarFillCap * BarFillMinCaps * art;
                inner.width = Mathf.Max(minWidth, inner.width * charge);
                GUI.color = BatteryColor(charge);
                UiKit.NineSlice(BarFillSprite, inner, art);
                GUI.color = Color.white;
            }

            if (s.BatteryEmpty)
            {
                bar.fontSize = Font(FontBar, k);
                ShadowLabel(frame, DeadLabel, bar, Warn);
            }
        }

        private static Color BatteryColor(float charge)
        {
            return charge >= Num.Half ? Color.Lerp(BarMid, BarFull, (charge - Num.Half) / Num.Half) : Color.Lerp(BarLow, BarMid, charge / Num.Half);
        }

        private void DrawHint(string text, float x, float y, float k)
        {
            hint.fontSize = Font(FontHint, k);
            ShadowLabel(new Rect(x, y, PlateW * k, HintH * k), text, hint, Light);
        }

        /// <summary>Flag, detector and defuse kit slots with a counter in the dark square and a caption underneath.</summary>
        private void DrawHotbar(HudState s, float k)
        {
            int slots = s.HasKits ? 3 : 2;
            float slotH = SlotH * k;
            float slotW = slotH * UiKit.Aspect(SlotFlag);
            float total = slots * slotW + (slots - 1) * SlotGap * k;
            float x = (Screen.width - total) * Num.Half, y = Screen.height - SlotBottom * k - slotH;

            DrawSlot(SlotFlag, s.Flags.ToString(), "Flags", Color.white, x, y, slotW, slotH, k);
            x += slotW + SlotGap * k;
            string probes = s.HasProbeLimit ? "Probes " + s.Probes + "/" + s.ProbeCap : "Detector";
            DrawSlot(SlotDetector, s.Probes.ToString(), probes, s.HasProbeLimit && s.Probes > s.ProbeCap ? Warn : Color.white, x, y, slotW, slotH, k);
            if (!s.HasKits) return;
            x += slotW + SlotGap * k;
            DrawSlot(SlotKit, s.KitsLeft.ToString(), "Kits " + s.KitsLeft + "/" + s.KitsTotal, s.KitsLeft == 0 ? Warn : Color.white, x, y, slotW, slotH, k);
        }

        private void DrawSlot(string sprite, string count, string label, Color tint, float x, float y, float w, float h, float k)
        {
            var rect = new Rect(x, y, w, h);
            UiKit.Draw(sprite, rect);

            counter.fontSize = Font(FontCounter, k);
            ShadowLabel(new Rect(x + w * CounterLeft, y + h * CounterTop, w * (CounterRight - CounterLeft), h * (CounterBottom - CounterTop)), count, counter, tint);

            caption.fontSize = Font(FontCaption, k);
            ShadowLabel(new Rect(x - w * Num.Half, y + h + CaptionGap * k, w * 2f, CaptionH * k), label, caption, tint);
        }

        private static void ShadowLabel(Rect rect, string text, GUIStyle style, Color color)
        {
            Color saved = style.normal.textColor;
            SetColor(style, Shadow);
            GUI.Label(new Rect(rect.x + ShadowOffset.x, rect.y + ShadowOffset.y, rect.width, rect.height), text, style);
            SetColor(style, color);
            GUI.Label(rect, text, style);
            SetColor(style, saved);
        }

        private static int Font(float units, float k)
        {
            return Mathf.Max(MinFont, Mathf.RoundToInt(units * k));
        }

        private void EnsureStyles()
        {
            if (plate != null) return;
            plate = MakeStyle(TextAnchor.MiddleLeft, Ink);
            hint = MakeStyle(TextAnchor.MiddleRight, Light);
            caption = MakeStyle(TextAnchor.UpperCenter, Light);
            counter = MakeStyle(TextAnchor.MiddleCenter, Color.white);
            bar = MakeStyle(TextAnchor.MiddleCenter, Warn);
        }

        private static GUIStyle MakeStyle(TextAnchor anchor, Color color)
        {
            var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = anchor, wordWrap = false };
            SetColor(style, color);
            return style;
        }

        /// <summary>Every state gets the same colour, otherwise IMGUI labels turn white on hover.</summary>
        private static void SetColor(GUIStyle style, Color color)
        {
            style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = color;
            style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = color;
        }
    }
}
