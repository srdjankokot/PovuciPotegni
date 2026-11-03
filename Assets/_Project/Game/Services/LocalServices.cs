using System.Collections.Generic;
using System.Linq;
using PovuciPotegni.Core;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>Writes events to the Console until a real analytics service is connected. Release builds stay silent.</summary>
    public sealed class NullAnalytics : IAnalytics
    {
        public void Track(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string values = string.Join(", ", parameters.Select(entry => $"{entry.Key}={entry.Value}"));
            Debug.Log($"[analitika] {eventName}: {values}");
#endif
        }
    }

    /// <summary>Keeps reported questions on the device: they leave the pool at once and can be sent to a server later.</summary>
    public sealed class LocalReportSink : IReportSink
    {
        private readonly IStatsStore _store;

        public LocalReportSink(IStatsStore store)
        {
            _store = store;
        }

        public void ReportQuestion(string questionId)
        {
            _store.Data.ReportedQuestions.Add(questionId);
            _store.Save();
        }
    }
}
