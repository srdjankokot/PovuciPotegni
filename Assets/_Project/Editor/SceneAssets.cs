using System;
using System.Collections.Generic;
using System.IO;
using PovuciPotegni.Game;
using UnityEditor;
using UnityEngine;

namespace PovuciPotegni.EditorTools
{
    /// <summary>Saves generated meshes and materials as assets, so scenes and prefabs can reference them.</summary>
    internal sealed class SceneAssets : ISceneAssets
    {
        private const string GeneratedFolder = "Assets/_Project/Art/Generated";
        private const string MeshAssetPath = GeneratedFolder + "/SceneMeshes.asset";
        private const string RopeTexturePath = GeneratedFolder + "/RopeStripes.png";
        private const string MaterialFolder = GeneratedFolder + "/Materials";
        private const string ShaderName = "PovuciPotegni/Stylized";
        private const string FxShaderName = "PovuciPotegni/Fx";

        private const int RopeTextureWidth = 64;
        private const int RopeTextureHeight = 16;
        private static readonly Color32 RopeLight = new Color32(0xF2, 0xCF, 0x55, 0xFF);
        private static readonly Color32 RopeDark = new Color32(0xD4, 0xA6, 0x2A, 0xFF);

        private readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();
        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        private Mesh _meshContainer;
        private Texture2D _ropeTexture;

        /// <summary>Starts from an empty generated folder, so the output always matches the code.</summary>
        public SceneAssets()
        {
            AssetDatabase.DeleteAsset(GeneratedFolder);
            AssetDatabase.CreateFolder("Assets/_Project/Art", "Generated");
            AssetDatabase.CreateFolder(GeneratedFolder, "Materials");
        }

        public Mesh GetMesh(string key, Func<Mesh> create)
        {
            if (_meshes.TryGetValue(key, out Mesh existing))
            {
                return existing;
            }

            Mesh mesh = create();
            if (_meshContainer == null)
            {
                AssetDatabase.CreateAsset(mesh, MeshAssetPath);
                _meshContainer = mesh;
            }
            else
            {
                AssetDatabase.AddObjectToAsset(mesh, _meshContainer);
            }

            _meshes[key] = mesh;
            return mesh;
        }

        public Material GetMaterial(Surface surface)
        {
            string key = $"{surface.Color:X6}{(surface.Toon ? "_Toon" : "_Flat")}{(surface.Emission > 0 ? "_Glow" : "")}{(surface.RopeStripes ? "_Rope" : "")}";
            if (_materials.TryGetValue(key, out Material existing))
            {
                return existing;
            }

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                throw new FileNotFoundException("Shader nije pronađen.", ShaderName);
            }

            var material = new Material(shader);
            material.SetColor("_BaseColor", ToColor(surface.Color));
            material.SetFloat("_Toon", surface.Toon ? 1 : 0);
            material.SetFloat("_Emission", surface.Emission);
            if (surface.RopeStripes)
            {
                material.SetTexture("_BaseMap", GetRopeTexture());
            }

            AssetDatabase.CreateAsset(material, $"{MaterialFolder}/{key}.mat");
            _materials[key] = material;
            return material;
        }

        public Material GetFxMaterial()
        {
            const string path = MaterialFolder + "/Fx.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find(FxShaderName);
            if (shader == null)
            {
                throw new FileNotFoundException("Shader nije pronađen.", FxShaderName);
            }

            var material = new Material(shader) { enableInstancing = true };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Color ToColor(uint hex)
        {
            return new Color32((byte)(hex >> 16), (byte)(hex >> 8), (byte)hex, 0xFF);
        }

        private Texture2D GetRopeTexture()
        {
            if (_ropeTexture != null)
            {
                return _ropeTexture;
            }

            var texture = new Texture2D(RopeTextureWidth, RopeTextureHeight, TextureFormat.RGBA32, false);
            for (int y = 0; y < RopeTextureHeight; y++)
            {
                for (int x = 0; x < RopeTextureWidth; x++)
                {
                    // Slanted stripes, 16 pixels apart, leaning one stripe width over the texture's height.
                    int phase = (x - y * 8 / RopeTextureHeight + RopeTextureWidth) % 16;
                    texture.SetPixel(x, y, phase < 7 ? RopeDark : RopeLight);
                }
            }

            File.WriteAllBytes(RopeTexturePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(RopeTexturePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(RopeTexturePath);
            importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();

            _ropeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(RopeTexturePath);
            return _ropeTexture;
        }
    }
}
