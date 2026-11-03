namespace PovuciPotegni.Core
{
    /// <summary>Persistent player data: statistics, settings and the question history.</summary>
    public interface IStatsStore
    {
        PlayerData Data { get; }

        /// <summary>Writes the data to storage. The question history is saved with it, not on every question.</summary>
        void Save();

        bool IsQuestionSeen(string questionId);
        void MarkQuestionSeen(string questionId);
        void ClearSeenQuestions();
    }
}
