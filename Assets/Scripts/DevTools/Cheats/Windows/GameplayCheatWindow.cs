using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Field;
using MinefieldClerk.Player;

namespace MinefieldClerk.DevTools.Cheats
{
    /// <summary>The deminer, the mine field and the speed of the game.</summary>
    public sealed class GameplayCheatWindow : CheatWindow
    {
        private const int HealthyHits = 0, InjuredHits = 1, DeadHits = 2;
        private const string Hint = "Mines still go off with god mode on, they just never hurt. Field changes (fuse, dud chance) apply to mines armed from now on.";

        private PlayerController player;
        private PlayerHealth health;
        private FieldSession session;
        private MineField mines;
        private MineFieldView view;

        private int teleportX, teleportY;
        private string stats = "";
        private bool fuseCaptured;
        private float fuseMinDefault, fuseMaxDefault, dudDefault;   // what the scene's MineField started with

        public override string Id => "gameplay";
        public override string Title => "Player & Field";
        public override Key DefaultToggleKey => Key.F2;

        public override void OnUpdate()
        {
            Find(ref player);
            Find(ref health);
            Find(ref session);
            Find(ref mines);
            Find(ref view);
            if (mines != null && !fuseCaptured)
            {
                fuseCaptured = true;
                fuseMinDefault = mines.FuseMin;
                fuseMaxDefault = mines.FuseMax;
                dudDefault = mines.DudChance;
            }
            if (mines == null || health == null)
            {
                stats = "";
                return;
            }
            string state = health.Dead ? "dead" : (health.Injured ? "injured" : "healthy");
            stats = "Deminer " + state + "   Probed " + mines.ProbedCount + "   Flags " + mines.FlagCount + "   Burning " + mines.ArmedCount +
                "   Exploded " + mines.ExplodedCount + "   Duds " + mines.DudCount + "   Collected " + mines.CollectedCount + "   Mines left " + mines.MineCount;
        }

        protected override void Build(CheatList list)
        {
            Func<bool> hasHealth = () => health != null;
            Func<bool> hasPlayer = () => player != null;
            Func<bool> hasMines = () => mines != null;
            Func<bool> hasView = () => view != null;
            Func<bool> hasSession = () => session != null;

            list.Section("Deminer");
            list.Toggle("God mode", () => CheatState.GodMode, v => CheatState.GodMode = v, "Mine blasts never injure or kill the deminer").Saved();
            list.Button("Heal", () => health.CheatSetHits(HealthyHits), "Back to healthy and alive", hasHealth);
            list.Button("Injure", () => health.CheatSetHits(InjuredHits), "Slow and bandaged, as after the first hit", hasHealth);
            list.Button("Kill", () => health.CheatSetHits(DeadHits), "Dead: the shift ends after the usual delay", hasHealth);
            list.Float("Move speed x", () => CheatState.MoveSpeed, v => CheatState.MoveSpeed = v, CheatLimits.MinMoveSpeed, CheatLimits.MaxMoveSpeed, CheatLimits.SpeedStep, CheatState.NormalFactor).Saved();
            list.Float("Work speed x", () => CheatState.WorkSpeed, v => CheatState.WorkSpeed = v, CheatState.NormalFactor, CheatLimits.MaxWorkSpeed, CheatLimits.WorkStep, CheatState.NormalFactor, "Hold-E defusing and collecting go this many times faster").Saved();
            list.Int("Teleport column", () => teleportX, v => teleportX = v, 0, CheatLimits.MaxCellCoordinate);
            list.Int("Teleport row", () => teleportY, v => teleportY = v, 0, CheatLimits.MaxCellCoordinate);
            list.Button("Teleport to that cell", () => player.TeleportTo(new Vector3Int(teleportX, teleportY, 0)), "Moves to the nearest walkable cell if it is outside the field", hasPlayer);
            list.Button("Teleport to start", () => player.Respawn(), null, hasPlayer);

            list.Section("Field");
            list.Button("New field", () => session.NewDay(), "Same day and difficulty, a fresh layout", hasSession);
            list.Choice("Difficulty", Enum.GetNames(typeof(Difficulty)),
                () => session != null ? (int)session.Difficulty : 0,
                v => { if (session != null) session.SetDifficulty((Difficulty)v); }, 1, "Picking one makes a new field");
            list.Toggle("Show all mines (F1)", () => view != null && view.ShowingMines, v => { if (view != null && view.ShowingMines != v) view.ToggleMines(); });
            list.Toggle("Show all numbers (N)", () => view != null && view.ShowingAllNumbers, v => { if (view != null && view.ShowingAllNumbers != v) view.ToggleAllNumbers(); });
            list.Toggle("Show grid (G)", () => view != null && view.ShowingGrid, v => { if (view != null && view.ShowingGrid != v) view.ToggleGrid(); });
            list.Button("Flag every mine", FlagEveryMine, "Plants a correct flag on every mine still in the ground", hasMines);
            list.Button("Detonate burning mines", () => mines.DetonateArmed(), null, hasMines);

            list.Section("Mine fuse");
            list.Float("Fuse shortest (s)", () => mines != null ? mines.FuseMin : 0f, v => { if (mines != null) mines.FuseMin = v; }, 0f, CheatLimits.MaxFuseSeconds, CheatLimits.FuseStep, 0f, null, () => fuseMinDefault);
            list.Float("Fuse longest (s)", () => mines != null ? mines.FuseMax : 0f, v => { if (mines != null) mines.FuseMax = v; }, 0f, CheatLimits.MaxFuseSeconds, CheatLimits.FuseStep, 0f, null, () => fuseMaxDefault);
            list.Float("Dud chance", () => mines != null ? mines.DudChance : 0f, v => { if (mines != null) mines.DudChance = v; }, 0f, 1f, CheatLimits.DudStep, 0f, "Share of probed mines that never explode", () => dudDefault);

            list.Section("Game");
            list.Float("Game speed", () => CheatState.GameSpeed, CheatState.SetGameSpeed, 0f, CheatLimits.MaxGameSpeed, CheatLimits.GameSpeedStep, CheatState.NormalFactor, "0 freezes the game, 1 is normal");
        }

        protected override void DrawBody()
        {
            if (!string.IsNullOrEmpty(stats)) GUILayout.Label(stats, CheatGui.Wrap);
            GUILayout.Label(Hint, CheatGui.Subtle);
            DrawCheatList();
        }

        private void FlagEveryMine()
        {
            foreach (Vector3Int cell in new List<Vector3Int>(mines.LiveMineCells()))
            {
                if (mines.WorkAt(cell) == CellWork.NeedsFlag) mines.ToggleFlag(cell);
            }
        }
    }
}
