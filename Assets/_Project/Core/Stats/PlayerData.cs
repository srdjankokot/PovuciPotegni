using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace PovuciPotegni.Core
{
    public enum TextScript
    {
        Latin,
        Cyrillic,
    }

    /// <summary>
    /// Everything saved on the device: statistics, settings and the question history.
    /// Serialized by hand so IL2CPP stripping cannot affect it.
    /// </summary>
    public sealed class PlayerData
    {
        public int Matches { get; private set; }
        public int Wins { get; private set; }
        public int WinStreak { get; private set; }
        public int BestWinStreak { get; private set; }
        public int AnswersGiven { get; private set; }
        public int AnswersCorrect { get; private set; }

        public string SelectedBotId { get; set; } = "";
        public TextScript Script { get; set; } = TextScript.Latin;
        public bool SoundOn { get; set; } = true;
        public bool HapticsOn { get; set; } = true;

        public HashSet<string> SeenQuestions { get; } = new HashSet<string>();

        /// <summary>Questions the player reported as wrong. They are not asked again on this device.</summary>
        public HashSet<string> ReportedQuestions { get; } = new HashSet<string>();

        private readonly Dictionary<string, int> _winsByBot = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _answersByCategory = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _correctByCategory = new Dictionary<string, int>();

        public double CorrectShare => AnswersGiven == 0 ? 0 : AnswersCorrect / (double)AnswersGiven;

        public void RecordAnswer(string category, bool correct)
        {
            AnswersGiven++;
            AnswersCorrect += correct ? 1 : 0;
            Increase(_answersByCategory, category);
            if (correct)
            {
                Increase(_correctByCategory, category);
            }
        }

        public void RecordMatch(string botId, bool won)
        {
            Matches++;
            if (won)
            {
                Wins++;
                WinStreak++;
                BestWinStreak = System.Math.Max(BestWinStreak, WinStreak);
                Increase(_winsByBot, botId);
            }
            else
            {
                WinStreak = 0;
            }
        }

        public int WinsAgainst(string botId)
        {
            return _winsByBot.TryGetValue(botId, out int wins) ? wins : 0;
        }

        public int AnswersIn(string category)
        {
            return _answersByCategory.TryGetValue(category, out int answers) ? answers : 0;
        }

        public double CorrectShareIn(string category)
        {
            int answers = AnswersIn(category);
            if (answers == 0)
            {
                return 0;
            }

            return (_correctByCategory.TryGetValue(category, out int correct) ? correct : 0) / (double)answers;
        }

        /// <summary>The category with the best share of correct answers; null before any answer.</summary>
        public string StrongestCategory()
        {
            return _answersByCategory.Keys
                .OrderByDescending(CorrectShareIn)
                .ThenByDescending(AnswersIn)
                .ThenBy(category => category, System.StringComparer.Ordinal)
                .FirstOrDefault();
        }

        public bool IsUnlocked(BotProfile bot)
        {
            return Wins >= bot.WinsToUnlock;
        }

        public int UnlockedCount(GameConfig config)
        {
            return config.Bots.Count(IsUnlocked);
        }

        /// <summary>The next bot still locked, the one with the fewest wins to go; null when all are unlocked.</summary>
        public BotProfile NextLockedBot(GameConfig config)
        {
            return config.Bots
                .Where(bot => !IsUnlocked(bot))
                .OrderBy(bot => bot.WinsToUnlock)
                .FirstOrDefault();
        }

        /// <summary>The selected bot if it is still valid, otherwise the first one.</summary>
        public BotProfile SelectedBot(GameConfig config)
        {
            return config.Bots.FirstOrDefault(bot => bot.Id == SelectedBotId && IsUnlocked(bot)) ?? config.Bots[0];
        }

        public string ToJson()
        {
            var json = new JObject
            {
                ["matches"] = Matches,
                ["wins"] = Wins,
                ["winStreak"] = WinStreak,
                ["bestWinStreak"] = BestWinStreak,
                ["answersGiven"] = AnswersGiven,
                ["answersCorrect"] = AnswersCorrect,
                ["selectedBot"] = SelectedBotId,
                ["script"] = Script == TextScript.Cyrillic ? "cyrillic" : "latin",
                ["sound"] = SoundOn,
                ["haptics"] = HapticsOn,
                ["winsByBot"] = ToObject(_winsByBot),
                ["answersByCategory"] = ToObject(_answersByCategory),
                ["correctByCategory"] = ToObject(_correctByCategory),
                ["seen"] = new JArray(SeenQuestions.OrderBy(id => id, System.StringComparer.Ordinal).Cast<object>().ToArray()),
                ["reported"] = new JArray(ReportedQuestions.OrderBy(id => id, System.StringComparer.Ordinal).Cast<object>().ToArray()),
            };

            return json.ToString(Newtonsoft.Json.Formatting.None);
        }

        /// <summary>Reads saved data. Missing or damaged data gives a fresh start rather than a crash.</summary>
        public static PlayerData FromJson(string text)
        {
            var data = new PlayerData();
            if (string.IsNullOrWhiteSpace(text))
            {
                return data;
            }

            JObject json;
            try
            {
                json = JObject.Parse(text);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                return data;
            }

            data.Matches = (int?)json["matches"] ?? 0;
            data.Wins = (int?)json["wins"] ?? 0;
            data.WinStreak = (int?)json["winStreak"] ?? 0;
            data.BestWinStreak = (int?)json["bestWinStreak"] ?? 0;
            data.AnswersGiven = (int?)json["answersGiven"] ?? 0;
            data.AnswersCorrect = (int?)json["answersCorrect"] ?? 0;
            data.SelectedBotId = (string)json["selectedBot"] ?? "";
            data.Script = (string)json["script"] == "cyrillic" ? TextScript.Cyrillic : TextScript.Latin;
            data.SoundOn = (bool?)json["sound"] ?? true;
            data.HapticsOn = (bool?)json["haptics"] ?? true;

            ReadObject(json["winsByBot"], data._winsByBot);
            ReadObject(json["answersByCategory"], data._answersByCategory);
            ReadObject(json["correctByCategory"], data._correctByCategory);

            if (json["seen"] is JArray seen)
            {
                foreach (JToken id in seen)
                {
                    data.SeenQuestions.Add((string)id);
                }
            }

            if (json["reported"] is JArray reported)
            {
                foreach (JToken id in reported)
                {
                    data.ReportedQuestions.Add((string)id);
                }
            }

            return data;
        }

        private static void Increase(Dictionary<string, int> counts, string key)
        {
            counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        private static JObject ToObject(Dictionary<string, int> counts)
        {
            var result = new JObject();
            foreach (KeyValuePair<string, int> entry in counts)
            {
                result[entry.Key] = entry.Value;
            }

            return result;
        }

        private static void ReadObject(JToken token, Dictionary<string, int> counts)
        {
            if (token is JObject values)
            {
                foreach (JProperty property in values.Properties())
                {
                    counts[property.Name] = (int)property.Value;
                }
            }
        }
    }
}
