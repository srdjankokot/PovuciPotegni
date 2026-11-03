using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PovuciPotegni.Core.Tests
{
    public class QuestionAuditTests
    {
        private static readonly (string Category, int Count)[] GddMix =
        {
            ("istorija", 75), ("geografija", 75), ("sport", 75), ("film_tv", 60),
            ("muzika", 60), ("nauka", 55), ("umetnost_knjizevnost", 50), ("opste", 50),
        };

        private static string Entry(string id, string category, int difficulty, string verifiedBy = "urednik", string text = null)
        {
            return $"{{\"id\":\"{id}\",\"category\":\"{category}\",\"difficulty\":{difficulty},\"text\":\"{text ?? "Pitanje " + id}\","
                + $"\"options\":[\"A\",\"B\",\"C\"],\"answer\":0,\"verifiedBy\":\"{verifiedBy}\"}}";
        }

        private static string FullBank()
        {
            var entries = new List<string>();
            int number = 0;
            foreach ((string category, int count) in GddMix)
            {
                for (int i = 0; i < count; i++)
                {
                    entries.Add(Entry($"q{number}", category, number % 3 + 1));
                    number++;
                }
            }

            return "[" + string.Join(",", entries) + "]";
        }

        [Test]
        public void FullVerifiedBank_IsReadyForRelease()
        {
            QuestionAudit audit = QuestionAudit.Check(FullBank());

            Assert.That(audit.Total, Is.EqualTo(500));
            Assert.That(audit.Verified, Is.EqualTo(500));
            Assert.That(audit.Warnings, Is.Empty);
            Assert.That(audit.IsReadyForRelease, Is.True);
            Assert.That(audit.ToReport(), Does.Contain("Spremno za izdanje"));
        }

        [Test]
        public void SampleBank_LoadsButBlocksRelease()
        {
            QuestionAudit audit = QuestionAudit.Check("[" + Entry("q1", "sport", 1, "") + "]");

            Assert.That(audit.CanLoad, Is.True);
            Assert.That(audit.IsReadyForRelease, Is.False);
            Assert.That(audit.Warnings.Any(warning => warning.Contains("500")), Is.True);
            Assert.That(audit.Warnings.Any(warning => warning.Contains("nije provereno")), Is.True);
            Assert.That(audit.Warnings.Any(warning => warning.Contains("težine 3")), Is.True);
        }

        [Test]
        public void Audit_ReportsEveryBadEntry_NotJustTheFirst()
        {
            string json = "[" + Entry("q1", "kuvanje", 1) + "," + Entry("q2", "sport", 9) + "," + Entry("q3", "sport", 1) + "]";

            QuestionAudit audit = QuestionAudit.Check(json);

            Assert.That(audit.Errors, Has.Count.EqualTo(2));
            Assert.That(audit.Total, Is.EqualTo(1));
            Assert.That(audit.CanLoad, Is.False);
        }

        [Test]
        public void DuplicateId_IsError_AndSameText_IsWarning()
        {
            string json = "[" + Entry("q1", "sport", 1, text: "Isto?") + "," + Entry("q1", "sport", 1) + "," + Entry("q2", "sport", 1, text: "isto?") + "]";

            QuestionAudit audit = QuestionAudit.Check(json);

            Assert.That(audit.Errors.Single(), Does.Contain("q1"));
            Assert.That(audit.Warnings.Any(warning => warning.Contains("Isti tekst")), Is.True);
        }

        [Test]
        public void WrongCategoryShare_BlocksRelease()
        {
            var builder = new StringBuilder("[");
            for (int i = 0; i < 500; i++)
            {
                builder.Append(i == 0 ? "" : ",").Append(Entry($"q{i}", "sport", i % 3 + 1));
            }

            QuestionAudit audit = QuestionAudit.Check(builder.Append(']').ToString());

            Assert.That(audit.CanLoad, Is.True);
            Assert.That(audit.IsReadyForRelease, Is.False);
            Assert.That(audit.Warnings.Any(warning => warning.Contains("Kategorija sport")), Is.True);
        }

        [Test]
        public void BrokenJson_IsError()
        {
            QuestionAudit audit = QuestionAudit.Check("nije json");

            Assert.That(audit.CanLoad, Is.False);
            Assert.That(audit.ToReport(), Does.Contain("NIJE ispravan"));
        }
    }

    public class MatchAnalyticsTests
    {
        private sealed class RecordingAnalytics : IAnalytics
        {
            public List<(string Name, IReadOnlyDictionary<string, object> Parameters)> Events { get; } =
                new List<(string, IReadOnlyDictionary<string, object>)>();

            public void Track(string eventName, IReadOnlyDictionary<string, object> parameters)
            {
                Events.Add((eventName, parameters));
            }

            public IReadOnlyDictionary<string, object> Last(string name)
            {
                return Events.Last(entry => entry.Name == name).Parameters;
            }
        }

        private static BotMatchService CreateService(FakeClock clock)
        {
            var questions = Enumerable.Range(0, 20).Select(i => TestQuestions.Create("q" + i, i % 3 + 1)).ToArray();
            return new BotMatchService(new GameConfig(), new QuestionPicker(new QuestionBank(questions), new InMemoryStatsStore()), clock);
        }

        [Test]
        public void WholeMatch_SendsDocumentedEvents()
        {
            var clock = new FakeClock();
            var analytics = new RecordingAnalytics();
            BotMatchService service = CreateService(clock);
            var bot = new BotProfile("test", 1, 1, 1, 0, "", "");

            using (var tracker = new MatchAnalytics(analytics, service, bot.Id))
            {
                tracker.TrackStart();
                service.Start(new MatchSetup(bot, 1));
                service.UseJoker(JokerType.FiftyFifty);

                for (int i = 0; i < 3; i++)
                {
                    service.SubmitAnswer(-1);
                    clock.NowSeconds += 2;
                    service.Tick(clock.NowSeconds);
                    service.Continue();
                }
            }

            Assert.That(analytics.Last("match_start")["bot"], Is.EqualTo("test"));
            Assert.That(analytics.Last("joker_used")["joker"], Is.EqualTo("FiftyFifty"));
            Assert.That(analytics.Events.Count(entry => entry.Name == "question_answered"), Is.EqualTo(3));
            Assert.That(analytics.Last("question_answered")["correct"], Is.EqualTo(false));
            Assert.That(analytics.Last("match_end")["outcome"], Is.EqualTo("loss"));
            Assert.That(analytics.Last("match_end")["questions"], Is.EqualTo(3));
            Assert.That(analytics.Last("match_end")["reason"], Is.EqualTo("Edge"));
        }

        [Test]
        public void AfterDispose_NothingIsSent()
        {
            var clock = new FakeClock();
            var analytics = new RecordingAnalytics();
            BotMatchService service = CreateService(clock);

            new MatchAnalytics(analytics, service, "test").Dispose();
            service.Start(new MatchSetup(new BotProfile("test", 1, 1, 1, 0, "", ""), 1));
            service.UseJoker(JokerType.Shield);

            Assert.That(analytics.Events, Is.Empty);
        }
    }
}
