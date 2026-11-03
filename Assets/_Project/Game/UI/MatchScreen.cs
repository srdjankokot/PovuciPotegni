using System;
using System.Collections.Generic;
using PovuciPotegni.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace PovuciPotegni.Game
{
    /// <summary>The match HUD, the question sheet and the result banner. It only shows what it is told.</summary>
    public sealed class MatchScreen : UiScreen
    {
        private const int OptionCount = 3;

        private enum JokerLook
        {
            Ready,
            Armed,
            Used,
        }

        private readonly Button[] _options = new Button[OptionCount];
        private readonly Label[] _optionTexts = new Label[OptionCount];
        private readonly Dictionary<JokerType, JokerLook> _jokers = new Dictionary<JokerType, JokerLook>();
        private readonly VisualElement _sheet;
        private readonly VisualElement _scrim;
        private readonly VisualElement _banner;
        private readonly VisualElement _timerBar;
        private readonly VisualElement _status;
        private readonly VisualElement _incoming;
        private readonly VisualElement _chips;
        private readonly Label _timerText;

        private int _shownSeconds = -1;
        private string _botId = "";
        private string _questionText = "";
        private IReadOnlyList<string> _optionValues;

        public MatchScreen(
            VisualTreeAsset tree,
            VisualElement parent,
            UiContext context,
            Action<int> answer,
            Action<JokerType> joker,
            Action next,
            Action leave,
            Action report)
            : base(tree, parent, context)
        {
            _sheet = Find<VisualElement>("sheet");
            _scrim = Find<VisualElement>("scrim");
            _banner = Find<VisualElement>("banner");
            _timerBar = Find<VisualElement>("timer-bar");
            _status = Find<VisualElement>("status");
            _incoming = Find<VisualElement>("incoming");
            _chips = Find<VisualElement>("chips");
            _timerText = Label("timer-text");

            for (int i = 0; i < OptionCount; i++)
            {
                int index = i;
                _options[i] = Find<Button>($"option-{i}");
                _optionTexts[i] = _options[i].Q<Label>(className: "opt-text");
                _options[i].clicked += () => answer(index);
            }

            OnClick("joker-half", () => joker(JokerType.FiftyFifty));
            OnClick("joker-shield", () => joker(JokerType.Shield));
            OnClick("joker-double", () => joker(JokerType.Double));
            OnClick("next", next);
            OnClick("leave", leave);
            OnClick("report", report);
        }

        public void BeginMatch(string botId)
        {
            _botId = botId;
            _jokers[JokerType.FiftyFifty] = JokerLook.Ready;
            _jokers[JokerType.Shield] = JokerLook.Ready;
            _jokers[JokerType.Double] = JokerLook.Ready;

            SetPortrait(Find<VisualElement>("bot-portrait"), Context.Portrait(botId));
            HideSheet();
            HideBanner();
            HideChips();
            HideIncoming();
            HideReportButton();
            ShowRope(0);
            Label("number").text = "";
            Label("category").text = "";
            Refresh();
        }

        public void ShowRope(int rope)
        {
            const int Edge = 3;
            Label("you-left").text = Text.Format("match.toWin", Edge - rope);
            Label("bot-left").text = Text.Format("match.toWin", Edge + rope);
        }

        public void ShowIncoming(int number)
        {
            Label("incoming-text").text = Text.Format("match.incoming", number);
            Label("number").text = Text.Format("match.questionOf", number, Context.Config.MaxQuestions);
            _incoming.RemoveFromClassList("hidden");
        }

        public void HideIncoming()
        {
            _incoming.AddToClassList("hidden");
        }

        public void ShowQuestion(QuestionShown question)
        {
            _questionText = question.Text;
            _shownSeconds = -1;
            _optionValues = question.Options;
            HideIncoming();
            HideBanner();
            HideChips();
            HideReportButton();

            Label("number").text = question.IsOvertime
                ? Text.Get("match.overtime")
                : Text.Format("match.questionOf", question.Number, Context.Config.MaxQuestions);
            Label("category").text = Text.Get($"category.{question.Category}");
            Label("question").text = Text.Content(_questionText);

            for (int i = 0; i < OptionCount; i++)
            {
                _options[i].ClearClassList();
                _options[i].AddToClassList("opt");
                _options[i].SetEnabled(true);
                _optionTexts[i].text = Text.Content(question.Options[i]);
                _options[i].Q<Label>(className: "opt-tag--bot").text = Context.BotName(_botId);
            }

            ShowStatus(Text.Format("match.thinking", Context.BotName(_botId)), "");
            ApplyJokers();

            _sheet.AddToClassList("sheet--open");
            _scrim.AddToClassList("scrim--on");
        }

        public void ShowOpponentAnswered()
        {
            ShowStatus(Text.Format("match.answered", Context.BotName(_botId)), "status--answered");
        }

        /// <summary>Locks the answers while waiting for the opponent and marks the player's choice.</summary>
        /// <param name="picked">The chosen option; -1 when time ran out.</param>
        public void ShowPlayerPick(int picked)
        {
            for (int i = 0; i < OptionCount; i++)
            {
                _options[i].SetEnabled(false);
                _options[i].EnableInClassList("opt--picked", i == picked);
                _options[i].EnableInClassList("opt--show-you", i == picked);
                _options[i].EnableInClassList("opt--dim", picked >= 0 && i != picked);
            }

            foreach (JokerType joker in new[] { JokerType.FiftyFifty, JokerType.Shield, JokerType.Double })
            {
                JokerButton(joker).SetEnabled(false);
            }
        }

        public void ShowTimer(float remainingShare, int remainingSeconds)
        {
            // Scaling needs no new layout, so the bar stays smooth; the text changes only once a second.
            _timerBar.style.scale = new Scale(new Vector2(Mathf.Clamp01(remainingShare), 1));
            if (remainingSeconds != _shownSeconds)
            {
                _shownSeconds = remainingSeconds;
                _timerText.text = Text.Format("match.seconds", remainingSeconds);
            }
        }

        public void ShowJoker(JokerUsed used)
        {
            _jokers[used.Joker] = used.Joker == JokerType.FiftyFifty ? JokerLook.Used : JokerLook.Armed;
            if (used.RemovedOptionIndex >= 0)
            {
                _options[used.RemovedOptionIndex].AddToClassList("opt--gone");
                _options[used.RemovedOptionIndex].SetEnabled(false);
            }

            ApplyJokers();
        }

        public void ShowRoundResult(RoundResolved round)
        {
            for (int i = 0; i < OptionCount; i++)
            {
                _options[i].SetEnabled(false);
                _options[i].RemoveFromClassList("opt--gone");
                _options[i].RemoveFromClassList("opt--picked");
                _options[i].RemoveFromClassList("opt--dim");
                bool picked = i == round.PlayerChoice || i == round.OpponentChoice;

                if (i == round.CorrectIndex)
                {
                    _options[i].AddToClassList("opt--correct");
                }
                else
                {
                    _options[i].AddToClassList(picked ? "opt--wrong" : "opt--dim");
                }

                _options[i].EnableInClassList("opt--show-you", i == round.PlayerChoice);
                _options[i].EnableInClassList("opt--show-bot", i == round.OpponentChoice);
            }

            ShowReportButton();

            string you = VerdictWord(round.PlayerChoice, round.CorrectIndex);
            string bot = VerdictWord(round.OpponentChoice, round.CorrectIndex);
            ShowStatus(
                $"{Text.Format("match.summaryYou", you)} · {Text.Format("match.summaryBot", Context.BotName(_botId), bot)}",
                "status--summary");

            Label("chip-you").text = Text.Format("match.chip", Text.Get("match.you"), OptionText(round.PlayerChoice), you);
            Label("chip-bot").text = Text.Format("match.chip", Context.BotName(_botId), OptionText(round.OpponentChoice), bot);

            // A Shield lasts one question; a Double lasts until it pulls.
            if (_jokers[JokerType.Shield] == JokerLook.Armed)
            {
                _jokers[JokerType.Shield] = JokerLook.Used;
            }

            if (round.DoubleUsedBy == Side.Player)
            {
                _jokers[JokerType.Double] = JokerLook.Used;
            }

            ApplyJokers();
        }

        public void HideSheet()
        {
            _sheet.RemoveFromClassList("sheet--open");
            _scrim.RemoveFromClassList("scrim--on");
        }

        public void ShowChips()
        {
            _chips.RemoveFromClassList("hidden");
        }

        public void HideChips()
        {
            _chips.AddToClassList("hidden");
        }

        public void ShowBanner(RoundResolved round)
        {
            string name = Context.BotName(_botId);
            string title;
            string text;
            string look = "";

            if (round.ShieldBlockedBy == Side.Player)
            {
                title = Text.Get("banner.shield.title");
                text = Text.Format("banner.shield.text", name);
            }
            else if (round.Steps >= 2)
            {
                title = Text.Get("banner.double.title");
                text = Text.Format("banner.double.text", name);
                look = "banner--win";
            }
            else if (round.Steps > 0)
            {
                title = Text.Get("banner.pull.title");
                text = Text.Format("banner.pull.text", name);
                look = "banner--win";
            }
            else if (round.Steps < 0)
            {
                title = Text.Get("banner.pulled.title");
                text = Text.Format("banner.pulled.text", name);
                look = "banner--lose";
            }
            else
            {
                title = Text.Get("banner.tie.title");
                text = Text.Get(round.Outcome == RoundOutcome.BothCorrect ? "banner.both.text" : "banner.none.text");
            }

            Label("banner-title").text = title;
            Label("banner-text").text = text;
            _banner.RemoveFromClassList("banner--win");
            _banner.RemoveFromClassList("banner--lose");
            if (look != "")
            {
                _banner.AddToClassList(look);
            }

            _banner.RemoveFromClassList("hidden");
            _banner.schedule.Execute(() => _banner.AddToClassList("banner--on"));
        }

        public void ShowReported()
        {
            var report = Find<Button>("report");
            report.SetEnabled(false);
            Label("report-label").text = Text.Get("report.done");
        }

        public void HideReportButton()
        {
            Find<Button>("report").AddToClassList("hidden");
        }

        private void ShowReportButton()
        {
            var report = Find<Button>("report");
            report.RemoveFromClassList("hidden");
            report.SetEnabled(true);
            Label("report-label").text = Text.Get("report.button");
        }

        public void HideBanner()
        {
            _banner.RemoveFromClassList("banner--on");
            _banner.AddToClassList("hidden");
        }

        protected override void OnRefresh()
        {
            if (_botId != "")
            {
                Label("bot-name").text = Context.BotName(_botId);
                ApplyJokers();
            }
        }

        private string VerdictWord(int choice, int correct)
        {
            if (choice < 0)
            {
                return Text.Get("match.timeout");
            }

            return Text.Get(choice == correct ? "match.correct" : "match.wrong");
        }

        private string OptionText(int choice)
        {
            return choice >= 0 && _optionValues != null ? Text.Content(_optionValues[choice]) : "—";
        }

        private void ShowStatus(string text, string look)
        {
            Label("status-text").text = text;
            _status.RemoveFromClassList("status--answered");
            _status.RemoveFromClassList("status--summary");
            if (look != "")
            {
                _status.AddToClassList(look);
            }
        }

        private Button JokerButton(JokerType joker)
        {
            switch (joker)
            {
                case JokerType.FiftyFifty:
                    return Find<Button>("joker-half");
                case JokerType.Shield:
                    return Find<Button>("joker-shield");
                default:
                    return Find<Button>("joker-double");
            }
        }

        private void ApplyJokers()
        {
            ApplyJoker(JokerType.FiftyFifty, "joker.half.hint");
            ApplyJoker(JokerType.Shield, "joker.shield.hint");
            ApplyJoker(JokerType.Double, "joker.double.hint");
        }

        private void ApplyJoker(JokerType joker, string hintKey)
        {
            if (!_jokers.TryGetValue(joker, out JokerLook look))
            {
                return;
            }

            Button button = JokerButton(joker);
            button.EnableInClassList("joker--armed", look == JokerLook.Armed);
            button.EnableInClassList("joker--used", look == JokerLook.Used);
            button.SetEnabled(look == JokerLook.Ready);

            string hint = look == JokerLook.Ready ? hintKey : look == JokerLook.Armed ? "joker.armed" : "joker.used";
            button.Q<Label>(className: "joker-hint").text = Text.Get(hint);
        }
    }
}
