using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PovuciPotegni.Core
{
    /// <summary>
    /// Questions loaded from the JSON format in docs/GDD.md. A malformed entry fails the whole load,
    /// because a wrong question must never reach a player. Parsed by hand so IL2CPP stripping cannot affect it.
    /// </summary>
    public sealed class QuestionBank
    {
        private const int OptionCount = 3;

        public IReadOnlyList<Question> Questions { get; }

        public QuestionBank(IReadOnlyList<Question> questions)
        {
            Questions = questions;
        }

        public static QuestionBank FromJson(string json, bool allowUnverified)
        {
            var questions = new List<Question>();
            var ids = new HashSet<string>();

            foreach (JToken item in JArray.Parse(json))
            {
                Question question = ParseQuestion(item);
                if (!ids.Add(question.Id))
                {
                    throw new FormatException($"Duplicate question id '{question.Id}'.");
                }

                if (allowUnverified || question.IsVerified)
                {
                    questions.Add(question);
                }
            }

            return new QuestionBank(questions);
        }

        internal static Question ParseQuestion(JToken item)
        {
            string id = (string)item["id"];
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new FormatException("A question has no id.");
            }

            string category = (string)item["category"];
            if (category == null || !QuestionCategories.All.Contains(category))
            {
                throw new FormatException($"Question '{id}' has unknown category '{category}'.");
            }

            int? difficulty = (int?)item["difficulty"];
            if (difficulty == null || difficulty < MatchRules.EasyDifficulty || difficulty > MatchRules.HardDifficulty)
            {
                throw new FormatException($"Question '{id}' has invalid difficulty '{difficulty}'.");
            }

            string text = (string)item["text"];
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException($"Question '{id}' has no text.");
            }

            var options = item["options"] as JArray;
            if (options == null || options.Count != OptionCount)
            {
                throw new FormatException($"Question '{id}' must have {OptionCount} options.");
            }

            List<string> optionTexts = options.Select(option => (string)option).ToList();
            if (optionTexts.Any(string.IsNullOrWhiteSpace) || optionTexts.Distinct().Count() != OptionCount)
            {
                throw new FormatException($"Question '{id}' has an empty or repeated option.");
            }

            int? answer = (int?)item["answer"];
            if (answer == null || answer < 0 || answer >= OptionCount)
            {
                throw new FormatException($"Question '{id}' has invalid answer '{answer}'.");
            }

            return new Question(id, category, difficulty.Value, text, optionTexts, answer.Value, (string)item["verifiedBy"]);
        }
    }
}
