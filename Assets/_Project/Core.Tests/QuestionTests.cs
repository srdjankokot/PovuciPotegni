using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace PovuciPotegni.Core.Tests
{
    public class QuestionBankTests
    {
        private const string ValidTemplate =
            "{{\"id\":\"{0}\",\"lang\":\"sr-Latn\",\"category\":\"{1}\",\"difficulty\":{2},\"text\":\"Pitanje?\"," +
            "\"options\":{3},\"answer\":{4},\"source\":\"\",\"verifiedBy\":\"{5}\",\"verifiedAt\":\"\"," +
            "\"stats\":{{\"shown\":0,\"correct\":0}}}}";

        private static string Entry(
            string id = "q1",
            string category = "sport",
            int difficulty = 1,
            string options = "[\"A\",\"B\",\"C\"]",
            int answer = 0,
            string verifiedBy = "urednik")
        {
            return string.Format(ValidTemplate, id, category, difficulty, options, answer, verifiedBy);
        }

        [Test]
        public void ReadsEveryField()
        {
            QuestionBank bank = QuestionBank.FromJson("[" + Entry(difficulty: 2, answer: 2) + "]", false);

            Question question = bank.Questions.Single();
            Assert.That(question.Id, Is.EqualTo("q1"));
            Assert.That(question.Category, Is.EqualTo("sport"));
            Assert.That(question.Difficulty, Is.EqualTo(2));
            Assert.That(question.Text, Is.EqualTo("Pitanje?"));
            Assert.That(question.Options, Is.EqualTo(new[] { "A", "B", "C" }));
            Assert.That(question.Answer, Is.EqualTo(2));
            Assert.That(question.IsVerified, Is.True);
        }

        [Test]
        public void SampleFileLoadsOnlyWhenUnverifiedAllowed()
        {
            string json = File.ReadAllText(Path.Combine("reference", "pitanja-primer.json"));

            Assert.That(QuestionBank.FromJson(json, true).Questions, Has.Count.EqualTo(12));
            Assert.That(QuestionBank.FromJson(json, false).Questions, Is.Empty);
        }

        [Test]
        public void UnverifiedQuestion_IsFilteredByDefault()
        {
            string json = "[" + Entry("q1") + "," + Entry("q2", verifiedBy: "") + "]";

            Assert.That(QuestionBank.FromJson(json, false).Questions.Single().Id, Is.EqualTo("q1"));
            Assert.That(QuestionBank.FromJson(json, true).Questions, Has.Count.EqualTo(2));
        }

        private static IEnumerable<TestCaseData> InvalidEntries()
        {
            yield return new TestCaseData(Entry(id: "")).SetName("EmptyId");
            yield return new TestCaseData(Entry(category: "kuvanje")).SetName("UnknownCategory");
            yield return new TestCaseData(Entry(difficulty: 4)).SetName("DifficultyTooHigh");
            yield return new TestCaseData(Entry(options: "[\"A\",\"B\"]")).SetName("TwoOptions");
            yield return new TestCaseData(Entry(options: "[\"A\",\"A\",\"C\"]")).SetName("RepeatedOption");
            yield return new TestCaseData(Entry(answer: 3)).SetName("AnswerOutOfRange");
            yield return new TestCaseData(Entry() + "," + Entry()).SetName("DuplicateId");
        }

        [TestCaseSource(nameof(InvalidEntries))]
        public void InvalidEntry_FailsLoad(string entries)
        {
            Assert.That(() => QuestionBank.FromJson("[" + entries + "]", true), Throws.InstanceOf<FormatException>());
        }
    }

    public class QuestionPickerTests
    {
        private static QuestionPicker Picker(IStatsStore stats, params Question[] questions)
        {
            return new QuestionPicker(new QuestionBank(questions), stats);
        }

        [Test]
        public void NoRepeatsUntilBankIsExhausted()
        {
            Question[] questions = Enumerable.Range(0, 500)
                .Select(i => TestQuestions.Create("q" + i, i % 3 + 1))
                .ToArray();
            QuestionPicker picker = Picker(new InMemoryStatsStore(), questions);
            var random = new SeededRandom(5);
            var seen = new HashSet<string>();

            for (int i = 0; i < 500; i++)
            {
                Assert.That(seen.Add(picker.Pick(i % 3 + 1, random).Id), Is.True, "Question repeated too early.");
            }

            Assert.That(seen, Has.Count.EqualTo(500));
        }

        [Test]
        public void AfterExhaustion_HistoryStartsOver()
        {
            var stats = new InMemoryStatsStore();
            QuestionPicker picker = Picker(stats, TestQuestions.Create("q1"), TestQuestions.Create("q2"));
            var random = new SeededRandom(5);

            picker.Pick(1, random);
            picker.Pick(1, random);
            Assert.That(stats.SeenCount, Is.EqualTo(2));

            picker.Pick(1, random);
            Assert.That(stats.SeenCount, Is.EqualTo(1));
        }

        [Test]
        public void HistorySurvivesNewPicker()
        {
            var stats = new InMemoryStatsStore();
            Question[] questions = { TestQuestions.Create("q1"), TestQuestions.Create("q2") };

            string first = Picker(stats, questions).Pick(1, new SeededRandom(1)).Id;
            string second = Picker(stats, questions).Pick(1, new SeededRandom(1)).Id;

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [Test]
        public void PicksWantedDifficulty()
        {
            QuestionPicker picker = Picker(
                new InMemoryStatsStore(),
                TestQuestions.Create("easy", 1),
                TestQuestions.Create("medium", 2),
                TestQuestions.Create("hard", 3));

            Assert.That(picker.Pick(2, new SeededRandom(1)).Id, Is.EqualTo("medium"));
        }

        [TestCase(1, "medium")]
        [TestCase(3, "medium")]
        public void FallsBackToClosestDifficulty(int wanted, string expected)
        {
            QuestionPicker picker = Picker(
                new InMemoryStatsStore(),
                TestQuestions.Create("medium", 2),
                TestQuestions.Create(wanted == 1 ? "hard" : "easy", wanted == 1 ? 3 : 1));

            Assert.That(picker.Pick(wanted, new SeededRandom(1)).Id, Is.EqualTo(expected));
        }

        [Test]
        public void RespectsCategoryFilter()
        {
            QuestionPicker picker = Picker(
                new InMemoryStatsStore(),
                TestQuestions.Create("sport", 1, QuestionCategories.Sport),
                TestQuestions.Create("music", 1, QuestionCategories.Music));

            Question picked = picker.Pick(1, new SeededRandom(1), new[] { QuestionCategories.Music });

            Assert.That(picked.Id, Is.EqualTo("music"));
        }

        [Test]
        public void ReportedQuestion_IsNotPickedAgain()
        {
            var stats = new InMemoryStatsStore();
            stats.Data.ReportedQuestions.Add("q1");
            QuestionPicker picker = Picker(stats, TestQuestions.Create("q1"), TestQuestions.Create("q2"));
            var random = new SeededRandom(5);

            for (int i = 0; i < 10; i++)
            {
                Assert.That(picker.Pick(1, random).Id, Is.EqualTo("q2"));
            }
        }

        [Test]
        public void EveryQuestionReported_StillGivesAQuestion()
        {
            var stats = new InMemoryStatsStore();
            stats.Data.ReportedQuestions.Add("q1");

            Assert.That(Picker(stats, TestQuestions.Create("q1")).Pick(1, new SeededRandom(5)).Id, Is.EqualTo("q1"));
        }

        [Test]
        public void EmptyBank_Throws()
        {
            QuestionPicker picker = Picker(new InMemoryStatsStore());

            Assert.That(() => picker.Pick(1, new SeededRandom(1)), Throws.InvalidOperationException);
        }
    }
}
