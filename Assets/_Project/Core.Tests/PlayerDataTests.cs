using NUnit.Framework;

namespace PovuciPotegni.Core.Tests
{
    public class PlayerDataTests
    {
        private readonly GameConfig _config = new GameConfig();

        private static PlayerData WithWins(int wins)
        {
            var data = new PlayerData();
            for (int i = 0; i < wins; i++)
            {
                data.RecordMatch("segrt", true);
            }

            return data;
        }

        [Test]
        public void Match_UpdatesCountsAndStreak()
        {
            var data = new PlayerData();

            data.RecordMatch("segrt", true);
            data.RecordMatch("segrt", true);
            data.RecordMatch("profesor", false);
            data.RecordMatch("profesor", true);

            Assert.That(data.Matches, Is.EqualTo(4));
            Assert.That(data.Wins, Is.EqualTo(3));
            Assert.That(data.WinStreak, Is.EqualTo(1));
            Assert.That(data.BestWinStreak, Is.EqualTo(2));
            Assert.That(data.WinsAgainst("segrt"), Is.EqualTo(2));
            Assert.That(data.WinsAgainst("profesor"), Is.EqualTo(1));
        }

        [Test]
        public void Answers_GiveSharePerCategory()
        {
            var data = new PlayerData();

            data.RecordAnswer("sport", true);
            data.RecordAnswer("sport", false);
            data.RecordAnswer("muzika", true);

            Assert.That(data.CorrectShare, Is.EqualTo(2 / 3.0).Within(1e-9));
            Assert.That(data.CorrectShareIn("sport"), Is.EqualTo(0.5));
            Assert.That(data.AnswersIn("muzika"), Is.EqualTo(1));
            Assert.That(data.StrongestCategory(), Is.EqualTo("muzika"));
        }

        [Test]
        public void NoAnswers_HaveNoStrongestCategoryAndZeroShare()
        {
            var data = new PlayerData();

            Assert.That(data.StrongestCategory(), Is.Null);
            Assert.That(data.CorrectShare, Is.EqualTo(0));
            Assert.That(data.CorrectShareIn("sport"), Is.EqualTo(0));
        }

        [TestCase(0, 1, "profesor")]
        [TestCase(2, 1, "profesor")]
        [TestCase(3, 2, "lisac")]
        [TestCase(10, 3, "majstor")]
        public void Unlocking_FollowsGddTable(int wins, int unlocked, string nextLocked)
        {
            PlayerData data = WithWins(wins);

            Assert.That(data.UnlockedCount(_config), Is.EqualTo(unlocked));
            Assert.That(data.NextLockedBot(_config).Id, Is.EqualTo(nextLocked));
        }

        [Test]
        public void AllWins_UnlockEveryBot()
        {
            PlayerData data = WithWins(25);

            Assert.That(data.UnlockedCount(_config), Is.EqualTo(4));
            Assert.That(data.NextLockedBot(_config), Is.Null);
        }

        [Test]
        public void SelectedBot_FallsBackWhenLockedOrUnknown()
        {
            var data = new PlayerData { SelectedBotId = "majstor" };
            Assert.That(data.SelectedBot(_config).Id, Is.EqualTo("segrt"));

            data.SelectedBotId = "nepostoji";
            Assert.That(data.SelectedBot(_config).Id, Is.EqualTo("segrt"));

            PlayerData veteran = WithWins(3);
            veteran.SelectedBotId = "profesor";
            Assert.That(veteran.SelectedBot(_config).Id, Is.EqualTo("profesor"));
        }

        [Test]
        public void Json_RoundTripsEverything()
        {
            PlayerData data = WithWins(4);
            data.RecordMatch("lisac", false);
            data.RecordAnswer("sport", true);
            data.RecordAnswer("sport", false);
            data.SeenQuestions.Add("q1");
            data.ReportedQuestions.Add("q9");
            data.SelectedBotId = "profesor";
            data.Script = TextScript.Cyrillic;
            data.SoundOn = false;
            data.HapticsOn = false;

            PlayerData copy = PlayerData.FromJson(data.ToJson());

            Assert.That(copy.Matches, Is.EqualTo(5));
            Assert.That(copy.Wins, Is.EqualTo(4));
            Assert.That(copy.WinStreak, Is.EqualTo(0));
            Assert.That(copy.BestWinStreak, Is.EqualTo(4));
            Assert.That(copy.WinsAgainst("segrt"), Is.EqualTo(4));
            Assert.That(copy.CorrectShareIn("sport"), Is.EqualTo(0.5));
            Assert.That(copy.SeenQuestions, Does.Contain("q1"));
            Assert.That(copy.ReportedQuestions, Does.Contain("q9"));
            Assert.That(copy.SelectedBotId, Is.EqualTo("profesor"));
            Assert.That(copy.Script, Is.EqualTo(TextScript.Cyrillic));
            Assert.That(copy.SoundOn, Is.False);
            Assert.That(copy.HapticsOn, Is.False);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("nije json {")]
        public void DamagedSave_GivesFreshStart(string text)
        {
            PlayerData data = PlayerData.FromJson(text);

            Assert.That(data.Matches, Is.EqualTo(0));
            Assert.That(data.SoundOn, Is.True);
            Assert.That(data.Script, Is.EqualTo(TextScript.Latin));
        }
    }

    public class LocalizationTests
    {
        [Test]
        public void CyrillicScript_TransliteratesText()
        {
            var latin = new Localization(TextScript.Latin);
            var cyrillic = new Localization(TextScript.Cyrillic);

            Assert.That(latin.Get("home.play"), Is.EqualTo("Igraj"));
            Assert.That(cyrillic.Get("home.play"), Is.EqualTo("Играј"));
            Assert.That(cyrillic.Content("Koja reka?"), Is.EqualTo("Која река?"));
        }

        [Test]
        public void Format_FillsArguments()
        {
            var text = new Localization(TextScript.Latin);

            Assert.That(text.Format("match.toWin", 3), Is.EqualTo("3 do pobede"));
            Assert.That(text.Format("opponents.play", text.Get("bot.profesor.genitive")), Is.EqualTo("Igraj protiv Profesora"));
        }

        [TestCase(1, "pobeda")]
        [TestCase(2, "pobede")]
        [TestCase(4, "pobede")]
        [TestCase(5, "pobeda")]
        [TestCase(10, "pobeda")]
        [TestCase(12, "pobeda")]
        [TestCase(22, "pobede")]
        [TestCase(25, "pobeda")]
        public void WinsWord_FollowsSerbianPlural(int count, string expected)
        {
            Assert.That(new Localization(TextScript.Latin).WinsWord(count), Is.EqualTo(expected));
        }

        [Test]
        public void MissingKey_ShowsItself()
        {
            Assert.That(new Localization(TextScript.Latin).Get("nema.takvog"), Is.EqualTo("nema.takvog"));
        }

        [Test]
        public void EveryBotAndCategory_HasText()
        {
            foreach (BotProfile bot in new GameConfig().Bots)
            {
                foreach (string field in new[] { "name", "genitive", "level", "traits" })
                {
                    Assert.That(Localization.HasKey($"bot.{bot.Id}.{field}"), Is.True, $"{bot.Id}.{field}");
                }
            }

            foreach (string category in QuestionCategories.All)
            {
                Assert.That(Localization.HasKey($"category.{category}"), Is.True, category);
            }
        }
    }
}
