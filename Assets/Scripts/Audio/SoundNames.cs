namespace MinefieldClerk.Sound
{
    /// <summary>Sound event names. Each one is a folder under Assets/Audio/Sfx or Assets/Audio/Amb.</summary>
    public static class SfxEvent
    {
        public const string MinePlate = "mine_plate";
        public const string MineWindup = "mine_windup";
        public const string MineExplosion = "mine_explosion";
        public const string DudFizzle = "dud_fizzle";
        public const string DefuseSnip = "defuse_snip";
        public const string CollectPickup = "collect_pickup";
        public const string FlagPlant = "flag_plant";
        public const string FlagWave = "flag_wave";
        public const string DigHole = "dig_hole";
        public const string PlayerHurt = "player_hurt";
        public const string PlayerDeath = "player_death";
        public const string StepDirt = "step_dirt";
        public const string BatteryLow = "battery_low";
        public const string BookOpen = "book_open";
        public const string PageTurn = "page_turn";
        public const string UiClick = "ui_click";
        public const string StampApproved = "stamp_approved";
        public const string DayEndBell = "day_end_bell";
        public const string AmbWindField = "amb_wind_field";
        public const string AmbLeaves = "amb_leaves";
        public const string AmbOfficeHum = "amb_office_hum";
    }

    /// <summary>Music track names: the file names (without extension) in Assets/Audio/Music.</summary>
    public static class MusicName
    {
        public const string Title = "titleMinistry";
        public const string Briefing = "briefing";
        public const string Field = "field";
        public const string Report = "report";
        public const string Expenses = "expenses";
    }
}
