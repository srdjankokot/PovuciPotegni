using System;
using System.Collections;
using System.IO;
using PovuciPotegni.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace PovuciPotegni.Game
{
    /// <summary>Loads the question file. On Android, StreamingAssets sits inside the package, so it needs a web request.</summary>
    public static class QuestionLoader
    {
        private const string RelativePath = "questions/sr-Latn.json";

        public static IEnumerator Load(bool allowUnverified, Action<QuestionBank> onLoaded, Action<string> onFailed)
        {
            string path = Path.Combine(Application.streamingAssetsPath, RelativePath);
            string uri = path.Contains("://") ? path : new Uri(path).AbsoluteUri;

            using (UnityWebRequest request = UnityWebRequest.Get(uri))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onFailed(request.error);
                    yield break;
                }

                QuestionBank bank;
                try
                {
                    bank = QuestionBank.FromJson(request.downloadHandler.text, allowUnverified);
                }
                catch (Exception exception) when (exception is FormatException || exception is Newtonsoft.Json.JsonException)
                {
                    onFailed(exception.Message);
                    yield break;
                }

                onLoaded(bank);
            }
        }
    }
}
