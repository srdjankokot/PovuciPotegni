using System.Collections.Generic;

namespace PovuciPotegni.Game
{
    public enum HairStyle
    {
        BackwardCap,
        BaldWithTufts,
        SweptFringe,
        Spiky,
    }

    public enum Outfit
    {
        Jersey,
        ShirtAndBowTie,
        Hoodie,
        StripedTee,
        Turtleneck,
    }

    /// <summary>Colors and accessories of one character (docs/SCENA.md, reference/slike/p-*.png).</summary>
    public sealed class CharacterLook
    {
        public string Id;
        public HairStyle Hair;
        public Outfit Outfit;
        public uint Shirt;
        public uint Pants;
        public uint Skin;
        public uint HairColor;
        public uint Brow;
        public uint Shoes;
        public uint Sole = 0xFFFFFF;
        public uint? Stripe;
        public uint? Cap;
        public uint? Mustache;
        public uint? BowTie;
        public uint? Belt;
        public bool Glasses;
        public bool NumberSeven;

        public static readonly CharacterLook Player = new CharacterLook
        {
            Id = "player",
            Hair = HairStyle.BackwardCap,
            Outfit = Outfit.Jersey,
            Shirt = 0x3550E8,
            Stripe = 0xFFFFFF,
            NumberSeven = true,
            Pants = 0x2E3A59,
            Skin = 0xE8B48F,
            HairColor = 0x3B2A20,
            Brow = 0x3B2A20,
            Cap = 0xFF5A4F,
            Shoes = 0xF3F4F8,
            Sole = 0x2E3A59,
        };

        public static readonly CharacterLook Segrt = new CharacterLook
        {
            Id = "segrt",
            Hair = HairStyle.SweptFringe,
            Outfit = Outfit.Hoodie,
            Shirt = 0x3BBF8E,
            Pants = 0x3A4A5A,
            Skin = 0xF2C7A5,
            HairColor = 0x9A5F2A,
            Brow = 0x9A5F2A,
            Shoes = 0x3A2A20,
            Sole = 0x1B1C24,
        };

        public static readonly CharacterLook Profesor = new CharacterLook
        {
            Id = "profesor",
            Hair = HairStyle.BaldWithTufts,
            Outfit = Outfit.ShirtAndBowTie,
            Shirt = 0xFF8A3D,
            Pants = 0x5A4A3A,
            Skin = 0xF2C7A5,
            HairColor = 0xD7D9DF,
            Brow = 0xB8BBC3,
            Mustache = 0xD7D9DF,
            BowTie = 0x2638B8,
            Belt = 0x6B4A2E,
            Shoes = 0x3A2A20,
            Sole = 0x1B1C24,
            Glasses = true,
        };

        public static readonly CharacterLook Lisac = new CharacterLook
        {
            Id = "lisac",
            Hair = HairStyle.Spiky,
            Outfit = Outfit.StripedTee,
            Shirt = 0x6E4BD8,
            Stripe = 0xF2CF55,
            Pants = 0x2E2A4A,
            Skin = 0xF2C7A5,
            HairColor = 0xD9572B,
            Brow = 0xC24A22,
            Shoes = 0x3A2A20,
            Sole = 0x1B1C24,
        };

        public static readonly CharacterLook Majstor = new CharacterLook
        {
            Id = "majstor",
            Hair = HairStyle.SweptFringe,
            Outfit = Outfit.Turtleneck,
            Belt = 0x15171F,
            Shirt = 0x1E2230,
            Pants = 0x2A2D38,
            Skin = 0xE8B48F,
            HairColor = 0xB4BAC2,
            Brow = 0x8E949C,
            Mustache = 0xB4BAC2,
            Shoes = 0x1B1C24,
            Sole = 0x1B1C24,
            Glasses = true,
        };

        public static readonly IReadOnlyList<CharacterLook> All = new[] { Player, Segrt, Profesor, Lisac, Majstor };
    }
}
