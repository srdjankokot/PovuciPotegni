using System.Collections.Generic;

namespace PovuciPotegni.Core
{
    public sealed class Question
    {
        public string Id { get; }
        public string Category { get; }
        public int Difficulty { get; }
        public string Text { get; }
        public IReadOnlyList<string> Options { get; }

        /// <summary>Index into <see cref="Options"/> before shuffling.</summary>
        public int Answer { get; }

        public string VerifiedBy { get; }

        public bool IsVerified => !string.IsNullOrWhiteSpace(VerifiedBy);

        public Question(
            string id,
            string category,
            int difficulty,
            string text,
            IReadOnlyList<string> options,
            int answer,
            string verifiedBy)
        {
            Id = id;
            Category = category;
            Difficulty = difficulty;
            Text = text;
            Options = options;
            Answer = answer;
            VerifiedBy = verifiedBy;
        }
    }
}
