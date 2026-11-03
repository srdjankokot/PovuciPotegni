using System.Collections.Generic;

namespace PovuciPotegni.Core.Tests
{
    internal sealed class FakeClock : IClock
    {
        public double NowSeconds { get; set; }
    }

    internal sealed class InMemoryStatsStore : IStatsStore
    {
        public PlayerData Data { get; } = new PlayerData();

        public int SeenCount => Data.SeenQuestions.Count;

        public void Save()
        {
        }

        public bool IsQuestionSeen(string questionId)
        {
            return Data.SeenQuestions.Contains(questionId);
        }

        public void MarkQuestionSeen(string questionId)
        {
            Data.SeenQuestions.Add(questionId);
        }

        public void ClearSeenQuestions()
        {
            Data.SeenQuestions.Clear();
        }
    }

    internal static class TestQuestions
    {
        public static Question Create(string id, int difficulty = 1, string category = QuestionCategories.Sport, int answer = 0)
        {
            return new Question(id, category, difficulty, "Pitanje " + id, new[] { "A", "B", "C" }, answer, "urednik");
        }
    }
}
