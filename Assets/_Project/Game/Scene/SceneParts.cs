using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace PovuciPotegni.Game
{
    /// <summary>What a surface looks like. Colors are sRGB hex values, as in docs/SCENA.md.</summary>
    public readonly struct Surface
    {
        public uint Color { get; }
        public bool Toon { get; }
        public float Emission { get; }
        public bool RopeStripes { get; }

        public Surface(uint color, bool toon = false, float emission = 0, bool ropeStripes = false)
        {
            Color = color;
            Toon = toon;
            Emission = emission;
            RopeStripes = ropeStripes;
        }
    }

    /// <summary>Supplies shared meshes and materials; the editor implementation saves them as assets.</summary>
    public interface ISceneAssets
    {
        Mesh GetMesh(string key, Func<Mesh> create);
        Material GetMaterial(Surface surface);
    }

    public static class SceneParts
    {
        public static Transform Group(string name, Transform parent, Vector3 position = default)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = position;
            return group;
        }

        public static Transform Part(
            string name,
            Transform parent,
            Mesh mesh,
            Material material,
            Vector3 position = default,
            Quaternion? rotation = null,
            Vector3? scale = null,
            bool castShadow = true)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation ?? Quaternion.identity;
            part.transform.localScale = scale ?? Vector3.one;

            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return part.transform;
        }

        /// <summary>A rotation from radians, in the same order as three.js Euler 'XYZ'.</summary>
        public static Quaternion Rotation(float x, float y = 0, float z = 0)
        {
            return Quaternion.AngleAxis(x * Mathf.Rad2Deg, Vector3.right)
                * Quaternion.AngleAxis(y * Mathf.Rad2Deg, Vector3.up)
                * Quaternion.AngleAxis(z * Mathf.Rad2Deg, Vector3.forward);
        }
    }
}
