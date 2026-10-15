using UnityEngine;
using UnityEngine.InputSystem;
using MinefieldClerk.Field;

namespace MinefieldClerk.Player
{
    /// <summary>
    /// Free walking on the isometric field. WASD / arrow keys / gamepad stick move in screen directions;
    /// vertical movement is halved so speed looks even in the 2:1 projection.
    /// The logical position is a float; the sprite is drawn snapped to whole art pixels.
    /// Walls (the field edge) are handled per isometric cell axis so the player slides along them.
    /// The player also faces one of the 8 neighbouring cells (the probe / flag target).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // Neighbour cell offset for each 45 degree screen direction, starting at "right" and going counter-clockwise:
        // right, up-right, up, up-left, left, down-left, down, down-right.
        private static readonly Vector2Int[] FacingByOctant =
        {
            new Vector2Int(1, -1), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1),
            new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1),
        };

        [SerializeField] private FieldBuilder field;
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Horizontal screen speed in world units per second (32 units = 1 tile width)")]
        [SerializeField] private float speed = 2.5f;

        private Vector2 feet;
        private Vector2Int facing = new Vector2Int(1, 1);
        private Vector2? walkTarget;
        private bool mouseActive;
        private Camera cam;
        private Vector2 lastMouse;

        /// <summary>Isometric cell the player's feet are in.</summary>
        public Vector3Int CurrentCell { get; private set; }

        /// <summary>The neighbouring cell the player is facing: where probing and flagging act.</summary>
        /// <summary>Facing as a cell offset (each component -1..1).</summary>
        public Vector2Int Facing => facing;

        public Vector3Int FacingCell => new Vector3Int(CurrentCell.x + facing.x, CurrentCell.y + facing.y, 0);

        /// <summary>Cell under the mouse pointer.</summary>
        public Vector3Int HoverCell { get; private set; }

        /// <summary>True when a hovered cell exists (always, once the mouse is over the game): the pointer is clamped to cells within <see cref="ReachCells"/> of the player.</summary>
        public bool HoverValid { get; private set; }

        /// <summary>Where probing and flagging act: the hovered neighbour while the mouse is in use, else the facing cell.</summary>
        public Vector3Int TargetCell => (mouseActive && HoverValid) ? HoverCell : FacingCell;

        public const int ReachCells = 2;

        /// <summary>Turns to face a neighbouring cell.</summary>
        public void FaceCell(Vector3Int cell)
        {
            Vector2Int d = new Vector2Int(cell.x - CurrentCell.x, cell.y - CurrentCell.y);
            if (d != Vector2Int.zero)
            {
                facing = new Vector2Int(Mathf.Clamp(d.x, -1, 1), Mathf.Clamp(d.y, -1, 1));
                // screen-x of a cell offset (x - y) decides which way the sprite looks
                if (d.x != d.y && body != null) body.flipX = d.x < d.y;
            }
        }

        public float SpeedMultiplier { get; set; } = 1f;
        public bool Frozen { get; set; }

        private void Start()
        {
            Respawn();
        }

        public void Respawn()
        {
            feet = field.CellToWorld(field.StartCell);
            facing = new Vector2Int(1, 1);
            walkTarget = null;
            Apply();
        }

        private void Update()
        {
            UpdateMouse();
            if (!Frozen)
            {
                Vector2 input = ReadInput();
                if (input.sqrMagnitude > 0.0001f)
                {
                    walkTarget = null; // keyboard / stick overrides click-walk; the highlight keeps following the mouse
                }
                else if (walkTarget.HasValue)
                {
                    input = WalkInput();
                }

                if (input.sqrMagnitude > 0.0001f)
                {
                    float ratio = PixelConstants.CellHeight / PixelConstants.CellWidth;
                    Vector2 delta = new Vector2(input.x, input.y * ratio) * (speed * SpeedMultiplier * Time.deltaTime);
                    Vector2 before = feet;
                    Move(delta);
                    if (walkTarget.HasValue && (feet - before).sqrMagnitude < 1e-10f)
                    {
                        walkTarget = null; // stuck against the field edge
                    }

                    if (input.sqrMagnitude > 0.25f)
                    {
                        int octant = Mathf.RoundToInt(Mathf.Atan2(input.y, input.x) / (Mathf.PI / 4f));
                        facing = FacingByOctant[(octant + 8) % 8];
                    }

                    if (body != null && Mathf.Abs(input.x) > 0.1f)
                    {
                        body.flipX = input.x < 0f;
                    }
                }
            }
            Apply();
        }

        private void UpdateMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Vector2 screen = mouse.position.ReadValue();
            if ((screen - lastMouse).sqrMagnitude > 0.5f || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                mouseActive = true;
            }
            lastMouse = screen;

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            world.z = 0f;
            Vector3Int c = field.WorldToCell(world);
            bool pointerOnField = field.IsWalkable(new Vector3Int(c.x, c.y, 0));

            // The highlight is clamped to cells within ReachCells of the player (own cell included),
            // and kept inside the field, so it always shows the nearest reachable cell to the pointer.
            int cx = Mathf.Clamp(c.x, CurrentCell.x - ReachCells, CurrentCell.x + ReachCells);
            int cy = Mathf.Clamp(c.y, CurrentCell.y - ReachCells, CurrentCell.y + ReachCells);
            cx = Mathf.Clamp(cx, 0, field.Width - 1);
            cy = Mathf.Clamp(cy, 0, field.Height - 1);
            HoverCell = new Vector3Int(cx, cy, 0);
            HoverValid = field.IsWalkable(HoverCell);

            if (Frozen) return;
            // Left click (or hold) walks to that point.
            bool overUi = MinefieldClerk.DevTools.DevHud.IsOverUi(screen);
            if (!overUi && pointerOnField && (mouse.leftButton.wasPressedThisFrame || mouse.leftButton.isPressed))
            {
                walkTarget = new Vector2(world.x, world.y);
            }
        }

        private Vector2 WalkInput()
        {
            Vector2 d = walkTarget.Value - feet;
            float step = speed * SpeedMultiplier * Time.deltaTime;
            if (d.magnitude <= step)
            {
                if (CanStand(walkTarget.Value)) feet = walkTarget.Value;
                walkTarget = null;
                return Vector2.zero;
            }
            float ratio = PixelConstants.CellHeight / PixelConstants.CellWidth;
            return new Vector2(d.x, d.y / ratio).normalized;
        }

        private void Move(Vector2 delta)
        {
            if (CanStand(feet + delta))
            {
                feet += delta;
                return;
            }

            // Blocked: try each isometric axis on its own so the player slides along the wall.
            IsoMath.SplitAlongCellAxes(delta, out Vector2 alongX, out Vector2 alongY);
            if (CanStand(feet + alongX))
            {
                feet += alongX;
            }
            if (CanStand(feet + alongY))
            {
                feet += alongY;
            }
        }

        private bool CanStand(Vector2 position)
        {
            return field.IsWalkable(field.WorldToCell(new Vector3(position.x, position.y, 0f)));
        }

        private void Apply()
        {
            transform.position = IsoMath.SnapToPixel(new Vector3(feet.x, feet.y, 0f));
            Vector3Int cell = field.WorldToCell(new Vector3(feet.x, feet.y, 0f));
            CurrentCell = new Vector3Int(cell.x, cell.y, 0);
        }

        private static Vector2 ReadInput()
        {
            Vector2 v = Vector2.zero;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                v += pad.leftStick.ReadValue();
            }

            return Vector2.ClampMagnitude(v, 1f);
        }
    }
}
