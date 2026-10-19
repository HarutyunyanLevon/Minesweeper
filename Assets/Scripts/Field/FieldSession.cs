using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Player;

namespace MinefieldClerk.Field
{
    /// <summary>
    /// Runs one field "day" for now: builds the ground, lays the mines, resets the player.
    /// R starts a new field, F1 shows or hides the mines (dev). In M3 this grows into the real day loop.
    /// The difficulty decides the field size (picked from the preset's sizes), the mine count and the
    /// money / radar / encounter modifiers; changing it regenerates the field.
    /// </summary>
    public class FieldSession : MonoBehaviour
    {
        [SerializeField] private FieldBuilder field;
        [SerializeField] private MineField mines;
        [SerializeField] private MineFieldView view;
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private Difficulty difficulty = Difficulty.Medium;

        private readonly System.Random sizeRng = new System.Random();

        public Difficulty Difficulty => difficulty;
        public DifficultySettings Settings => DifficultyTable.Get(difficulty);

        private void Start()
        {
            NewDay();
        }

        /// <summary>Picks the difficulty (at game start, or the dev buttons) and regenerates the field.</summary>
        public void SetDifficulty(Difficulty value)
        {
            difficulty = value;
            NewDay();
        }

        public void NewDay()
        {
            DifficultySettings settings = Settings;
            FieldSize size = settings.PickSize(sizeRng);
            field.Resize(size.width, size.height);

            field.Build();
            mines.Generate(settings.mines);   // also clears the view
            health.ResetDay();
            player.Respawn();
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (!DevTools.DevMode.Enabled) return;
            if (kb.rKey.wasPressedThisFrame) NewDay();
            if (kb.f1Key.wasPressedThisFrame) view.ToggleMines();
        }
    }
}
