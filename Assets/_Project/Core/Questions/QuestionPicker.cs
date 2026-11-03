using System;
using System.Collections.Generic;
using System.Linq;

namespace PovuciPotegni.Core
{
    /// <summary>
    /// Picks unseen questions, closest to the wanted difficulty. When every allowed question was seen,
    /// the history starts over. Category shares follow the shares in the bank.
    /// </summary>
    public sealed class QuestionPicker
    {
        private readonly QuestionBank _bank;
        private readonly IStatsStore _stats;

        public QuestionPicker(QuestionBank bank, IStatsStore stats)
        {
            _bank = bank;
            _stats = stats;
        }

        /// <param name="categories">Allowed category keys; null or empty allows all.</param>
        public Question Pick(int difficulty, IRandom random, IReadOnlyCollection<string> categories = null)
        {
            List<Question> inCategories = _bank.Questions
                .Where(question => categories == null || categories.Count == 0 || categories.Contains(question.Category))
                .ToList();

            if (inCategories.Count == 0)
            {
                throw new InvalidOperationException("No questions are available for this match.");
            }

            // A reported question is not asked again, unless the player has reported every last one.
            List<Question> allowed = inCategories.Where(question => !_stats.Data.ReportedQuestions.Contains(question.Id)).ToList();
            if (allowed.Count == 0)
            {
                allowed = inCategories;
            }

            List<Question> unseen = allowed.Where(question => !_stats.IsQuestionSeen(question.Id)).ToList();
            if (unseen.Count == 0)
            {
                _stats.ClearSeenQuestions();
                unseen = allowed;
            }

            int closestDifficulty = unseen
                .Select(question => question.Difficulty)
                .Distinct()
                .OrderBy(candidate => Math.Abs(candidate - difficulty))
                .ThenByDescending(candidate => candidate)
                .First();

            List<Question> candidates = unseen.Where(question => question.Difficulty == closestDifficulty).ToList();
            Question picked = candidates[random.Next(candidates.Count)];
            _stats.MarkQuestionSeen(picked.Id);
            return picked;
        }
    }
}
