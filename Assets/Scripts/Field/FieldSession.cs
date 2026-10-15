using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Player;

namespace MinefieldClerk.Field
{
    /// <summary>
    /// Runs one field "day" for now: builds the ground, lays the mines, resets the player.
    /// R starts a new field, F1 shows or hides the mines (dev). In M3 this grows into the real day loop.
    /// </summary>
    public class FieldSession : MonoBehaviour
    {
        [SerializeField] private FieldBuilder field;
        [SerializeField] private MineField mines;
        [SerializeField] private MineFieldView view;
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerHealth health;

        private void Start()
        {
            NewDay();
        }

        public void NewDay()
        {
            field.Build();
            mines.Generate();   // also clears the view
            health.ResetDay();
            player.Respawn();
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.rKey.wasPressedThisFrame) NewDay();
            if (kb.f1Key.wasPressedThisFrame) view.ToggleMines();
        }
    }
}
