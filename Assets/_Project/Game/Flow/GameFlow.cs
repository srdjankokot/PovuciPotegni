using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PovuciPotegni.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace PovuciPotegni.Game
{
    public enum GameScreen
    {
        Loading,
        Home,
        Opponents,
        Match,
        Result,
        Stats,
        Settings,
        Error,
    }

    /// <summary>The screens' state machine, and the owner of everything shared between them.</summary>
    public sealed class GameFlow : MonoBehaviour
    {
        private const int TargetFrameRate = 60;

        [Serializable]
        public struct BotEntry
        {
            public string Id;
            public PoseController View;
            public Texture2D Portrait;
        }

        [SerializeField] private UIDocument document;
        [SerializeField] private GameConfigAsset configAsset;
        [SerializeField] private ArenaDirector director;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private MatchController matchController;
        [SerializeField] private SfxPlayer sfxPlayer;
        [SerializeField] private BotEntry[] bots = new BotEntry[0];

        [SerializeField] private VisualTreeAsset homeTree;
        [SerializeField] private VisualTreeAsset opponentsTree;
        [SerializeField] private VisualTreeAsset opponentCardTree;
        [SerializeField] private VisualTreeAsset matchTree;
        [SerializeField] private VisualTreeAsset resultTree;
        [SerializeField] private VisualTreeAsset statsTree;
        [SerializeField] private VisualTreeAsset settingsTree;
        [SerializeField] private VisualTreeAsset errorTree;

        private readonly Dictionary<GameScreen, UiScreen> _screens = new Dictionary<GameScreen, UiScreen>();
        private readonly IClock _clock = new UnityClock();

        private PlayerPrefsStatsStore _store;
        private UiContext _context;
        private QuestionBank _bank;
        private MatchScreen _matchScreen;
        private ResultScreen _resultScreen;
        private Vector3 _appliedSafeArea;
        private GameConfig Config => configAsset.Config;

        public GameScreen Current { get; private set; } = GameScreen.Loading;
        public VisualElement Root => document.rootVisualElement;
        public MatchController Match => matchController;
        public ArenaDirector Director => director;
        public PlayerData Data => _store.Data;
        public PanelSettings Panel => document.panelSettings;

        public void Bind(
            UIDocument uiDocument,
            GameConfigAsset config,
            ArenaDirector arenaDirector,
            CameraRig rig,
            MatchController match,
            SfxPlayer sfx,
            BotEntry[] botEntries,
            VisualTreeAsset[] trees)
        {
            document = uiDocument;
            configAsset = config;
            director = arenaDirector;
            cameraRig = rig;
            matchController = match;
            sfxPlayer = sfx;
            bots = botEntries;
            homeTree = trees[0];
            opponentsTree = trees[1];
            opponentCardTree = trees[2];
            matchTree = trees[3];
            resultTree = trees[4];
            statsTree = trees[5];
            settingsTree = trees[6];
            errorTree = trees[7];
        }

        public void ShowHome()
        {
            matchController.Stop();
            director.ResetArena();
            ShowOpponentView(Data.SelectedBot(Config).Id);
            cameraRig.SetMenuMode(true);
            Show(GameScreen.Home);
        }

        public void StartMatch(string botId)
        {
            BotProfile bot = Config.Bots.First(profile => profile.Id == botId);
            Data.SelectedBotId = botId;
            _store.Save();

            director.ResetArena();
            ShowOpponentView(botId);
            cameraRig.SetMenuMode(false);
            Show(GameScreen.Match);

            var service = new BotMatchService(Config, new QuestionPicker(_bank, _store), _clock);
            matchController.Begin(service, bot, Environment.TickCount);
        }

        private void Awake()
        {
            Application.targetFrameRate = TargetFrameRate;
            _store = new PlayerPrefsStatsStore();
            _context = new UiContext
            {
                Text = new Localization(_store.Data.Script),
                Config = Config,
                Store = _store,
                Portrait = id => bots.First(bot => bot.Id == id).Portrait,
            };
        }

        private void Start()
        {
            CreateScreens();
            cameraRig.SetMenuMode(true);
            cameraRig.Snap();
            ApplySafeArea();
            StartCoroutine(LoadQuestions());
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _store?.Save();
            }
        }

        private void Update()
        {
            ApplySafeArea();

            // Android's back button arrives as Escape. During a match it does nothing, so a slip cannot end the match.
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnBackPressed();
            }
        }

        private void OnBackPressed()
        {
            switch (Current)
            {
                case GameScreen.Home:
                    Application.Quit();
                    break;
                case GameScreen.Opponents:
                case GameScreen.Stats:
                case GameScreen.Settings:
                case GameScreen.Result:
                    ShowHome();
                    break;
            }
        }

        private void CreateScreens()
        {
            VisualElement root = Root;
            root.AddToClassList("screen");

            _screens[GameScreen.Home] = new HomeScreen(
                homeTree,
                root,
                _context,
                OnPlayPressed,
                () => Show(GameScreen.Opponents),
                () => Show(GameScreen.Stats),
                () => Show(GameScreen.Settings));

            _screens[GameScreen.Opponents] = new OpponentsScreen(opponentsTree, opponentCardTree, root, _context, ShowHome, StartMatch);

            _matchScreen = new MatchScreen(matchTree, root, _context, matchController.Answer, matchController.UseJoker, matchController.Next, ShowHome, matchController.ReportLastQuestion);
            _screens[GameScreen.Match] = _matchScreen;

            _resultScreen = new ResultScreen(
                resultTree,
                root,
                _context,
                () => StartMatch(Data.SelectedBot(Config).Id),
                ShowOpponentsAfterMatch,
                ShowHome,
                matchController.ReportLastQuestion);
            _screens[GameScreen.Result] = _resultScreen;

            _screens[GameScreen.Stats] = new StatsScreen(statsTree, root, _context, ShowHome);
            _screens[GameScreen.Settings] = new SettingsScreen(settingsTree, root, _context, ShowHome, OnScriptChanged);
            _screens[GameScreen.Error] = new ErrorScreen(errorTree, root, _context, () => StartCoroutine(LoadQuestions()));

            var feedback = new GameFeedback(sfxPlayer, _store.Data);
            matchController.Bind(director, _matchScreen, _store, _clock, Config, new NullAnalytics(), new LocalReportSink(_store), feedback, OnMatchFinished);
        }

        private IEnumerator LoadQuestions()
        {
            yield return QuestionLoader.Load(
                Config.AllowUnverified,
                bank =>
                {
                    _bank = bank;
                    if (bank.Questions.Count == 0)
                    {
                        Debug.LogWarning("Nema pitanja za igru. Proverite verifiedBy ili AllowUnverified.");
                        Show(GameScreen.Error);
                    }
                    else
                    {
                        ShowHome();
                    }
                },
                error =>
                {
                    Debug.LogWarning($"Pitanja nisu učitana: {error}");
                    Show(GameScreen.Error);
                });
        }

        private void Show(GameScreen screen)
        {
            foreach (KeyValuePair<GameScreen, UiScreen> entry in _screens)
            {
                if (entry.Key != screen)
                {
                    entry.Value.Hide();
                }
            }

            Current = screen;
            _screens[screen].Show();
        }

        private void ShowOpponentView(string botId)
        {
            foreach (BotEntry entry in bots)
            {
                entry.View.gameObject.SetActive(entry.Id == botId);
                if (entry.Id == botId)
                {
                    director.SetOpponent(entry.View);
                    entry.View.ShowIdle();
                    entry.View.SetMood(CharacterMood.Grit);
                }
            }
        }

        /// <summary>With a choice to make the player picks an opponent first; with one opponent the match starts at once.</summary>
        private void OnPlayPressed()
        {
            if (Data.UnlockedCount(Config) > 1)
            {
                Show(GameScreen.Opponents);
            }
            else
            {
                StartMatch(Data.SelectedBot(Config).Id);
            }
        }

        private void ShowOpponentsAfterMatch()
        {
            director.ResetArena();
            cameraRig.SetMenuMode(true);
            Show(GameScreen.Opponents);
        }

        private void OnMatchFinished(MatchEnded ended, string botId)
        {
            _resultScreen.Set(ended, botId);
            Show(GameScreen.Result);
        }

        private void OnScriptChanged()
        {
            foreach (UiScreen screen in _screens.Values)
            {
                screen.Refresh();
            }
        }

        /// <summary>Keeps the interface clear of notches and rounded corners.</summary>
        private void ApplySafeArea()
        {
            VisualElement root = Root;
            if (root == null || root.panel == null || Screen.width <= 0 || Screen.height <= 0 || root.layout.height <= 0)
            {
                return;
            }

            Rect safe = Screen.safeArea;
            float toPanel = root.layout.height / Screen.height;
            var applied = new Vector3(safe.yMin, safe.yMax, toPanel);
            if (applied == _appliedSafeArea)
            {
                return;
            }

            _appliedSafeArea = applied;
            foreach (VisualElement child in root.Children())
            {
                child.style.top = (Screen.height - safe.yMax) * toPanel;
                child.style.bottom = safe.yMin * toPanel;
            }
        }
    }
}
