using System.Collections.Generic;
using UnityEngine;
using MinefieldClerk.Ambience;

namespace MinefieldClerk.Field
{
    /// <summary>
    /// Scenery around the minefield plot: a broken post-and-rail fence along the outer edge of the dirt border, the ruined
    /// farmhouse, barn and farmyard, the wrecked tractor with its sandbags, a dog asleep on a leash, wheat that was never
    /// harvested, signs, poles, junk and scorched ground. The farmhouse yard is top left, the tank top right.
    /// Rebuilt whenever a new field is generated. Positions are in cell-edge coordinates (u along cell x, v along cell y).
    /// </summary>
    public class FieldDecor : MonoBehaviour
    {
        [SerializeField] private FieldBuilder field;
        [SerializeField] private MineField mines;
        [SerializeField, HideInInspector] private Sprite fenceX;
        [SerializeField, HideInInspector] private Sprite fenceXBroken;
        [SerializeField, HideInInspector] private Sprite fenceY;
        [SerializeField, HideInInspector] private Sprite fenceYBroken;
        [SerializeField, HideInInspector] private Sprite farmhouse;
        [SerializeField, HideInInspector] private Sprite tank;
        [SerializeField, HideInInspector] private Sprite[] scatterTrees;
        [SerializeField, HideInInspector] private Sprite[] scatterBushes;
        [SerializeField, HideInInspector] private Sprite[] scatterTufts;
        [SerializeField, HideInInspector] private Sprite[] scatterRubble;
        // ruin set (ConfigureRuin)
        [SerializeField, HideInInspector] private Sprite[] damagedX;
        [SerializeField, HideInInspector] private Sprite[] damagedY;
        [SerializeField, HideInInspector] private Sprite[] clutter;      // barrels, crates, sandbags, scrap, tank trap, barbed wire
        [SerializeField, HideInInspector] private Sprite[] wheat;        // tall a, tall b, small a, small b
        [SerializeField, HideInInspector] private Sprite[] yardProps;    // haystack, cart
        [SerializeField, HideInInspector] private Sprite barn;
        [SerializeField, HideInInspector] private Sprite well;
        [SerializeField, HideInInspector] private Sprite kennel;
        [SerializeField, HideInInspector] private Sprite dog;
        [SerializeField, HideInInspector] private Sprite mineSign;
        [SerializeField, HideInInspector] private Sprite pole;
        [Tooltip("Baked: the props live in the scene as plain objects you can move. Only the fence is still rebuilt for the field size. Use Minefield > Decor > Bake / Regenerate.")]
        [SerializeField] private bool baked;
        [SerializeField] private int seed = 7;
        [SerializeField, Range(0f, 1f)] private float brokenChance = 0.22f;

        // ---- fence
        private const float DamagedChance = 0.24f;   // share of segments flattened, leaning or wired
        private const float MissingChance = 0.05f;   // share missing altogether
        private const float FenceBaseOffsetPx = 6f;
        private const float FenceMargin = 1f;        // fence stands this many cells outside the plot
        private const int EntranceHalfWidth = 1;     // missing segments either side of the entrance centre

        // ---- seeds (added to the serialized seed so each system has its own random sequence)
        private const int SeedScatterOffset = 1;
        private const int SeedWheatOffset = 2;
        private const int SeedDecalOffset = 3;
        private const int DecalSpriteSeed = 99;
        private const int RoadsideSkipPerCell = 4;   // random draws skipped so the roadside sequence stays stable

        // ---- cell-edge geometry
        private const float StartCellCentre = 0.5f;  // start cell centre offset in u and v
        private const float PlotClearMargin = 3f;    // keep this many cells beyond the plot free of decals and meadow props
        private const float LaneHalfWidth = 3.5f;    // entrance lane kept clear of meadow props

        // ---- tank tracks
        private const int TrackW = 44, TrackH = 26;
        private const int TrackOffsetX = 12, TrackOffsetY = 6;   // pixels either side of the lane centre
        private const int TrackHalfLength = 8;                    // pixels from centre to piece end
        private const int TrackSlope = 2;                         // 2:1 iso slope
        private const int TrackThickness = 2;
        private const int TrackTickMask = 3;                      // every 4th pixel is a tread tick
        private const int TrackTickOffset = TrackHalfLength * 2;  // keeps the tick index positive
        private const float TrackStartGap = 1.8f;                 // cells behind the tank where the tread starts
        private const float TrackStep = 1f;
        private const float TrackEndPastField = 3.4f, TrackEndPastTank = 3f;
        private const float TrackRunOnPastField = 10f;
        private const float TrackClaimRadius = 1.3f;
        private static readonly Color32 TrackColor = new Color32(52, 38, 26, 235);
        private static readonly Color32 TrackTick = new Color32(34, 24, 16, 245);
        private static readonly Color32 TrackLip = new Color32(96, 74, 48, 150);
        private const int AxisU = 0, AxisV = 1;

        // ---- roadside
        private const int PoleCount = 4;
        private const float PoleU = -9f, PoleUJitter = 0.6f, PoleSpanPad = 6f, PoleVStart = 3f, PoleRadius = 1.5f;
        private const int WireAlongU = 5, WireAlongV = 3;
        private const float WireMargin = 2f;                       // keeps wire away from the corners
        private const float WireBelowV = 3f, WireBelowJitter = 1.5f;
        private const float WireSideU = 3f;                        // past the far edge
        private const float JunkU = 3.5f;                          // before the near edge
        private const float WireClaimCheck = 1.2f, WireRadius = 1.4f;
        private const int JunkKinds = 4;                           // clutter[0..3] are junk

        // ---- wheat
        private static readonly WheatSide[] WheatSides =
        {
            new WheatSide(20, 0f, -8f, 4.5f, 1f),     // left of the plot
            new WheatSide(14, 1f, 5.5f, 4f, 5f),      // right of the plot
        };
        private const float WheatV = 0.5f, WheatRadius = 0.9f, WheatSway = 1.1f;
        private const double TallWheatChance = 0.6;
        private const int TallWheatKinds = 2;

        // ---- decals
        private static readonly int[] DecalSizes = { 16, 24, 32 };
        private const int DecalCount = 46;
        private const int RingDecalCount = 12;
        private const int PlacementTries = 40;
        private const float DecalMargin = 11f, DecalSpan = 22f;
        private const float RingOffset = 0.5f;

        // ---- meadow scatter
        private const int ScatterCount = 150;
        private const float ScatterMargin = 12f, ScatterSpan = 22f;
        private const float ScatterClaim = 0.8f;
        private const float ScatterDropPx = 2f;
        private const double ChanceTufts = 0.30, ChanceBushes = 0.18, ChanceTrees = 0.10, ChanceRubble = 0.12, ChanceWheat = 0.10;
        private const int SmallWheatFirst = 2;   // wheat[2], wheat[3] are the small ones

        private readonly struct WheatSide
        {
            public readonly int count;
            public readonly float widthShare, uOffset, uRange, vTrim;
            public WheatSide(int count, float widthShare, float uOffset, float uRange, float vTrim)
            {
                this.count = count; this.widthShare = widthShare; this.uOffset = uOffset; this.uRange = uRange; this.vTrim = vTrim;
            }
        }

        private string group = "Misc";
        private const string FencesGroup = "Fences", TracksGroup = "Tank Tracks", DecalGroup = "Ground Decals";
        private const string MeadowGroup = "Meadow", WheatGroup = "Wheat Fields", TankPath = "Military/Tank";
        public bool Baked { get => baked; set => baked = value; }
        private readonly List<Vector3> claimed = new List<Vector3>();   // x = u, y = v, z = radius
        private Sprite[] decals;

        public void Configure(FieldBuilder fieldBuilder, MineField mineField, Sprite fx, Sprite fxb, Sprite fy, Sprite fyb, Sprite house, Sprite tankSprite)
        {
            field = fieldBuilder;
            mines = mineField;
            fenceX = fx; fenceXBroken = fxb; fenceY = fy; fenceYBroken = fyb;
            farmhouse = house; tank = tankSprite;
        }

        public void ConfigureScatter(Sprite[] trees, Sprite[] bushes, Sprite[] tufts, Sprite[] rubble)
        {
            scatterTrees = trees; scatterBushes = bushes; scatterTufts = tufts; scatterRubble = rubble;
        }

        public void ConfigureRuin(Sprite[] dmgX, Sprite[] dmgY, Sprite[] clutterSprites, Sprite[] wheatSprites, Sprite[] yard,
                                  Sprite barnSprite, Sprite wellSprite, Sprite kennelSprite, Sprite dogSprite, Sprite signSprite, Sprite poleSprite)
        {
            damagedX = dmgX; damagedY = dmgY; clutter = clutterSprites; wheat = wheatSprites; yardProps = yard;
            barn = barnSprite; well = wellSprite; kennel = kennelSprite; dog = dogSprite; mineSign = signSprite; pole = poleSprite;
        }

        private void OnEnable()
        {
            basePos.Clear();
            if (mines != null) mines.Regenerated += OnRegenerated;
        }

        private void OnDisable()
        {
            if (mines != null) mines.Regenerated -= OnRegenerated;
        }

        /// <summary>World position of a point given in cell-edge coordinates (u along cell x, v along cell y).</summary>
        private static Vector3 P(float u, float v)
        {
            return IsoMath.EdgeToWorld(u, v);
        }

        private void OnRegenerated()
        {
            if (baked) { Reposition(); RebuildAuto(); } else Build();
        }

        private static void Kill(GameObject go)
        {
            // Destroy() is deferred; detach first so a group of the same name is not found again before it is gone.
            if (Application.isPlaying) { go.transform.SetParent(null, false); Destroy(go); } else DestroyImmediate(go);
        }

        private Transform GroupRoot(string name)
        {
            Transform t = transform.Find(name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(transform, false);
            }
            return t;
        }

        /// <summary>Rebuilds the parts that depend on the field size: fence and tank tracks.</summary>
        public void RebuildAuto()
        {
            BuildFences();
            BuildTracks();
        }

        private readonly Dictionary<Transform, Vector3> basePos = new Dictionary<Transform, Vector3>();

        /// <summary>
        /// Adapts the hand-placed props to the current field size. Positions in the scene are the Medium layout. A prop keeps its
        /// distance to the field edges: its u and v shift by how much the field grew or shrank, weighted by how far along the
        /// field it stands (left/near edge = 0, right/far edge = 1). The house behind the far fence stays behind the far fence,
        /// the tank at the right corner stays at the right corner, and props along an edge spread out with it.
        /// </summary>
        public void Reposition()
        {
            int w = field.Width, h = field.Height;
            foreach (Transform grp in transform)
            {
                if (grp.name == FencesGroup || grp.name == TracksGroup) continue;
                foreach (Transform t in grp)
                {
                    if (!basePos.TryGetValue(t, out Vector3 b))
                    {
                        b = t.position;
                        basePos[t] = b;
                    }
                    Vector2 edge = IsoMath.WorldToEdge(b);
                    float du = (w - FieldBuilder.AnchorWidth) * Mathf.Clamp01(edge.x / FieldBuilder.AnchorWidth);
                    float dv = (h - FieldBuilder.AnchorHeight) * Mathf.Clamp01(edge.y / FieldBuilder.AnchorHeight);
                    t.position = IsoMath.SnapToPixel(b + IsoMath.EdgeToWorld(du, dv));
                }
            }
        }

        /// <summary>Tracks start at wherever the Tank prop stands and run to the right side of the current field.</summary>
        private void BuildTracks()
        {
            Transform old = transform.Find(TracksGroup);
            if (old != null) Kill(old.gameObject);
            Transform tank = transform.Find(TankPath);
            if (tank == null) return;
            Vector3 q = tank.position - field.Origin;
            Tracks(field.Width, field.Height, IsoMath.WorldToEdge(q));
            Transform g = transform.Find(TracksGroup);
            if (g != null) g.position = field.Origin;
        }

        /// <summary>Rebuilds only the fence, which has to match the field size. Props stay where they are in the scene.</summary>
        public void BuildFences()
        {
            Transform old = transform.Find(FencesGroup);
            if (old != null) Kill(old.gameObject);
            group = FencesGroup;
            int w = field.Width, h = field.Height;
            var rng = new System.Random(seed);
            int gap = w / 2;
            int first = -(int)FenceMargin;
            for (int i = first; i <= w; i++)
            {
                if (i >= gap - EntranceHalfWidth && i <= gap + EntranceHalfWidth) continue; // entrance on the near side
                Segment(true, i + Num.Half, -FenceMargin, rng, i == first || i == w);
            }
            for (int i = first; i <= w; i++) Segment(true, i + Num.Half, h + FenceMargin, rng, i == first || i == w);
            for (int j = first; j <= h; j++) Segment(false, -FenceMargin, j + Num.Half, rng, j == first || j == h);
            for (int j = first; j <= h; j++) Segment(false, w + FenceMargin, j + Num.Half, rng, j == first || j == h);
            Transform g = transform.Find(FencesGroup);
            if (g != null) g.position = field.Origin;
        }

        /// <summary>Generates the whole scenery (props, decals, tracks, fence) as children grouped by category.</summary>
        public void Build()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Kill(transform.GetChild(i).gameObject);
            }
            claimed.Clear();

            int w = field.Width, h = field.Height;
            var rng = new System.Random(seed);
            BuildFences();
            for (int n = 0; n < RoadsideSkipPerCell * (w + h); n++) rng.NextDouble();   // keep the roadside random sequence stable

            PlaceCluster(w);
            BuildTracks();
            Roadside(w, h, rng);
            WheatFields(w, h, new System.Random(seed + SeedWheatOffset));

            BuildDecals(w, h, new System.Random(seed + SeedDecalOffset));
            Scatter(w, h, new System.Random(seed + SeedScatterOffset));
        }

        // ---------------------------------------------------------------- hand-placed clusters

        /// <summary>Cell-edge position (u,v) of a point given as a screen offset in world units from the start cell.</summary>
        private static Vector2 S(int w, float dx, float dy)
        {
            return new Vector2(w * Num.Half + StartCellCentre + dx + IsoMath.Slope * dy, StartCellCentre + IsoMath.Slope * dy - dx);
        }

        /// <summary>The sprite for a slot, or null when the sprites for it are not wired in.</summary>
        private Sprite SpriteFor(PropSlot slot)
        {
            switch (slot)
            {
                case PropSlot.Farmhouse: return farmhouse;
                case PropSlot.Barn: return barn;
                case PropSlot.Tank: return tank;
                case PropSlot.Kennel: return kennel;
                case PropSlot.Dog: return dog;
                case PropSlot.Well: return well;
                case PropSlot.Haystack: return Pick(yardProps, YardHaystack);
                case PropSlot.Cart: return Pick(yardProps, YardCart);
                case PropSlot.Barrel: return Pick(clutter, ClutterBarrel);
                case PropSlot.Crates: return Pick(clutter, ClutterCrates);
                case PropSlot.Sandbags: return Pick(clutter, ClutterSandbags);
                case PropSlot.Scrap: return Pick(clutter, ClutterScrap);
                case PropSlot.TankTrap: return Pick(clutter, ClutterTankTrap);
                case PropSlot.Wire: return Pick(clutter, ClutterWire);
                case PropSlot.Sign: return mineSign;
                case PropSlot.Pole: return pole;
                default: return null;
            }
        }

        // Positions inside the sprite sets wired by ConfigureRuin.
        private const int YardHaystack = 0, YardCart = 1;
        private const int ClutterBarrel = 0, ClutterCrates = 1, ClutterSandbags = 2, ClutterScrap = 3, ClutterTankTrap = 4, ClutterWire = 5;

        private static Sprite Pick(Sprite[] set, int index)
        {
            return set != null && index < set.Length ? set[index] : null;
        }

        private void PlaceCluster(int w)
        {
            foreach (AnchoredProp prop in DecorLayout.Cluster)
            {
                group = prop.group;
                Vector2 p = S(w, prop.dx, prop.dy);
                Place(prop.name, SpriteFor(prop.slot), p.x, p.y, prop.radius);
            }
        }

        // ---------------------------------------------------------------- tank tracks

        private static Sprite[] trackSprites;   // [AxisU] runs along u, [AxisV] runs along v

        /// <summary>One cell of double tread, 16 px wide so neighbouring pieces join without a seam.</summary>
        private static Sprite MakeTrack(bool alongU)
        {
            const int W = TrackW, H = TrackH;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var clear = new Color32[W * H];
            tex.SetPixels32(clear);
            int sx = alongU ? 1 : -1;               // screen x direction per step along the axis
            for (int side = -1; side <= 1; side += 2)
            {
                int ox = (alongU ? -TrackOffsetX : TrackOffsetX) * side;   // tracks sit either side, along the other axis
                int oy = TrackOffsetY * side;
                for (int i = -TrackHalfLength; i < TrackHalfLength; i++)
                {
                    int x = W / 2 + ox + sx * i;
                    int y = H / 2 + oy + Mathf.FloorToInt(i / (float)TrackSlope);
                    for (int t = 1 - TrackThickness; t <= 0; t++)
                    {
                        Color32 c = TrackColor;
                        if (((i + TrackTickOffset) & TrackTickMask) == 0) c = TrackTick;
                        Put(tex, x, y + t, c);
                    }
                    Put(tex, x, y + 1, TrackLip);               // raised lip on the far side
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(Num.Half, Num.Half), PixelConstants.PixelsPerUnit);
        }

        private static void Put(Texture2D t, int x, int y, Color32 c)
        {
            if (x >= 0 && y >= 0 && x < t.width && y < t.height) t.SetPixel(x, y, c);
        }

        /// <summary>Continuous double tread leaving the tank to the right, then running up the right side of the plot.</summary>
        private void Tracks(int w, int h, Vector2 tank)
        {
            group = TracksGroup;
            if (trackSprites == null || trackSprites[AxisU] == null) trackSprites = new[] { MakeTrack(true), MakeTrack(false) };
            float vt = tank.y;
            float uEnd = Mathf.Max(w + TrackEndPastField, tank.x + TrackEndPastTank);
            // along u: from just behind the tank out to the right side of the field
            for (float u = tank.x + TrackStartGap; u <= uEnd; u += TrackStep) Tread(AxisU, u, vt);
            // along v: up the right side, outside the fence
            for (float v = vt + TrackStep; v <= h + TrackRunOnPastField; v += TrackStep) Tread(AxisV, uEnd, v);
        }

        private void Tread(int axis, float u, float v)
        {
            var go = new GameObject("Track");
            go.transform.SetParent(GroupRoot(group), false);
            go.transform.position = IsoMath.SnapToPixel(P(u, v));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = trackSprites[axis];
            sr.sortingOrder = PixelConstants.SortingOrderGround + 1;
            claimed.Add(new Vector3(u, v, TrackClaimRadius));
        }

        private void Roadside(int w, int h, System.Random rng)
        {
            group = DecorLayout.Roadside;
            foreach (EdgeProp sign in DecorLayout.Signs)
            {
                Place(sign.name, SpriteFor(sign.slot), sign.widthShare * w + sign.uOffset, sign.heightShare * h + sign.vOffset, sign.radius);
            }

            // a leaning telegraph line
            for (int i = 0; i < PoleCount; i++)
            {
                float v = (i + Num.Half) * (h + PoleSpanPad) / PoleCount - PoleVStart;
                Place("Pole", pole, PoleU - (float)rng.NextDouble() * PoleUJitter, v, PoleRadius);
            }

            Sprite wire = SpriteFor(PropSlot.Wire);
            if (clutter != null && wire != null)
            {
                // barbed wire and junk strung around the field
                for (int i = 0; i < WireAlongU; i++)
                {
                    float u = WireMargin + (float)rng.NextDouble() * (w - WireMargin * 2f);
                    float wv = -WireBelowV - (float)rng.NextDouble() * WireBelowJitter;
                    if (IsClaimed(u, wv, WireClaimCheck)) continue;
                    Place("Wire", wire, u, wv, WireRadius);
                }
                for (int i = 0; i < WireAlongV; i++)
                {
                    float v = WireMargin + (float)rng.NextDouble() * (h - WireMargin * 2f);
                    Place("Wire", wire, w + WireSideU + (float)rng.NextDouble(), v, WireRadius);
                    Place("Junk", clutter[rng.Next(JunkKinds)], -JunkU - (float)rng.NextDouble(), v, WireRadius);
                }
            }
        }

        private void WheatFields(int w, int h, System.Random rng)
        {
            if (wheat == null || wheat.Length == 0) return;
            group = WheatGroup;
            // never-harvested wheat on both sides of the plot
            foreach (WheatSide side in WheatSides)
            {
                for (int i = 0; i < side.count; i++)
                {
                    float u = side.widthShare * w + side.uOffset + (float)rng.NextDouble() * side.uRange;
                    float v = WheatV + (float)rng.NextDouble() * (h - side.vTrim);
                    if (IsClaimed(u, v, WheatRadius)) continue;
                    // taller stalks toward the middle of the field, sparse trampled patches at its edges
                    Sprite s = wheat[rng.NextDouble() < TallWheatChance ? rng.Next(Mathf.Min(TallWheatKinds, wheat.Length)) : rng.Next(wheat.Length)];
                    Place("Wheat", s, u, v, WheatRadius, 0f, WindSway.Kind.Foliage, WheatSway);
                }
            }
        }

        // ---------------------------------------------------------------- scorched ground

        private void BuildDecals(int w, int h, System.Random rng)
        {
            group = DecalGroup;
            if (decals == null || decals.Length == 0)
            {
                var list = new List<Sprite>();
                var gen = new System.Random(DecalSpriteSeed);
                foreach (GroundDecals.Kind kind in System.Enum.GetValues(typeof(GroundDecals.Kind)))
                {
                    foreach (int size in DecalSizes) list.Add(GroundDecals.Make(gen, kind, size));
                }
                decals = list.ToArray();
            }

            // out in the meadow
            int placed = 0, tries = 0;
            while (placed < DecalCount && tries < DecalCount * PlacementTries)
            {
                tries++;
                float u = -DecalMargin + (float)rng.NextDouble() * (w + DecalSpan);
                float v = -DecalMargin + (float)rng.NextDouble() * (h + DecalSpan);
                if (InsidePlotMargin(u, v, w, h)) continue;   // plot, fence, lane stay clean
                Decal(u, v, rng);
                placed++;
            }

            // a few hits on the dirt ring just inside the fence
            for (int i = 0; i < RingDecalCount; i++)
            {
                float t = (float)rng.NextDouble();
                switch (rng.Next(RingSides))
                {
                    case 0: Decal(t * w, -RingOffset, rng); break;
                    case 1: Decal(t * w, h + RingOffset, rng); break;
                    case 2: Decal(-RingOffset, t * h, rng); break;
                    default: Decal(w + RingOffset, t * h, rng); break;
                }
            }
        }

        private const int RingSides = 4;

        private static bool InsidePlotMargin(float u, float v, int w, int h)
        {
            return u > -PlotClearMargin && u < w + PlotClearMargin && v > -PlotClearMargin && v < h + PlotClearMargin;
        }

        private void Decal(float u, float v, System.Random rng)
        {
            Sprite s = decals[rng.Next(decals.Length)];
            var go = new GameObject("Decal");
            go.transform.SetParent(GroupRoot(group), false);
            go.transform.position = IsoMath.SnapToPixel(P(u, v));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = PixelConstants.SortingOrderGround + 1;
        }

        // ---------------------------------------------------------------- random meadow props

        /// <summary>Trees, bushes, grass tufts, junk and rubble in the meadow around the plot (never inside the fence).</summary>
        private void Scatter(int w, int h, System.Random rng)
        {
            group = MeadowGroup;
            const double TuftsTo = ChanceTufts;
            const double BushesTo = TuftsTo + ChanceBushes;
            const double TreesTo = BushesTo + ChanceTrees;
            const double RubbleTo = TreesTo + ChanceRubble;
            const double WheatTo = RubbleTo + ChanceWheat;

            int placed = 0, tries = 0;
            while (placed < ScatterCount && tries < ScatterCount * PlacementTries)
            {
                tries++;
                float u = -ScatterMargin + (float)rng.NextDouble() * (w + ScatterSpan);
                float v = -ScatterMargin + (float)rng.NextDouble() * (h + ScatterSpan);
                // keep the plot, the fence ring and the entrance lane clear
                if (InsidePlotMargin(u, v, w, h)) continue;
                if (v < 0f && Mathf.Abs(u - w * Num.Half) < LaneHalfWidth) continue;
                if (IsClaimed(u, v, ScatterClaim)) continue;

                double roll = rng.NextDouble();
                Sprite[] set;
                bool sway = false;
                if (roll < TuftsTo) { set = scatterTufts; sway = true; }
                else if (roll < BushesTo) { set = scatterBushes; sway = true; }
                else if (roll < TreesTo) set = scatterTrees;
                else if (roll < RubbleTo) set = scatterRubble;
                else if (roll < WheatTo && wheat != null && wheat.Length > SmallWheatFirst) { set = new[] { wheat[SmallWheatFirst], wheat[(SmallWheatFirst + 1) % wheat.Length] }; sway = true; }
                else set = clutter != null && clutter.Length > 0 ? clutter : scatterRubble;
                if (set == null || set.Length == 0) continue;

                Sprite s = set[rng.Next(set.Length)];
                var go = Prop("Scatter", s, P(u, v) - new Vector3(0f, ScatterDropPx / PixelConstants.PixelsPerUnit, 0f));
                if (go != null && sway) WindSway.Add(go, WindSway.Kind.Foliage);
                placed++;
            }
        }

        // ---------------------------------------------------------------- helpers

        private bool IsClaimed(float u, float v, float extra)
        {
            foreach (Vector3 c in claimed)
            {
                float r = c.z + extra;
                if ((u - c.x) * (u - c.x) + (v - c.y) * (v - c.y) < r * r) return true;
            }
            return false;
        }

        /// <summary>Puts a prop at a cell-edge position and reserves the ground around it.</summary>
        private GameObject Place(string objectName, Sprite sprite, float u, float v, float radius, float dyPx = 0f,
                                 WindSway.Kind? sway = null, float swayAmount = 1f)
        {
            if (sprite == null) return null;
            claimed.Add(new Vector3(u, v, radius));
            GameObject go = Prop(objectName, sprite, P(u, v) + new Vector3(0f, dyPx / PixelConstants.PixelsPerUnit, 0f));
            if (go != null && sway.HasValue) WindSway.Add(go, sway.Value, swayAmount);
            return go;
        }

        private void Segment(bool alongU, float u, float v, System.Random rng, bool corner)
        {
            double roll = rng.NextDouble();
            Sprite[] damaged = alongU ? damagedX : damagedY;
            Sprite s;
            if (!corner && roll < MissingChance)
            {
                return;   // a whole section is gone
            }
            if (!corner && roll < MissingChance + DamagedChance && damaged != null && damaged.Length > 0)
            {
                s = damaged[rng.Next(damaged.Length)];
            }
            else
            {
                bool broken = rng.NextDouble() < brokenChance;
                s = alongU ? (broken ? fenceXBroken : fenceX) : (broken ? fenceYBroken : fenceY);
            }
            if (s == null) return;
            Vector3 pos = P(u, v) - new Vector3(0f, FenceBaseOffsetPx / PixelConstants.PixelsPerUnit, 0f);
            Prop(alongU ? "FenceX" : "FenceY", s, pos);
        }

        private GameObject Prop(string objectName, Sprite sprite, Vector3 position)
        {
            if (sprite == null) return null;
            var go = new GameObject(objectName);
            go.transform.SetParent(GroupRoot(group), false);
            go.transform.position = IsoMath.SnapToPixel(position);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = PixelConstants.SortingOrderDefault;
            sr.spriteSortPoint = SpriteSortPoint.Pivot;
            return go;
        }
    }
}
