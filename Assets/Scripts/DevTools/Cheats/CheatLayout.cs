using UnityEngine;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>Sizes, fonts and colours of the cheat windows, in IMGUI pixels at UI scale 1. Nothing about the look is hard-coded elsewhere.</summary>
    public static class CheatLayout
    {
        public const int FontSize = 14;
        public const int GuiDepth = -1000;              // lower draws on top of DevHud and everything else
        public const int WindowIdBase = 7100;

        // ---- windows
        public const float TitleHeight = 24f;
        public const float TitleButtonSize = 20f;
        public const float TitleButtonGap = 2f;
        public const float TitleButtonTop = 2f;
        public const float WindowPadding = 6f;
        public const float ResizeHandle = 18f;
        public const float TooltipHeight = 18f;
        public const float MinWindowWidth = 280f;
        public const float MinWindowHeight = 120f;
        public const float MinVisibleWidth = 80f;       // a window can be dragged off screen but this much always stays reachable
        public const float DefaultWindowWidth = 460f;
        public const float DefaultWindowHeight = 600f;
        public const float FirstWindowX = 24f;
        public const float FirstWindowY = 70f;
        public const float CascadeStep = 34f;
        public const string CloseText = "x";
        public const string CollapseText = "_";
        public const string ResizeText = "::";

        // ---- rows
        public const float LabelWidth = 150f;
        public const float WideLabelWidth = 210f;
        public const float FieldWidth = 64f;
        public const float StepButtonWidth = 24f;
        public const float HotkeyButtonWidth = 64f;
        public const float IndentWidth = 14f;
        public const float SectionGap = 6f;
        public const float TreeHeight = 220f;
        public const float TextAreaHeight = 80f;
        public const int ChoiceColumns = 3;
        public const string StepDownText = "-", StepUpText = "+", ResetText = "R";
        public const string HotkeyNone = "[  ]", HotkeyWaiting = "press key", HotkeyOpen = "[", HotkeyClose = "]";

        // ---- toolbar (screen pixels, not scaled)
        public const float ToolbarHeight = 30f;
        public const float ToolbarLeft = 140f;          // right of the DEV button
        public const float ToolbarBottom = 10f;
        public const float ToolbarButtonWidth = 118f;
        public const float ToolbarGap = 4f;

        // ---- whole system
        public const float DefaultUiScale = 1.2f;
        public const float MinUiScale = 0.6f;
        public const float MaxUiScale = 2.5f;
        public const float DefaultOpacity = 0.96f;
        public const float MinOpacity = 0.3f;

        // ---- selection outline drawn over the game by the UI window
        public const float OutlineThickness = 2f;
        public static readonly Color OutlineColor = new Color(1f, 0.25f, 0.85f, 1f);
        public static readonly Color SubtleTextColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        public static readonly Color SelectedRowColor = new Color(1f, 0.85f, 0.3f, 1f);
    }

    /// <summary>The ranges the cheat sliders offer. They limit the tools only, never the game.</summary>
    public static class CheatLimits
    {
        public const int CampaignDays = 30;
        public const int MaxMoney = 99999;
        public const int MoneySmall = 10, MoneyMedium = 100, MoneyLarge = 1000;
        public const float MaxBatterySeconds = 600f;
        public const float BatteryStep = 10f;
        public const int MaxProbes = 999;
        public const int MaxKits = 99;
        public const float MinMoveSpeed = 0.1f, MaxMoveSpeed = 8f, SpeedStep = 0.25f;
        public const float MaxWorkSpeed = 50f, WorkStep = 1f;
        public const float MaxGameSpeed = 5f, GameSpeedStep = 0.25f;
        public const float MaxFuseSeconds = 10f, FuseStep = 0.25f;
        public const float DudStep = 0.05f;
        public const int MaxCellCoordinate = 99;
        public const float FloatPrecision = 1000f;       // floats are rounded to 1/1000 so slider noise never shows
        public const float UnitStep = 0.05f;
        public const float PitchMin = 0.25f, PitchMax = 2f;
        public const float MaxFadeSeconds = 10f;
        public const float MaxLeavesGapSeconds = 120f;
        public const float PositionRange = 1500f;
        public const float SizeRange = 2000f;
        public const float ScaleRange = 5f;
        public const float RotationRange = 180f;
        public const float PixelStep = 1f;
        public const int MaxFontSize = 120;
        public const int FieldSizeMax = 60;
        public const int RuleNumberMax = 9999;
        public const float RuleFloatMax = 100f;
        public const float MultiplierMax = 10f;
        public const int SearchWidth = 220;
    }
}
