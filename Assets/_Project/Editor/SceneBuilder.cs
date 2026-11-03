using System.Collections.Generic;
using System.Linq;
using PovuciPotegni.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PovuciPotegni.EditorTools
{
    /// <summary>Builds the prefabs, portraits and both scenes from code. Safe to run again.</summary>
    public static class SceneBuilder
    {
        public const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        public const string SandboxScenePath = "Assets/_Project/Scenes/Sandbox.unity";
        public const string CharacterFolder = "Assets/_Project/Art/Characters";

        private const string UiFolder = "Assets/_Project/Game/UI";
        private const string ScreensFolder = UiFolder + "/Screens";
        private const string PanelSettingsPath = "Assets/_Project/Settings/UiPanel.asset";
        private const string GameConfigPath = "Assets/_Project/Settings/GameConfig.asset";
        private const int ReferenceWidth = 390;
        private const int ReferenceHeight = 844;

        private static readonly Color32 SkyColor = new Color32(0xA8, 0xDC, 0xFF, 0xFF);
        private const uint KnotColor = 0xB3262B;
        private const uint RibbonColor = 0xE5484D;
        private const uint RopeColor = 0xFFFFFF;

        private const string SandboxOpponentId = "profesor";

        private static readonly string[] BotIds = { "segrt", "profesor", "lisac", "majstor" };

        [MenuItem("Povuci-Potegni/Izgradi arenu")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/_Project/Art", "Characters");
            EnsureFolder("Assets/_Project", "Scenes");

            var assets = new SceneAssets();
            Dictionary<string, GameObject> prefabs = BuildCharacterPrefabs(assets);
            GameObject crocodilePrefab = BuildCrocodilePrefab(assets);

            BuildSandboxScene(assets, prefabs, crocodilePrefab);
            Dictionary<string, Texture2D> portraits = PortraitRenderer.RenderAll();
            PortraitRenderer.RenderBranding();
            BuildGameScene(assets, prefabs, crocodilePrefab, portraits);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(GameScenePath, true),
                new EditorBuildSettingsScene(SandboxScenePath, true),
            };

            AssetDatabase.SaveAssets();
            ProjectSetup.Apply();
            Debug.Log("Povuci-Potegni: arena, likovi, portreti i scene su izgrađeni.");
        }

        private static void BuildSandboxScene(SceneAssets assets, Dictionary<string, GameObject> prefabs, GameObject crocodilePrefab)
        {
            Scene scene = NewScene();
            ArenaParts parts = BuildArena(assets, prefabs[CharacterLook.Player.Id], prefabs[SandboxOpponentId], crocodilePrefab);

            new GameObject("Sandbox").AddComponent<SandboxController>().Bind(parts.Director);

            EditorSceneManager.SaveScene(scene, SandboxScenePath);
            LogSceneCost();
        }

        private static void BuildGameScene(
            SceneAssets assets,
            Dictionary<string, GameObject> prefabs,
            GameObject crocodilePrefab,
            Dictionary<string, Texture2D> portraits)
        {
            Scene scene = NewScene();
            ArenaParts parts = BuildArena(assets, prefabs[CharacterLook.Player.Id], prefabs[BotIds[0]], crocodilePrefab);

            var entries = new List<GameFlow.BotEntry>();
            foreach (string botId in BotIds)
            {
                PoseController view = botId == BotIds[0]
                    ? parts.Opponent
                    : PlaceCharacter(prefabs[botId], ArenaLayout.OpponentZ(0), 180).GetComponent<PoseController>();

                entries.Add(new GameFlow.BotEntry { Id = botId, View = view, Portrait = portraits[botId] });
                view.gameObject.SetActive(botId == BotIds[0]);
            }

            var flowObject = new GameObject("GameFlow");
            var document = flowObject.AddComponent<UIDocument>();
            document.panelSettings = EnsurePanelSettings();

            var match = flowObject.AddComponent<MatchController>();
            var audioSource = flowObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            var sfx = flowObject.AddComponent<SfxPlayer>();
            sfx.Bind(audioSource, SoundBaker.BakeAll());

            var flow = flowObject.AddComponent<GameFlow>();
            flow.Bind(
                document,
                AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath),
                parts.Director,
                parts.Camera,
                match,
                sfx,
                entries.ToArray(),
                new[]
                {
                    Tree("Home"),
                    Tree("Opponents"),
                    Tree("OpponentCard"),
                    Tree("Match"),
                    Tree("Result"),
                    Tree("Stats"),
                    Tree("Settings"),
                    Tree("Error"),
                });

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static VisualTreeAsset Tree(string name)
        {
            string path = $"{ScreensFolder}/{name}.uxml";
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            if (tree == null)
            {
                throw new System.IO.FileNotFoundException("UXML nije pronađen.", path);
            }

            return tree;
        }

        /// <summary>A panel scaled to the design's width. Theme.tss brings in the styles every screen uses.</summary>
        private static PanelSettings EnsurePanelSettings()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }

            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>($"{UiFolder}/Theme.tss");
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(ReferenceWidth, ReferenceHeight);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0;

            EditorUtility.SetDirty(panel);
            return panel;
        }

        private readonly struct ArenaParts
        {
            public readonly ArenaDirector Director;
            public readonly CameraRig Camera;
            public readonly PoseController Opponent;

            public ArenaParts(ArenaDirector director, CameraRig camera, PoseController opponent)
            {
                Director = director;
                Camera = camera;
                Opponent = opponent;
            }
        }

        private static Scene NewScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.fog = false;
            return scene;
        }

        private static ArenaParts BuildArena(SceneAssets assets, GameObject playerPrefab, GameObject opponentPrefab, GameObject crocodilePrefab)
        {
            GameObject arena = new ArenaBuilder(assets).Build();
            MarkStatic(arena.transform.Find(ArenaBuilder.StaticGroupName));
            CreateSun();

            CharacterRig player = PlaceCharacter(playerPrefab, ArenaLayout.PlayerZ(0), 0);
            CharacterRig opponent = PlaceCharacter(opponentPrefab, ArenaLayout.OpponentZ(0), 180);

            RopeView rope = CreateRope(assets, player, opponent);
            CameraRig cameraRig = CreateCamera(player.transform);

            var crocodile = ((GameObject)PrefabUtility.InstantiatePrefab(crocodilePrefab)).GetComponent<CrocodileView>();
            FxPool fx = CreateFxPool(assets);

            var director = new GameObject("ArenaDirector").AddComponent<ArenaDirector>();
            var opponentView = opponent.GetComponent<PoseController>();
            director.Bind(player.GetComponent<PoseController>(), opponentView, rope, cameraRig, crocodile, fx);

            rope.Rebuild(0);
            cameraRig.Snap();
            return new ArenaParts(director, cameraRig, opponentView);
        }

        private static GameObject BuildCrocodilePrefab(ISceneAssets assets)
        {
            GameObject instance = new CrocodileBuilder(assets).Build();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{CharacterFolder}/crocodile.prefab");
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static FxPool CreateFxPool(SceneAssets assets)
        {
            var fx = new GameObject("FxPool").AddComponent<FxPool>();
            fx.Bind(
                assets.GetFxMaterial(),
                assets.GetMesh("Rock", () => ProceduralMeshes.Dodecahedron("Rock")),
                assets.GetMesh("FxPuff", () => ProceduralMeshes.Sphere("FxPuff", 1, 8, 6, false)),
                assets.GetMesh("FxRing", () => ProceduralMeshes.Torus("FxRing", 0.3f, 0.035f, 6, 32, false)),
                assets.GetMesh("Plane", () => ProceduralMeshes.Plane("Plane")));
            return fx;
        }

        private static void LogSceneCost()
        {
            MeshFilter[] filters = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
            int triangles = filters.Where(filter => filter.sharedMesh != null).Sum(filter => filter.sharedMesh.triangles.Length / 3);
            Debug.Log($"Povuci-Potegni: scena ima {filters.Length} objekata i {triangles} trouglova (bez užeta).");
        }

        private static Dictionary<string, GameObject> BuildCharacterPrefabs(ISceneAssets assets)
        {
            var builder = new CharacterBuilder(assets);
            var prefabs = new Dictionary<string, GameObject>();

            foreach (CharacterLook look in CharacterLook.All)
            {
                GameObject instance = builder.Build(look);
                prefabs[look.Id] = PrefabUtility.SaveAsPrefabAsset(instance, $"{CharacterFolder}/{look.Id}.prefab");
                Object.DestroyImmediate(instance);
            }

            return prefabs;
        }

        private static CharacterRig PlaceCharacter(GameObject prefab, float z, float yaw)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(0, 0, z);
            instance.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return instance.GetComponent<CharacterRig>();
        }

        private static void CreateSun()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color32(0xFF, 0xF3, 0xE0, 0xFF);
            sun.intensity = 0.7f;
            sun.shadows = LightShadows.Soft;
            sun.shadowBias = 0.05f;
            sun.shadowNormalBias = 0.15f;
            sun.transform.position = new Vector3(7, 14, -9);
            sun.transform.rotation = Quaternion.LookRotation(-sun.transform.position.normalized);
        }

        private static RopeView CreateRope(ISceneAssets assets, CharacterRig player, CharacterRig opponent)
        {
            var ropeObject = new GameObject("Rope");
            ropeObject.AddComponent<MeshFilter>();
            var renderer = ropeObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = assets.GetMaterial(new Surface(RopeColor, ropeStripes: true));
            renderer.shadowCastingMode = ShadowCastingMode.On;

            Transform knot = SceneParts.Group("Knot", ropeObject.transform);
            Mesh ball = assets.GetMesh("KnotBall", () => ProceduralMeshes.Sphere("KnotBall", 0.1f, 8, 6, true));
            Mesh ribbon = assets.GetMesh("KnotRibbon", () => ProceduralMeshes.Cone("KnotRibbon", 0.11f, 0.5f, 4, true));

            SceneParts.Part("Ball", knot, ball, assets.GetMaterial(new Surface(KnotColor)));
            foreach (float side in new[] { -1f, 1f })
            {
                SceneParts.Part(
                    "Ribbon",
                    knot,
                    ribbon,
                    assets.GetMaterial(new Surface(RibbonColor)),
                    new Vector3(side * 0.07f, -0.26f, 0),
                    SceneParts.Rotation(0, 0, Mathf.PI + side * 0.25f));
            }

            var rope = ropeObject.AddComponent<RopeView>();
            rope.Bind(player, opponent, knot);
            return rope;
        }

        private static CameraRig CreateCamera(Transform player)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkyColor;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.fieldOfView = ArenaLayout.PortraitFov;

            cameraObject.AddComponent<AudioListener>();

            var rig = cameraObject.AddComponent<CameraRig>();
            rig.Bind(player);
            return rig;
        }

        private static void MarkStatic(Transform root)
        {
            foreach (Transform part in root.GetComponentsInChildren<Transform>(true).Where(item => item != null))
            {
                GameObjectUtility.SetStaticEditorFlags(part.gameObject, StaticEditorFlags.BatchingStatic);
            }
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
