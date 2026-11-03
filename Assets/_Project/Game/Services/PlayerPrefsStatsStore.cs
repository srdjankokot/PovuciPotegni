using PovuciPotegni.Core;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>Keeps the player's data as one JSON string in PlayerPrefs.</summary>
    public sealed class PlayerPrefsStatsStore : IStatsStore
    {
        public const string Key = "povuci-potegni.player";

        public PlayerData Data { get; }

        public PlayerPrefsStatsStore()
        {
            Data = PlayerData.FromJson(PlayerPrefs.GetString(Key, ""));
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, Data.ToJson());
            PlayerPrefs.Save();
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

    public sealed class UnityClock : IClock
    {
        public double NowSeconds => Time.timeAsDouble;
    }
}
