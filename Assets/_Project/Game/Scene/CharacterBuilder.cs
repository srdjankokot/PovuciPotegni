using System;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>
    /// Assembles a stocky character from primitives: a big head on a short body, with the joint hierarchy from
    /// docs/SCENA.md. The face and hair are laid out for a head of radius 0.3 and scaled up by the Head joint.
    /// </summary>
    public sealed class CharacterBuilder
    {
        private const uint MouthColor = 0x4A1F22;
        private const uint EyeColor = 0x1B1C24;
        private const uint EyeHighlightColor = 0xFFFFFF;
        private const uint CheekColor = 0xF0A08C;
        private const uint BuckleColor = 0xE0B040;
        private const uint WhiteTrimColor = 0xF3F4F8;
        private const uint GlassesColor = 0x2A2B33;
        private const uint TeethColor = 0xFFFFFF;
        private const uint PenColor = 0x2638B8;

        private const float HipsHeight = 0.6f;
        private const float HeadHeight = 0.82f;
        private const float HeadScale = 1.27f;

        private static readonly Quaternion Flat = SceneParts.Rotation(Mathf.PI / 2);

        private readonly ISceneAssets _assets;

        public CharacterBuilder(ISceneAssets assets)
        {
            _assets = assets;
        }

        public GameObject Build(CharacterLook look)
        {
            var root = new GameObject(look.Id);
            Transform hips = SceneParts.Group("Hips", root.transform, new Vector3(0, HipsHeight, 0));
            Part("Pelvis", hips, Sphere(0.29f, 16, 12), look.Pants, Vector3.zero, null, new Vector3(1, 0.62f, 0.85f));

            var thighs = new Transform[2];
            var knees = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                BuildLeg(hips, look, i == 0 ? -1 : 1, out thighs[i], out knees[i]);
            }

            Transform body = SceneParts.Group("Body", hips);
            BuildTorso(body, look);
            BuildOutfit(body, look);

            Transform head = SceneParts.Group("Head", body, new Vector3(0, HeadHeight, 0));
            head.localScale = Vector3.one * HeadScale;
            var pupils = new Transform[2];
            var brows = new Transform[2];
            BuildFace(head, look, pupils, brows, out GameObject grit, out GameObject oh, out GameObject smile);
            BuildHair(head, look);

            var shoulders = new Transform[2];
            var elbows = new Transform[2];
            var hands = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                BuildArm(body, look, i == 0 ? -1 : 1, out shoulders[i], out elbows[i], out hands[i]);
            }

            var rig = root.AddComponent<CharacterRig>();
            rig.Bind(hips, body, head, thighs, knees, shoulders, elbows, hands, brows, pupils, grit, oh, smile);
            rig.ApplyBracePose();
            rig.SetMood(CharacterMood.Grit);

            // A fixed phase per character keeps both from breathing in step.
            root.AddComponent<PoseController>().Bind(rig, look.Id.Length * 1.3f, 0);
            return root;
        }

        private void BuildLeg(Transform hips, CharacterLook look, float side, out Transform thigh, out Transform knee)
        {
            thigh = SceneParts.Group("Thigh", hips, new Vector3(side * 0.16f, -0.04f, 0));
            Part("ThighMesh", thigh, Cylinder(0.145f, 0.125f, 0.24f, 16), look.Pants, new Vector3(0, -0.12f, 0));

            knee = SceneParts.Group("Knee", thigh, new Vector3(0, -0.24f, 0));
            Part("KneeMesh", knee, Sphere(0.138f, 20, 16), look.Pants, Vector3.zero);
            Part("Shin", knee, Cylinder(0.12f, 0.11f, 0.2f, 16), look.Pants, new Vector3(0, -0.1f, 0));
            Part("PantsCuff", knee, Cylinder(0.123f, 0.123f, 0.05f, 16), Shade(look.Pants, 0.8f), new Vector3(0, -0.175f, 0));

            Part("Shoe", knee, Sphere(0.17f, 16, 12), look.Shoes, new Vector3(0, -0.24f, 0.07f), null, new Vector3(0.95f, 0.6f, 1.4f));
            Part("ToeCap", knee, Sphere(0.115f, 12, 8), Shade(look.Shoes, 0.88f), new Vector3(0, -0.262f, 0.2f), null, new Vector3(1.15f, 0.72f, 0.85f));
            Part("Sole", knee, Box(), look.Sole, new Vector3(0, -0.29f, 0.07f), null, new Vector3(0.22f, 0.035f, 0.38f));

            for (int i = 0; i < 2; i++)
            {
                float z = 0.03f + i * 0.065f;
                Part("Lace", knee, Box(), look.Sole, new Vector3(0, -0.145f - i * 0.018f, z), SceneParts.Rotation(0.45f), new Vector3(0.12f, 0.016f, 0.028f));
            }
        }

        private void BuildTorso(Transform body, CharacterLook look)
        {
            Part("Torso", body, Cylinder(0.27f, 0.32f, 0.44f, 20), look.Shirt, new Vector3(0, 0.22f, 0));
            Part("Chest", body, Sphere(0.27f, 18, 12), look.Shirt, new Vector3(0, 0.42f, 0), null, new Vector3(1.08f, 0.62f, 0.92f));
            Part("Belly", body, Sphere(0.32f, 24, 16), look.Shirt, new Vector3(0, 0.03f, 0), null, new Vector3(1, 0.34f, 0.92f));
            Part("Neck", body, Cylinder(0.1f, 0.11f, 0.14f, 12), look.Skin, new Vector3(0, 0.55f, 0));

            if (look.Stripe.HasValue)
            {
                Part("Stripe", body, Cylinder(0.305f, 0.313f, 0.06f, 20), look.Stripe.Value, new Vector3(0, 0.13f, 0));
            }

            if (look.Belt.HasValue)
            {
                uint buckle = look.Outfit == Outfit.Turtleneck ? 0xC8CCD4 : BuckleColor;
                Part("Belt", body, Cylinder(0.325f, 0.33f, 0.07f, 20), look.Belt.Value, new Vector3(0, 0.06f, 0));
                Part("Buckle", body, Box(), buckle, new Vector3(0, 0.06f, 0.325f), null, new Vector3(0.1f, 0.075f, 0.025f));
                Part("BuckleCenter", body, Box(), look.Belt.Value, new Vector3(0, 0.06f, 0.336f), null, new Vector3(0.05f, 0.032f, 0.008f));
            }
            else
            {
                Part("Hem", body, Cylinder(0.338f, 0.34f, 0.045f, 24), Shade(look.Shirt, 0.8f), new Vector3(0, 0.015f, 0));
            }
        }

        private void BuildOutfit(Transform body, CharacterLook look)
        {
            switch (look.Outfit)
            {
                case Outfit.Jersey:
                    BuildJersey(body, look);
                    break;
                case Outfit.ShirtAndBowTie:
                    BuildShirtAndBowTie(body, look);
                    break;
                case Outfit.Hoodie:
                    BuildHoodie(body, look);
                    break;
                case Outfit.StripedTee:
                    Part("Collar", body, Ring(0.13f, 0.035f), Shade(look.Shirt, 0.72f), new Vector3(0, 0.5f, 0), Flat);
                    Part("ChestMark", body, Sphere(0.05f, 10, 8), look.Stripe ?? WhiteTrimColor, new Vector3(-0.13f, 0.34f, 0.25f), null, new Vector3(1, 1, 0.25f));
                    break;
                case Outfit.Turtleneck:
                    BuildTurtleneck(body, look);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(look), look.Outfit, null);
            }
        }

        private void BuildJersey(Transform body, CharacterLook look)
        {
            uint trim = look.Stripe ?? WhiteTrimColor;
            Part("Collar", body, Ring(0.13f, 0.035f), trim, new Vector3(0, 0.5f, 0), Flat);

            if (look.NumberSeven)
            {
                Part("NumberTop", body, Box(), WhiteTrimColor, new Vector3(0, 0.355f, -0.283f), null, new Vector3(0.14f, 0.03f, 0.01f));
                Part("NumberStem", body, Box(), WhiteTrimColor, new Vector3(0.012f, 0.28f, -0.291f), SceneParts.Rotation(0, 0, -0.35f), new Vector3(0.03f, 0.15f, 0.01f));
            }

            // Team badge on the chest.
            Part("Badge", body, Sphere(0.045f, 10, 8), trim, new Vector3(0.14f, 0.34f, 0.245f), null, new Vector3(1, 1, 0.25f));
        }

        private void BuildShirtAndBowTie(Transform body, CharacterLook look)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                Part("CollarWing", body, Box(), WhiteTrimColor, new Vector3(side * 0.08f, 0.5f, 0.15f), SceneParts.Rotation(0.3f, 0, side * -0.6f), new Vector3(0.1f, 0.05f, 0.03f));

                if (look.BowTie.HasValue)
                {
                    Mesh wing = _assets.GetMesh("BowTieWing", () => ProceduralMeshes.Cone("BowTieWing", 0.08f, 0.13f, 4, false));
                    Part("BowTie", body, wing, look.BowTie.Value, new Vector3(side * 0.065f, 0.49f, 0.19f), SceneParts.Rotation(0, 0, side * Mathf.PI / 2));
                }
            }

            if (look.BowTie.HasValue)
            {
                Part("BowTieKnot", body, Sphere(0.028f, 8, 6), Shade(look.BowTie.Value, 0.8f), new Vector3(0, 0.49f, 0.2f));
            }

            foreach (float y in new[] { 0.36f, 0.27f, 0.18f })
            {
                Part("Button", body, Sphere(0.018f, 8, 6), WhiteTrimColor, new Vector3(0, y, 0.272f + (0.36f - y) * 0.12f), null, new Vector3(1, 1, 0.5f));
            }

            // A chest pocket with a pen, turned to follow the body's curve.
            Quaternion pocketTurn = SceneParts.Rotation(0, -0.47f);
            Part("Pocket", body, Box(), Shade(look.Shirt, 0.84f), new Vector3(-0.13f, 0.29f, 0.25f), pocketTurn, new Vector3(0.11f, 0.1f, 0.012f));
            Part("Pen", body, Box(), PenColor, new Vector3(-0.115f, 0.355f, 0.258f), pocketTurn, new Vector3(0.016f, 0.06f, 0.012f));
        }

        private void BuildHoodie(Transform body, CharacterLook look)
        {
            Mesh hood = _assets.GetMesh("Hood", () => ProceduralMeshes.Torus("Hood", 0.16f, 0.085f, 10, 22, false));
            Part("Hood", body, hood, Shade(look.Shirt, 0.88f), new Vector3(0, 0.5f, -0.05f), SceneParts.Rotation(Mathf.PI / 2 - 0.3f));

            foreach (float side in new[] { -1f, 1f })
            {
                Part("Drawstring", body, Box(), WhiteTrimColor, new Vector3(side * 0.05f, 0.32f, 0.27f), null, new Vector3(0.016f, 0.14f, 0.012f));
                Part("DrawstringEnd", body, Sphere(0.016f, 6, 5), WhiteTrimColor, new Vector3(side * 0.05f, 0.245f, 0.278f));
            }

            Part("Pocket", body, Box(), Shade(look.Shirt, 0.84f), new Vector3(0, 0.1f, 0.305f), null, new Vector3(0.28f, 0.1f, 0.03f));
        }

        private void BuildTurtleneck(Transform body, CharacterLook look)
        {
            Part("HighCollar", body, Cylinder(0.125f, 0.135f, 0.15f, 14), look.Shirt, new Vector3(0, 0.53f, 0));
            Part("CollarFold", body, Ring(0.128f, 0.025f), Shade(look.Shirt, 1.35f), new Vector3(0, 0.59f, 0), Flat);

            Mesh chain = _assets.GetMesh("MedalChain", () => ProceduralMeshes.Torus("MedalChain", 0.17f, 0.009f, 6, 22, false));
            Part("Chain", body, chain, BuckleColor, new Vector3(0, 0.43f, 0.09f), SceneParts.Rotation(Mathf.PI / 2 + 0.75f));
            Part("Medal", body, Cylinder(0.052f, 0.052f, 0.016f, 14), BuckleColor, new Vector3(0, 0.3f, 0.285f), Flat);
        }

        private void BuildFace(
            Transform head,
            CharacterLook look,
            Transform[] pupils,
            Transform[] brows,
            out GameObject grit,
            out GameObject oh,
            out GameObject smile)
        {
            Part("Skull", head, Sphere(0.3f, 24, 18), look.Skin, Vector3.zero);
            Part("Nose", head, Sphere(0.05f, 12, 8), look.Skin, new Vector3(0, -0.025f, 0.295f), null, new Vector3(1, 0.85f, 0.9f));

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1 : 1;
                Part("Ear", head, Sphere(0.065f, 12, 8), look.Skin, new Vector3(side * 0.29f, -0.02f, 0), null, new Vector3(0.6f, 1, 1));
                Part("EarInner", head, Sphere(0.032f, 8, 6), Shade(look.Skin, 0.82f), new Vector3(side * 0.316f, -0.02f, 0.012f), null, new Vector3(0.35f, 1, 1));
                Part("Cheek", head, Sphere(0.042f, 10, 8), CheekColor, new Vector3(side * 0.17f, -0.055f, 0.232f), null, new Vector3(1.1f, 0.7f, 0.3f));

                pupils[i] = Part("Eye", head, Sphere(0.042f, 12, 10), EyeColor, new Vector3(side * 0.115f, 0.035f, 0.268f), null, new Vector3(1, 1.2f, 0.5f));
                Part("EyeHighlight", head, Sphere(0.012f, 8, 6), EyeHighlightColor, new Vector3(side * 0.115f - 0.012f, 0.055f, 0.288f));
                brows[i] = Part("Brow", head, Sphere(0.056f, 10, 6), look.Brow, new Vector3(side * 0.115f, 0.125f, 0.258f), null, new Vector3(1, 0.34f, 0.32f));

                if (look.Glasses)
                {
                    Mesh ring = _assets.GetMesh("GlassesRing", () => ProceduralMeshes.Torus("GlassesRing", 0.08f, 0.013f, 8, 18, false));
                    Part("GlassesRing", head, ring, GlassesColor, new Vector3(side * 0.115f, 0.035f, 0.277f));
                    Part("GlassesArm", head, Box(), GlassesColor, new Vector3(side * 0.2f, 0.045f, 0.16f), null, new Vector3(0.012f, 0.012f, 0.25f));
                }

                if (look.Mustache.HasValue)
                {
                    Part(
                        "Mustache",
                        head,
                        Sphere(0.05f, 12, 8),
                        look.Mustache.Value,
                        new Vector3(side * 0.055f, -0.07f, 0.285f),
                        SceneParts.Rotation(0, 0, side * 0.25f),
                        new Vector3(1.3f, 0.55f, 0.6f));
                }
            }

            if (look.Glasses)
            {
                Part("GlassesBridge", head, Box(), GlassesColor, new Vector3(0, 0.045f, 0.288f), null, new Vector3(0.07f, 0.012f, 0.012f));
            }

            Transform mouth = SceneParts.Group("Mouth", head, new Vector3(0, -0.125f, 0.27f));

            Transform gritGroup = SceneParts.Group("Grit", mouth);
            Part("Lips", gritGroup, Box(), MouthColor, Vector3.zero, null, new Vector3(0.13f, 0.05f, 0.02f));
            Part("Teeth", gritGroup, Box(), TeethColor, new Vector3(0, 0, 0.004f), null, new Vector3(0.115f, 0.032f, 0.022f));
            grit = gritGroup.gameObject;

            oh = Part("Oh", mouth, Sphere(0.045f, 12, 10), MouthColor, Vector3.zero, null, new Vector3(0.9f, 1.2f, 0.4f)).gameObject;

            Mesh smileMesh = _assets.GetMesh("Smile", () => ProceduralMeshes.Torus("Smile", 0.06f, 0.016f, 8, 16, false, Mathf.PI));
            smile = Part("Smile", mouth, smileMesh, MouthColor, new Vector3(0, 0.03f, 0), SceneParts.Rotation(0, 0, Mathf.PI)).gameObject;
        }

        private void BuildHair(Transform head, CharacterLook look)
        {
            switch (look.Hair)
            {
                case HairStyle.BackwardCap:
                    BuildBackwardCap(head, look);
                    break;
                case HairStyle.BaldWithTufts:
                    BuildBaldWithTufts(head, look);
                    break;
                case HairStyle.SweptFringe:
                    BuildHairCap(head, look);
                    BuildHairClumps(head, look);
                    Part("Fringe", head, Sphere(0.2f, 14, 10), look.HairColor, new Vector3(-0.1f, 0.24f, 0.12f), SceneParts.Rotation(-0.35f, 0, 0.3f), new Vector3(1.2f, 0.32f, 0.85f));
                    Part("FringeTip", head, Sphere(0.1f, 12, 8), Shade(look.HairColor, 0.9f), new Vector3(0.1f, 0.25f, 0.13f), SceneParts.Rotation(-0.3f, 0, -0.45f), new Vector3(1.2f, 0.4f, 0.9f));
                    break;
                case HairStyle.Spiky:
                    BuildHairCap(head, look);
                    BuildHairClumps(head, look);
                    BuildSpikes(head, look);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(look), look.Hair, null);
            }
        }

        private void BuildHairCap(Transform head, CharacterLook look)
        {
            Mesh cap = _assets.GetMesh("HairCap", () => ProceduralMeshes.Sphere("HairCap", 0.305f, 20, 14, false, Mathf.PI * 0.55f));
            Part("Hair", head, cap, look.HairColor, Vector3.zero, SceneParts.Rotation(-0.78f));
        }

        /// <summary>Rounded masses of hair around the sides and back, so the hair is not a smooth shell.</summary>
        private void BuildHairClumps(Transform head, CharacterLook look)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                Part("Sideburn", head, Sphere(0.07f, 10, 8), look.HairColor, new Vector3(side * 0.27f, 0.07f, 0.03f), null, new Vector3(0.6f, 1.2f, 0.9f));
                Part("SideClump", head, Sphere(0.1f, 10, 8), look.HairColor, new Vector3(side * 0.2f, 0.02f, -0.18f), null, new Vector3(0.95f, 1.05f, 0.95f));
            }

            Part("BackClump", head, Sphere(0.13f, 12, 8), look.HairColor, new Vector3(0, -0.04f, -0.24f), null, new Vector3(1.25f, 0.9f, 0.7f));
        }

        private void BuildBackwardCap(Transform head, CharacterLook look)
        {
            BuildHairCap(head, look);
            BuildHairClumps(head, look);

            uint capColor = look.Cap ?? look.HairColor;
            Mesh dome = _assets.GetMesh("CapDome", () => ProceduralMeshes.Sphere("CapDome", 0.315f, 20, 14, false, Mathf.PI * 0.42f));
            Mesh brim = _assets.GetMesh("CapBrim", () => ProceduralMeshes.Cylinder("CapBrim", 0.2f, 0.2f, 0.025f, 18, false, Mathf.PI));
            Mesh band = _assets.GetMesh("CapBand", () => ProceduralMeshes.Torus("CapBand", 0.305f, 0.016f, 8, 28, false));

            Part("CapDome", head, dome, capColor, new Vector3(0, 0.02f, 0), SceneParts.Rotation(-0.3f));
            Part("CapBand", head, band, Shade(capColor, 0.78f), new Vector3(0, 0.1f, -0.025f), SceneParts.Rotation(Mathf.PI / 2 - 0.3f));
            Part("CapBrim", head, brim, Shade(capColor, 0.9f), new Vector3(0, 0.02f, -0.27f), SceneParts.Rotation(0.15f, Mathf.PI / 2));
            Part("CapButton", head, Sphere(0.03f, 10, 8), Shade(capColor, 0.78f), new Vector3(0, 0.325f, -0.04f));

            // The adjuster opening of a backward cap, with hair showing through.
            Part("CapOpening", head, Sphere(0.06f, 10, 8), look.HairColor, new Vector3(0, 0.19f, 0.245f), SceneParts.Rotation(-0.7f), new Vector3(1.2f, 0.65f, 0.5f));
        }

        private void BuildBaldWithTufts(Transform head, CharacterLook look)
        {
            // The opening of the ring faces forward, so the hair stays behind the face.
            Mesh ring = _assets.GetMesh("HairRing", () => ProceduralMeshes.Torus("HairRing", 0.255f, 0.075f, 10, 24, false, Mathf.PI * 1.25f));
            Part("HairRing", head, ring, look.HairColor, new Vector3(0, 0.05f, 0), SceneParts.Rotation(Mathf.PI / 2, 0, Mathf.PI * 0.875f));

            foreach (float side in new[] { -1f, 1f })
            {
                Part("Tuft", head, Sphere(0.07f, 10, 8), look.HairColor, new Vector3(side * 0.27f, 0.12f, -0.02f));
                Part("TuftLower", head, Sphere(0.055f, 10, 8), Shade(look.HairColor, 0.92f), new Vector3(side * 0.285f, 0.05f, 0.03f));
            }

            Part("Shine", head, Sphere(0.05f, 10, 8), Shade(look.Skin, 1.12f), new Vector3(0.07f, 0.265f, 0.1f), SceneParts.Rotation(-0.4f, 0, -0.25f), new Vector3(1.2f, 0.3f, 0.8f));
        }

        private void BuildSpikes(Transform head, CharacterLook look)
        {
            Mesh spike = _assets.GetMesh("HairSpike", () => ProceduralMeshes.Cone("HairSpike", 0.085f, 0.3f, 6, false));

            for (int i = -2; i <= 2; i++)
            {
                float angle = i * 0.36f;
                var position = new Vector3(Mathf.Sin(angle) * 0.3f, Mathf.Cos(angle) * 0.3f + 0.05f, 0.02f);
                Part("Spike", head, spike, look.HairColor, position, SceneParts.Rotation(0, 0, -angle));
            }

            for (int i = -1; i <= 1; i++)
            {
                float angle = i * 0.55f;
                var position = new Vector3(Mathf.Sin(angle) * 0.27f, Mathf.Cos(angle) * 0.24f + 0.04f, -0.14f);
                Part("BackSpike", head, spike, Shade(look.HairColor, 0.88f), position, SceneParts.Rotation(-0.55f, 0, -angle), Vector3.one * 0.85f);
            }
        }

        private void BuildArm(Transform body, CharacterLook look, float side, out Transform shoulder, out Transform elbow, out Transform hand)
        {
            bool longSleeves = look.Outfit == Outfit.Hoodie || look.Outfit == Outfit.Turtleneck;

            shoulder = SceneParts.Group("Shoulder", body, new Vector3(side * 0.33f, 0.4f, 0));
            Part("ShoulderBall", shoulder, Sphere(0.115f, 16, 12), look.Shirt, Vector3.zero);
            Part("UpperArm", shoulder, Cylinder(0.105f, 0.095f, 0.24f, 16), look.Shirt, new Vector3(0, -0.12f, 0));

            elbow = SceneParts.Group("Elbow", shoulder, new Vector3(0, -0.24f, 0));
            Part("Forearm", elbow, Cylinder(0.09f, 0.085f, 0.22f, 16), longSleeves ? look.Shirt : look.Skin, new Vector3(0, -0.11f, 0));

            if (longSleeves)
            {
                Part("WristCuff", elbow, Cylinder(0.097f, 0.097f, 0.05f, 16), Shade(look.Shirt, 0.78f), new Vector3(0, -0.19f, 0));
            }
            else
            {
                Part("SleeveHem", shoulder, Cylinder(0.107f, 0.107f, 0.045f, 16), look.Stripe ?? Shade(look.Shirt, 0.8f), new Vector3(0, -0.215f, 0));
            }

            if (look.Outfit == Outfit.StripedTee && look.Stripe.HasValue)
            {
                Part("Wristband", elbow, Cylinder(0.097f, 0.097f, 0.06f, 16), look.Stripe.Value, new Vector3(0, -0.175f, 0));
            }

            hand = Part("Hand", elbow, Sphere(0.115f, 16, 12), look.Skin, new Vector3(0, -0.27f, 0), null, new Vector3(1, 1.05f, 0.95f));
            Part("Thumb", elbow, Sphere(0.048f, 10, 8), look.Skin, new Vector3(-side * 0.075f, -0.24f, 0.07f), null, new Vector3(0.9f, 1.25f, 0.9f));
        }

        private Transform Part(string name, Transform parent, Mesh mesh, uint color, Vector3 position, Quaternion? rotation = null, Vector3? scale = null)
        {
            return SceneParts.Part(name, parent, mesh, _assets.GetMaterial(new Surface(color, true)), position, rotation, scale);
        }

        private static uint Shade(uint color, float factor)
        {
            uint Channel(int shift)
            {
                return (uint)Mathf.Clamp(Mathf.RoundToInt(((color >> shift) & 0xFF) * factor), 0, 255) << shift;
            }

            return Channel(16) | Channel(8) | Channel(0);
        }

        private Mesh Box()
        {
            return _assets.GetMesh("Box", () => ProceduralMeshes.Box("Box"));
        }

        private Mesh Ring(float radius, float tube)
        {
            string key = $"Ring_{radius:F3}_{tube:F3}";
            return _assets.GetMesh(key, () => ProceduralMeshes.Torus(key, radius, tube, 8, 20, false));
        }

        private Mesh Sphere(float radius, int width, int height)
        {
            string key = $"Sphere_{radius:F3}_{width}_{height}";
            return _assets.GetMesh(key, () => ProceduralMeshes.Sphere(key, radius, width, height, false));
        }

        private Mesh Cylinder(float top, float bottom, float height, int segments)
        {
            string key = $"Cylinder_{top:F3}_{bottom:F3}_{height:F3}_{segments}";
            return _assets.GetMesh(key, () => ProceduralMeshes.Cylinder(key, top, bottom, height, segments, false));
        }
    }
}
