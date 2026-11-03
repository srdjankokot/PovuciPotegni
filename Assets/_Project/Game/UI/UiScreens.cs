using System;
using System.Collections.Generic;
using System.Linq;
using PovuciPotegni.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PovuciPotegni.Game
{
    /// <summary>What the screens need from the rest of the game.</summary>
    public sealed class UiContext
    {
        public Localization Text;
        public GameConfig Config;
        public IStatsStore Store;
        public Func<string, Texture2D> Portrait;

        public PlayerData Data => Store.Data;

        public string BotName(string botId)
        {
            return Text.Get($"bot.{botId}.name");
        }
    }

    /// <summary>A screen from UXML. Labels written as "@key" take their text from the Localization table.</summary>
    public abstract class UiScreen
    {
        private const char KeyPrefix = '@';

        private readonly List<KeyValuePair<TextElement, string>> _keyed = new List<KeyValuePair<TextElement, string>>();

        protected UiContext Context { get; }
        public VisualElement Root { get; }

        protected Localization Text => Context.Text;
        protected PlayerData Data => Context.Data;

        protected UiScreen(VisualTreeAsset tree, VisualElement parent, UiContext context)
        {
            Context = context;
            VisualElement container = tree.Instantiate();
            Root = container[0];
            parent.Add(Root);
            CollectKeyedText(Root);
            Hide();
        }

        public void Show()
        {
            Root.RemoveFromClassList("hidden");
            Refresh();
        }

        public void Hide()
        {
            Root.AddToClassList("hidden");
        }

        /// <summary>Reloads every text and value, for example after the script changes.</summary>
        public void Refresh()
        {
            foreach (KeyValuePair<TextElement, string> entry in _keyed)
            {
                entry.Key.text = Text.Get(entry.Value);
            }

            OnRefresh();
        }

        protected abstract void OnRefresh();

        protected T Find<T>(string name)
            where T : VisualElement
        {
            T element = Root.Q<T>(name);
            if (element == null)
            {
                throw new InvalidOperationException($"UI element '{name}' does not exist.");
            }

            return element;
        }

        protected Label Label(string name)
        {
            return Find<Label>(name);
        }

        protected void OnClick(string name, Action action)
        {
            Find<Button>(name).clicked += action;
        }

        protected static void SetPortrait(VisualElement element, Texture2D portrait)
        {
            element.style.backgroundImage = new StyleBackground(portrait);
        }

        protected static void SetVisible(VisualElement element, bool visible)
        {
            element.EnableInClassList("hidden", !visible);
        }

        protected static void SetProgress(VisualElement fill, int value, int total)
        {
            fill.style.width = Length.Percent(total == 0 ? 100 : Mathf.Clamp01(value / (float)total) * 100);
        }

        private void CollectKeyedText(VisualElement root)
        {
            foreach (TextElement element in root.Query<TextElement>().ToList())
            {
                if (!string.IsNullOrEmpty(element.text) && element.text[0] == KeyPrefix)
                {
                    _keyed.Add(new KeyValuePair<TextElement, string>(element, element.text.Substring(1)));
                }
            }
        }
    }

    public sealed class HomeScreen : UiScreen
    {
        public HomeScreen(VisualTreeAsset tree, VisualElement parent, UiContext context, Action play, Action opponents, Action stats, Action settings)
            : base(tree, parent, context)
        {
            OnClick("play", play);
            OnClick("opponents", opponents);
            OnClick("stats", stats);
            OnClick("settings", settings);
        }

        protected override void OnRefresh()
        {
            BotProfile bot = Data.SelectedBot(Context.Config);
            SetPortrait(Find<VisualElement>("play-portrait"), Context.Portrait(bot.Id));
            Label("play-sub").text = Text.Format("home.opponent", Context.BotName(bot.Id), Text.Get($"bot.{bot.Id}.level").ToLowerInvariant());

            List<BotProfile> bots = Context.Config.Bots;
            SetPortrait(Find<VisualElement>("tile-portrait-1"), Context.Portrait(bots[0].Id));
            SetPortrait(Find<VisualElement>("tile-portrait-2"), Context.Portrait(bots[1].Id));
            Label("unlocked").text = Text.Format("home.unlocked", Data.UnlockedCount(Context.Config), bots.Count);

            string strongest = Data.StrongestCategory();
            Label("strongest").text = strongest == null
                ? Text.Get("home.noData")
                : Text.Format("home.strongest", Text.Get($"category.{strongest}"));

            Label("stat-wins").text = Data.Wins.ToString();
            Label("stat-streak").text = Data.WinStreak.ToString();
            Label("stat-correct").text = $"{Mathf.RoundToInt((float)Data.CorrectShare * 100)}%";
        }
    }

    public sealed class OpponentsScreen : UiScreen
    {
        private readonly Dictionary<string, VisualElement> _cards = new Dictionary<string, VisualElement>();

        public OpponentsScreen(VisualTreeAsset tree, VisualTreeAsset cardTree, VisualElement parent, UiContext context, Action back, Action<string> play)
            : base(tree, parent, context)
        {
            OnClick("back", back);
            var list = Find<ScrollView>("list");

            foreach (BotProfile bot in context.Config.Bots)
            {
                VisualElement card = cardTree.Instantiate()[0];
                string botId = bot.Id;

                card.RegisterCallback<ClickEvent>(_ => Select(botId));
                card.Q<Button>("play").clicked += () => play(botId);
                list.Add(card);
                _cards[bot.Id] = card;
            }
        }

        protected override void OnRefresh()
        {
            string selected = Data.SelectedBot(Context.Config).Id;

            foreach (BotProfile bot in Context.Config.Bots)
            {
                VisualElement card = _cards[bot.Id];
                bool unlocked = Data.IsUnlocked(bot);
                bool isSelected = unlocked && bot.Id == selected;
                string level = Text.Get($"bot.{bot.Id}.level");

                card.EnableInClassList("bot-card--selected", isSelected);
                card.EnableInClassList("bot-card--locked", !unlocked);
                SetPortrait(card.Q("portrait"), Context.Portrait(bot.Id));
                SetVisible(card.Q("lock"), !unlocked);

                card.Q<Label>("name").text = Context.BotName(bot.Id);
                card.Q<Label>("count").text = unlocked ? "" : Text.Format("opponents.progress", Data.Wins, bot.WinsToUnlock);
                card.Q<Label>("info").text = unlocked
                    ? $"{level} · {Text.Get($"bot.{bot.Id}.traits")}"
                    : Text.Format("opponents.locked", level, bot.WinsToUnlock, Text.WinsWord(bot.WinsToUnlock));

                int wins = Data.WinsAgainst(bot.Id);
                Label winsLabel = card.Q<Label>("wins");
                winsLabel.text = $"{wins} {Text.WinsWord(wins)}";
                SetVisible(winsLabel, unlocked && !isSelected && wins > 0);

                SetVisible(card.Q("track"), !unlocked);
                SetProgress(card.Q("fill"), Data.Wins, bot.WinsToUnlock);

                SetVisible(card.Q("play"), isSelected);
                card.Q<Label>("play-label").text = Text.Format("opponents.play", Text.Get($"bot.{bot.Id}.genitive"));
            }
        }

        private void Select(string botId)
        {
            BotProfile bot = Context.Config.Bots.First(profile => profile.Id == botId);
            if (Data.IsUnlocked(bot) && Data.SelectedBotId != botId)
            {
                Data.SelectedBotId = botId;
                Context.Store.Save();
                Refresh();
            }
        }
    }

    public sealed class ResultScreen : UiScreen
    {
        private MatchEnded _ended;
        private string _botId;

        private bool _reported;

        public ResultScreen(VisualTreeAsset tree, VisualElement parent, UiContext context, Action rematch, Action other, Action home, Action report)
            : base(tree, parent, context)
        {
            OnClick("report", () =>
            {
                report();
                _reported = true;
                Refresh();
            });
            OnClick("rematch", rematch);
            OnClick("other", other);
            OnClick("home", home);
        }

        public void Set(MatchEnded ended, string botId)
        {
            _ended = ended;
            _botId = botId;
            _reported = false;
        }

        protected override void OnRefresh()
        {
            if (_ended == null)
            {
                return;
            }

            bool won = _ended.Winner == Side.Player;
            Label title = Label("title");
            title.text = Text.Get(won ? "result.win" : "result.lose");
            title.EnableInClassList("result-title--lose", !won);

            Label("after").text = Text.Format("result.after", _ended.QuestionsPlayed);
            Label("text").text = Reason(won);
            Label("your-correct").text = $"{_ended.PlayerCorrect}/{_ended.QuestionsPlayed}";
            Label("streak").text = Data.WinStreak.ToString();

            Find<Button>("report").SetEnabled(!_reported);
            Label("report-label").text = Text.Get(_reported ? "report.done" : "report.button");

            BotProfile next = Data.NextLockedBot(Context.Config);
            SetVisible(Find<VisualElement>("next-bot"), next != null);
            if (next != null)
            {
                SetPortrait(Find<VisualElement>("next-portrait"), Context.Portrait(next.Id));
                Label("next-name").text = Text.Format("result.toNext", Text.Get($"bot.{next.Id}.genitive"));
                Label("next-count").text = Text.Format("result.toNextProgress", Data.Wins, next.WinsToUnlock);
                SetProgress(Find<VisualElement>("next-fill"), Data.Wins, next.WinsToUnlock);
            }
        }

        private string Reason(bool won)
        {
            string name = Context.BotName(_botId);
            switch (_ended.Reason)
            {
                case MatchEndReason.Edge:
                    return Text.Format(won ? "result.winEdge" : "result.loseEdge", name);
                case MatchEndReason.Overtime:
                    return Text.Get(won ? "result.winOvertime" : "result.loseOvertime");
                default:
                    return Text.Get(won ? "result.winPoints" : "result.losePoints");
            }
        }
    }

    public sealed class StatsScreen : UiScreen
    {
        public StatsScreen(VisualTreeAsset tree, VisualElement parent, UiContext context, Action back)
            : base(tree, parent, context)
        {
            OnClick("back", back);
        }

        protected override void OnRefresh()
        {
            Label("matches").text = Data.Matches.ToString();
            Label("wins").text = Data.Wins.ToString();
            Label("streak").text = Data.WinStreak.ToString();
            Label("best").text = Data.BestWinStreak.ToString();
            Label("correct").text = $"{Mathf.RoundToInt((float)Data.CorrectShare * 100)}%";

            VisualElement categories = Find<VisualElement>("categories");
            categories.Clear();

            List<string> played = QuestionCategories.All
                .Where(category => Data.AnswersIn(category) > 0)
                .OrderByDescending(Data.CorrectShareIn)
                .ToList();

            SetVisible(Label("empty"), played.Count == 0);
            foreach (string category in played)
            {
                categories.Add(CategoryRow(category));
            }
        }

        private VisualElement CategoryRow(string category)
        {
            int percent = Mathf.RoundToInt((float)Data.CorrectShareIn(category) * 100);

            var row = new VisualElement();
            row.AddToClassList("category-row");

            var line = new VisualElement();
            line.AddToClassList("row");
            line.AddToClassList("category-line");

            var name = new Label(Text.Get($"category.{category}"));
            name.AddToClassList("category-name");
            var value = new Label(Text.Format("stats.categoryLine", percent, Data.AnswersIn(category)));
            value.AddToClassList("category-value");
            line.Add(name);
            line.Add(value);

            var track = new VisualElement();
            track.AddToClassList("track");
            var fill = new VisualElement();
            fill.AddToClassList("track-fill");
            fill.style.width = Length.Percent(percent);
            track.Add(fill);

            row.Add(line);
            row.Add(track);
            return row;
        }
    }

    public sealed class SettingsScreen : UiScreen
    {
        private readonly Action _scriptChanged;

        public SettingsScreen(VisualTreeAsset tree, VisualElement parent, UiContext context, Action back, Action scriptChanged)
            : base(tree, parent, context)
        {
            _scriptChanged = scriptChanged;
            OnClick("back", back);
            OnClick("latin", () => SetScript(TextScript.Latin));
            OnClick("cyrillic", () => SetScript(TextScript.Cyrillic));
            OnClick("sound", () => Change(() => Data.SoundOn = !Data.SoundOn));
            OnClick("haptics", () => Change(() => Data.HapticsOn = !Data.HapticsOn));
        }

        protected override void OnRefresh()
        {
            Find<Button>("latin").EnableInClassList("choice--on", Data.Script == TextScript.Latin);
            Find<Button>("cyrillic").EnableInClassList("choice--on", Data.Script == TextScript.Cyrillic);
            ShowToggle("sound", Data.SoundOn);
            ShowToggle("haptics", Data.HapticsOn);
        }

        private void ShowToggle(string name, bool on)
        {
            var button = Find<Button>(name);
            button.EnableInClassList("toggle--on", on);
            button.Q<Label>().text = Text.Get(on ? "settings.on" : "settings.off");
        }

        private void SetScript(TextScript script)
        {
            Data.Script = script;
            Text.Script = script;
            Context.Store.Save();
            _scriptChanged();
        }

        private void Change(Action change)
        {
            change();
            Context.Store.Save();
            Refresh();
        }
    }

    public sealed class ErrorScreen : UiScreen
    {
        public ErrorScreen(VisualTreeAsset tree, VisualElement parent, UiContext context, Action retry)
            : base(tree, parent, context)
        {
            OnClick("retry", retry);
        }

        protected override void OnRefresh()
        {
        }
    }
}
