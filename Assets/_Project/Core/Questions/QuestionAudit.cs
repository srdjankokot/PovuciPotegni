using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PovuciPotegni.Core
{
    /// <summary>
    /// Checks a question file before it enters the game: format, duplicates, and the shares in docs/GDD.md.
    /// Unlike loading, it goes through the whole file, so the editor sees every problem at once.
    /// </summary>
    public sealed class QuestionAudit
    {
        public const int TargetQuestions = 500;
        private const double ShareTolerance = 0.03;

        private static readonly Dictionary<string, double> TargetShares = new Dictionary<string, double>
        {
            { QuestionCategories.History, 0.15 },
            { QuestionCategories.Geography, 0.15 },
            { QuestionCategories.Sport, 0.15 },
            { QuestionCategories.FilmTv, 0.12 },
            { QuestionCategories.Music, 0.12 },
            { QuestionCategories.Science, 0.11 },
            { QuestionCategories.ArtLiterature, 0.10 },
            { QuestionCategories.General, 0.10 },
        };

        /// <summary>Problems that stop the game from loading the file.</summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>Problems that allow development but block a release.</summary>
        public List<string> Warnings { get; } = new List<string>();

        public int Total { get; private set; }
        public int Verified { get; private set; }
        public Dictionary<string, int> ByCategory { get; } = new Dictionary<string, int>();
        public Dictionary<int, int> ByDifficulty { get; } = new Dictionary<int, int>();

        public bool CanLoad => Errors.Count == 0;
        public bool IsReadyForRelease => CanLoad && Warnings.Count == 0;

        public static QuestionAudit Check(string json)
        {
            var audit = new QuestionAudit();
            JArray items;

            try
            {
                items = JArray.Parse(json);
            }
            catch (JsonException exception)
            {
                audit.Errors.Add($"Fajl nije ispravan JSON niz: {exception.Message}");
                return audit;
            }

            audit.Read(items);
            audit.CheckTotals();
            return audit;
        }

        public string ToReport()
        {
            var report = new StringBuilder();
            report.AppendLine($"Pitanja: {Total} (cilj {TargetQuestions}), proverenih: {Verified}");

            report.AppendLine("Po kategoriji:");
            foreach (KeyValuePair<string, double> target in TargetShares)
            {
                int count = Count(ByCategory, target.Key);
                report.AppendLine($"  {target.Key}: {count} ({Share(count):P1}, cilj {target.Value:P0})");
            }

            report.AppendLine("Po težini:");
            for (int difficulty = MatchRules.EasyDifficulty; difficulty <= MatchRules.HardDifficulty; difficulty++)
            {
                report.AppendLine($"  {difficulty}: {Count(ByDifficulty, difficulty)}");
            }

            AppendList(report, "Greške (igra ne može da učita fajl)", Errors);
            AppendList(report, "Upozorenja (blokiraju izdanje)", Warnings);
            report.AppendLine(IsReadyForRelease ? "Spremno za izdanje." : CanLoad ? "Može u razvoju, nije spremno za izdanje." : "Fajl NIJE ispravan.");
            return report.ToString();
        }

        private void Read(JArray items)
        {
            var ids = new HashSet<string>();
            var texts = new Dictionary<string, string>();
            int position = 0;

            foreach (JToken item in items)
            {
                position++;
                Question question;

                try
                {
                    question = QuestionBank.ParseQuestion(item);
                }
                catch (Exception exception) when (exception is FormatException || exception is ArgumentException || exception is InvalidCastException || exception is OverflowException)
                {
                    Errors.Add($"Zapis {position}: {exception.Message}");
                    continue;
                }

                if (!ids.Add(question.Id))
                {
                    Errors.Add($"Ponovljen id '{question.Id}'.");
                    continue;
                }

                string normalized = question.Text.Trim().ToLowerInvariant();
                if (texts.TryGetValue(normalized, out string firstId))
                {
                    Warnings.Add($"Isti tekst pitanja: '{firstId}' i '{question.Id}'.");
                }
                else
                {
                    texts[normalized] = question.Id;
                }

                Total++;
                Verified += question.IsVerified ? 1 : 0;
                ByCategory[question.Category] = Count(ByCategory, question.Category) + 1;
                ByDifficulty[question.Difficulty] = Count(ByDifficulty, question.Difficulty) + 1;
            }
        }

        private void CheckTotals()
        {
            if (Total < TargetQuestions)
            {
                Warnings.Add($"Ima {Total} pitanja, a treba {TargetQuestions}.");
            }

            if (Verified < Total)
            {
                Warnings.Add($"{Total - Verified} pitanja nije provereno (prazan verifiedBy); igra ih u izdanju ne prikazuje.");
            }

            foreach (KeyValuePair<string, double> target in TargetShares)
            {
                double share = Share(Count(ByCategory, target.Key));
                if (Math.Abs(share - target.Value) > ShareTolerance)
                {
                    Warnings.Add($"Kategorija {target.Key}: {share:P1}, a cilj je {target.Value:P0}.");
                }
            }

            for (int difficulty = MatchRules.EasyDifficulty; difficulty <= MatchRules.HardDifficulty; difficulty++)
            {
                if (Count(ByDifficulty, difficulty) == 0)
                {
                    Warnings.Add($"Nema pitanja težine {difficulty}.");
                }
            }
        }

        private double Share(int count)
        {
            return Total == 0 ? 0 : count / (double)Total;
        }

        private static int Count<TKey>(Dictionary<TKey, int> counts, TKey key)
        {
            return counts.TryGetValue(key, out int count) ? count : 0;
        }

        private static void AppendList(StringBuilder report, string title, List<string> items)
        {
            if (items.Count > 0)
            {
                report.AppendLine($"{title}:");
                foreach (string item in items)
                {
                    report.AppendLine($"  - {item}");
                }
            }
        }
    }
}
