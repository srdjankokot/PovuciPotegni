using System.Collections.Generic;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>
    /// Dust, splash, water rings and confetti (docs/SCENA.md), as particle systems that are created once
    /// and reused.
    /// </summary>
    public sealed class FxPool : MonoBehaviour
    {
        private const float WaterHeight = -0.25f;
        private const float RingHeight = -0.3f;
        private const int SplashDrops = 70;
        private const int SplashRings = 3;
        private const float RingDelay = 0.25f;
        private const int DustPerBurst = 5;
        private const int ConfettiCount = 120;
        private const float GravityUnit = 9.81f;

        private static readonly Color32[] ConfettiColors =
        {
            new Color32(0x35, 0x50, 0xE8, 0xFF),
            new Color32(0xFF, 0x8A, 0x3D, 0xFF),
            new Color32(0xF2, 0xCF, 0x55, 0xFF),
            new Color32(0x3B, 0xBF, 0x8E, 0xFF),
            new Color32(0xE5, 0x48, 0x4D, 0xFF),
            new Color32(0xFF, 0xFF, 0xFF, 0xFF),
        };

        private static readonly Color32 DropWhite = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        private static readonly Color32 DropBlue = new Color32(0xBF, 0xE6, 0xFF, 0xFF);
        private static readonly Color32 DustColor = new Color32(0xD9, 0xC9, 0xA3, 0xB3);
        private static readonly Color32 RingColor = new Color32(0xFF, 0xFF, 0xFF, 0xCC);

        [SerializeField] private Material material;
        [SerializeField] private Mesh dropMesh;
        [SerializeField] private Mesh dustMesh;
        [SerializeField] private Mesh ringMesh;
        [SerializeField] private Mesh confettiMesh;

        private readonly List<PendingRing> _pendingRings = new List<PendingRing>();

        private ParticleSystem _drops;
        private ParticleSystem _dust;
        private ParticleSystem _rings;
        private ParticleSystem _confetti;

        private struct PendingRing
        {
            public float Delay;
            public Vector3 Position;
        }

        /// <summary>Particles currently alive in all systems.</summary>
        public int AliveCount
        {
            get
            {
                EnsureSystems();
                return _drops.particleCount + _dust.particleCount + _rings.particleCount + _confetti.particleCount;
            }
        }

        public void Bind(Material effectMaterial, Mesh drop, Mesh dustPuff, Mesh ring, Mesh confetti)
        {
            material = effectMaterial;
            dropMesh = drop;
            dustMesh = dustPuff;
            ringMesh = ring;
            confettiMesh = confetti;
        }

        public void Dust(Vector3 position)
        {
            EnsureSystems();

            for (int i = 0; i < DustPerBurst; i++)
            {
                var emit = new ParticleSystem.EmitParams
                {
                    position = position + new Vector3((Random.value - 0.5f) * 0.5f, 0, (Random.value - 0.5f) * 0.3f),
                    velocity = new Vector3((Random.value - 0.5f) * 0.8f, 0.4f + Random.value * 0.5f, (Random.value - 0.5f) * 0.8f),
                    startColor = DustColor,
                    startLifetime = 0.9f,
                    startSize = 0.12f,
                };
                _dust.Emit(emit, 1);
            }
        }

        public void Splash(float x, float z)
        {
            EnsureSystems();

            for (int i = 0; i < SplashDrops; i++)
            {
                float angle = Random.value * Mathf.PI * 2;
                float radius = Random.value * 0.5f;
                var emit = new ParticleSystem.EmitParams
                {
                    position = new Vector3(x + Mathf.Cos(angle) * radius, WaterHeight, z + Mathf.Sin(angle) * radius * 0.5f),
                    velocity = new Vector3(
                        Mathf.Cos(angle) * (1 + Random.value * 2.2f),
                        3 + Random.value * 4,
                        Mathf.Sin(angle) * (0.6f + Random.value * 1.2f)),
                    startColor = i % 3 == 0 ? DropBlue : DropWhite,
                    startLifetime = 1.2f,
                    startSize = 0.07f,
                };
                _drops.Emit(emit, 1);
            }

            for (int i = 0; i < SplashRings; i++)
            {
                _pendingRings.Add(new PendingRing { Delay = i * RingDelay, Position = new Vector3(x, RingHeight, z) });
            }
        }

        public void Confetti(Vector3 position)
        {
            EnsureSystems();

            for (int i = 0; i < ConfettiCount; i++)
            {
                var emit = new ParticleSystem.EmitParams
                {
                    position = position + new Vector3((Random.value - 0.5f) * 2, 0, (Random.value - 0.5f) * 2),
                    velocity = new Vector3((Random.value - 0.5f) * 4, 3 + Random.value * 4, (Random.value - 0.5f) * 4),
                    startColor = ConfettiColors[i % ConfettiColors.Length],
                    startLifetime = 3.2f,
                    startSize3D = new Vector3(0.12f, 1, 0.07f),
                    rotation3D = new Vector3(Random.value * 360, Random.value * 360, Random.value * 360),
                };
                _confetti.Emit(emit, 1);
            }
        }

        public void Clear()
        {
            EnsureSystems();
            _pendingRings.Clear();
            _drops.Clear();
            _dust.Clear();
            _rings.Clear();
            _confetti.Clear();
        }

        public void Tick(float deltaTime)
        {
            for (int i = _pendingRings.Count - 1; i >= 0; i--)
            {
                PendingRing ring = _pendingRings[i];
                ring.Delay -= deltaTime;

                if (ring.Delay > 0)
                {
                    _pendingRings[i] = ring;
                    continue;
                }

                _pendingRings.RemoveAt(i);
                EmitRing(ring.Position);
            }
        }

        /// <summary>Advances the particles by hand. Outside Play mode nothing else does.</summary>
        public void SimulateForCapture(float deltaTime)
        {
            EnsureSystems();
            foreach (ParticleSystem system in new[] { _drops, _dust, _rings, _confetti })
            {
                system.Simulate(deltaTime, false, false, false);
            }
        }

        private void EmitRing(Vector3 position)
        {
            EnsureSystems();
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                startColor = RingColor,
                startLifetime = 1.4f,
                startSize = 1,
                rotation3D = new Vector3(90, 0, 0),
            };
            _rings.Emit(emit, 1);
        }

        private void EnsureSystems()
        {
            if (_drops != null)
            {
                return;
            }

            _drops = CreateSystem("Drops", dropMesh, 9.8f, 200);
            SetSizeOverLife(_drops, 1, 0);

            _dust = CreateSystem("Dust", dustMesh, 0, 120);
            SetSizeOverLife(_dust, 1, 2.6f);
            SetFadeOut(_dust);

            _rings = CreateSystem("Rings", ringMesh, 0, 16);
            SetSizeOverLife(_rings, 0.2f, 4.2f);
            SetFadeOut(_rings);

            _confetti = CreateSystem("Confetti", confettiMesh, 3.2f, 200);
            SetSpin(_confetti);
            SetDrag(_confetti, 1.2f);
        }

        private ParticleSystem CreateSystem(string systemName, Mesh mesh, float gravity, int maxParticles)
        {
            var host = new GameObject(systemName) { hideFlags = HideFlags.DontSave };
            host.transform.SetParent(transform, false);
            host.SetActive(false);

            var system = host.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity / GravityUnit;
            main.startSize3D = true;
            main.startRotation3D = true;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.SetMeshes(new[] { mesh });
            renderer.sharedMaterial = material;
            renderer.alignment = ParticleSystemRenderSpace.World;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            host.SetActive(true);
            system.Play();
            return system;
        }

        private static void SetSizeOverLife(ParticleSystem system, float start, float end)
        {
            ParticleSystem.SizeOverLifetimeModule module = system.sizeOverLifetime;
            module.enabled = true;
            float peak = Mathf.Max(start, end);
            module.size = new ParticleSystem.MinMaxCurve(peak, AnimationCurve.Linear(0, start / peak, 1, end / peak));
        }

        private static void SetFadeOut(ParticleSystem system)
        {
            ParticleSystem.ColorOverLifetimeModule module = system.colorOverLifetime;
            module.enabled = true;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            module.color = gradient;
        }

        private static void SetSpin(ParticleSystem system)
        {
            ParticleSystem.RotationOverLifetimeModule module = system.rotationOverLifetime;
            module.enabled = true;
            module.separateAxes = true;
            module.x = new ParticleSystem.MinMaxCurve(6, 14);
            module.y = new ParticleSystem.MinMaxCurve(4, 10);
            module.z = new ParticleSystem.MinMaxCurve(0);
        }

        private static void SetDrag(ParticleSystem system, float drag)
        {
            ParticleSystem.LimitVelocityOverLifetimeModule module = system.limitVelocityOverLifetime;
            module.enabled = true;
            module.limit = float.PositiveInfinity;
            module.dampen = 0;
            module.drag = drag;
        }
    }
}
