namespace MinefieldClerk.Field
{
    /// <summary>Which sprite a placed prop uses; resolved by <see cref="FieldDecor"/> from the sprites wired into it.</summary>
    public enum PropSlot
    {
        Farmhouse, Barn, Tank, Kennel, Dog, Well,
        Cart, Haystack,                                   // farmyard props set
        Barrel, Crates, Sandbags, Scrap, TankTrap, Wire,  // clutter set
        Sign, Pole,
    }

    /// <summary>A prop placed by its screen offset (world units) from the start cell, so it keeps its place on screen for every field size.</summary>
    public readonly struct AnchoredProp
    {
        public readonly string group;
        public readonly string name;
        public readonly PropSlot slot;
        public readonly float dx, dy;
        public readonly float radius;   // ground reserved around the prop (cells)

        public AnchoredProp(string group, string name, PropSlot slot, float dx, float dy, float radius)
        {
            this.group = group; this.name = name; this.slot = slot; this.dx = dx; this.dy = dy; this.radius = radius;
        }
    }

    /// <summary>A prop placed relative to the field edges: u = widthShare * width + uOffset, v = heightShare * height + vOffset.</summary>
    public readonly struct EdgeProp
    {
        public readonly string name;
        public readonly PropSlot slot;
        public readonly float widthShare, uOffset, heightShare, vOffset;
        public readonly float radius;

        public EdgeProp(string name, PropSlot slot, float widthShare, float uOffset, float heightShare, float vOffset, float radius)
        {
            this.name = name; this.slot = slot;
            this.widthShare = widthShare; this.uOffset = uOffset; this.heightShare = heightShare; this.vOffset = vOffset;
            this.radius = radius;
        }
    }

    /// <summary>
    /// The hand-placed scenery as data. Everything big stands right in front of the entrance, inside the first screen the
    /// player sees. Edit the numbers here to move props; code never contains coordinates.
    /// </summary>
    public static class DecorLayout
    {
        public const string Buildings = "Buildings";
        public const string Military = "Military";
        public const string Farmyard = "Farmyard";
        public const string Roadside = "Roadside";

        public static readonly AnchoredProp[] Cluster =
        {
            new AnchoredProp(Buildings, "Farmhouse", PropSlot.Farmhouse, -5.0f, -4.0f, 2.6f),
            new AnchoredProp(Buildings, "Barn",      PropSlot.Barn,       4.6f, -3.6f, 2.2f),
            new AnchoredProp(Military,  "Tank",      PropSlot.Tank,       5.8f,  0.4f, 2.4f),

            // the dog sleeps on its leash beside the kennel, next to the house door
            new AnchoredProp(Farmyard, "Kennel", PropSlot.Kennel, -2.3f, -4.0f, 1.2f),
            new AnchoredProp(Farmyard, "Dog",    PropSlot.Dog,    -1.4f, -4.1f, 0.9f),
            new AnchoredProp(Farmyard, "Well",   PropSlot.Well,    0.3f, -4.0f, 1.4f),
            new AnchoredProp(Farmyard, "Cart",   PropSlot.Cart,    2.2f, -3.9f, 1.4f),
            // haystacks together beside the barn
            new AnchoredProp(Farmyard, "Haystack", PropSlot.Haystack, 6.7f, -4.0f, 1.0f),
            new AnchoredProp(Farmyard, "Haystack", PropSlot.Haystack, 6.1f, -3.1f, 1.0f),
            new AnchoredProp(Farmyard, "Haystack", PropSlot.Haystack, 7.2f, -3.2f, 1.0f),
            new AnchoredProp(Farmyard, "Barrel", PropSlot.Barrel,  1.2f, -4.1f, 0.8f),
            new AnchoredProp(Farmyard, "Crates", PropSlot.Crates, -0.9f, -3.0f, 1.0f),
            new AnchoredProp(Farmyard, "Scrap",  PropSlot.Scrap,  -3.4f, -2.8f, 1.2f),

            // sandbag nest and tank traps guard the tank, supplies sit beside it
            new AnchoredProp(Military, "Sandbags", PropSlot.Sandbags, 4.0f, -0.4f, 1.2f),
            new AnchoredProp(Military, "Sandbags", PropSlot.Sandbags, 7.0f, -1.2f, 1.2f),
            new AnchoredProp(Military, "TankTrap", PropSlot.TankTrap, 5.0f, -1.4f, 1.2f),
            new AnchoredProp(Military, "TankTrap", PropSlot.TankTrap, 7.2f, -2.3f, 1.2f),
            new AnchoredProp(Military, "Barrel",   PropSlot.Barrel,   7.3f, -0.4f, 0.8f),
            new AnchoredProp(Military, "Crates",   PropSlot.Crates,   3.0f, -0.9f, 1.0f),
        };

        /// <summary>Mine warning signs either side of the entrance lane and at the corners.</summary>
        public static readonly EdgeProp[] Signs =
        {
            new EdgeProp("Sign", PropSlot.Sign, 0.5f, -5.0f, 0f, -2.6f, 1.2f),
            new EdgeProp("Sign", PropSlot.Sign, 0.5f,  5.0f, 0f, -2.6f, 1.2f),
            new EdgeProp("Sign", PropSlot.Sign, 0f,   -2.6f, 0f, -2.6f, 1.2f),
            new EdgeProp("Sign", PropSlot.Sign, 1f,    2.6f, 0f, -2.6f, 1.2f),
            new EdgeProp("Sign", PropSlot.Sign, 1f,    2.6f, 1f,  2.6f, 1.2f),
        };
    }
}
