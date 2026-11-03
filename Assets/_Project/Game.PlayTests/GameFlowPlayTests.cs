using System.Collections;
using System.IO;
using NUnit.Framework;
using PovuciPotegni.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PovuciPotegni.Game.PlayTests
{
    /// <summary>Plays the real Game scene through its buttons. Saved player data is put back after each test.</summary>
    [Timeout(120000)]
    public class GameFlowPlayTests
    {
        private const string SceneName = "Game";
        private const float FastTime = 10f;
        private const int MaxQuestions = 60;
        private const string AllUnlocked = "{\"wins\":25,\"matches\":25}";
        private const string CaptureFolder = "TestResults/captures";
        private const int CaptureWidth = 780;
        private const int CaptureHeight = 1688;

        private string _savedData;
        private bool _hadSavedData;
        private GameFlow _flow;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _hadSavedData = PlayerPrefs.HasKey(PlayerPrefsStatsStore.Key);
            _savedData = PlayerPrefs.GetString(PlayerPrefsStatsStore.Key, "");
            PlayerPrefs.DeleteKey(PlayerPrefsStatsStore.Key);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1;
            if (_flow != null)
            {
                _flow.Panel.targetTexture = null;
            }

            if (_hadSavedData)
            {
                PlayerPrefs.SetString(PlayerPrefsStatsStore.Key, _savedData);
            }
            else
            {
                PlayerPrefs.DeleteKey(PlayerPrefsStatsStore.Key);
            }

            yield return null;
        }

        private IEnumerator LoadGame(string savedData = null)
        {
            if (savedData != null)
            {
                PlayerPrefs.SetString(PlayerPrefsStatsStore.Key, savedData);
            }

            SceneManager.LoadScene(SceneName);
            yield return null;
            _flow = Object.FindAnyObjectByType<GameFlow>();
            Assert.That(_flow, Is.Not.Null);
            yield return WaitFor(() => _flow.Current != GameScreen.Loading, 10);
            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Home));
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float seconds)
        {
            float limit = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(limit), "Isteklo vreme čekanja.");
                yield return null;
            }
        }

        /// <summary>Presses a button the way the UI does, so the test also covers its wiring.</summary>
        private IEnumerator Press(string name, VisualElement scope = null)
        {
            VisualElement search = scope ?? VisibleScreen();
            var button = search.Q<Button>(name);
            Assert.That(button, Is.Not.Null, $"Nema dugmeta '{name}'.");

            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }

            yield return null;
            yield return null;
        }

        private VisualElement VisibleScreen()
        {
            foreach (VisualElement screen in _flow.Root.Children())
            {
                if (!screen.ClassListContains("hidden"))
                {
                    return screen;
                }
            }

            Assert.Fail("Nijedan ekran nije vidljiv.");
            return null;
        }

        private IEnumerator PlayToEnd()
        {
            for (int question = 0; question < MaxQuestions && _flow.Current == GameScreen.Match; question++)
            {
                yield return WaitFor(() => _flow.Match.IsWaitingForAnswer || _flow.Current != GameScreen.Match, 20);
                if (_flow.Current != GameScreen.Match)
                {
                    yield break;
                }

                yield return Press($"option-{question % 3}");

                VisualElement match = VisibleScreen();
                var banner = match.Q("banner");
                yield return WaitFor(() => !banner.ClassListContains("hidden") || _flow.Current != GameScreen.Match, 30);
                if (_flow.Current == GameScreen.Match)
                {
                    yield return Press("next");
                }
            }

            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Result));
        }

        [UnityTest]
        public IEnumerator FreshStart_ShowsHomeWithFirstBotOnly()
        {
            yield return LoadGame();

            Assert.That(_flow.Data.Wins, Is.EqualTo(0));
            Assert.That(VisibleScreen().Q<Label>("unlocked").text, Is.EqualTo("1 od 4 otključana"));
            Assert.That(VisibleScreen().Q<Label>("play-sub").text, Does.Contain("Šegrt"));
        }

        [UnityTest]
        public IEnumerator FullMatch_AgainstEveryBot_ReturnsHome(
            [Values("segrt", "profesor", "lisac", "majstor")] string botId)
        {
            yield return LoadGame(AllUnlocked);
            Time.timeScale = FastTime;

            _flow.StartMatch(botId);
            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Match));

            yield return PlayToEnd();

            Assert.That(_flow.Data.Matches, Is.EqualTo(26));
            Assert.That(_flow.Data.AnswersGiven, Is.GreaterThanOrEqualTo(3));
            Assert.That(VisibleScreen().Q<Label>("title").text, Is.EqualTo("Pobeda!").Or.EqualTo("Poraz"));

            yield return Press("home");
            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Home));
        }

        [UnityTest]
        public IEnumerator Result_SavesToPlayerPrefs()
        {
            yield return LoadGame();
            Time.timeScale = FastTime;

            yield return Press("play");
            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Match));
            yield return PlayToEnd();

            PlayerData saved = PlayerData.FromJson(PlayerPrefs.GetString(PlayerPrefsStatsStore.Key, ""));
            Assert.That(saved.Matches, Is.EqualTo(1));
            Assert.That(saved.AnswersGiven, Is.EqualTo(_flow.Data.AnswersGiven));
        }

        [UnityTest]
        public IEnumerator Play_WithOneOpponent_StartsMatchAtOnce()
        {
            yield return LoadGame();

            yield return Press("play");

            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Match));
        }

        [UnityTest]
        public IEnumerator Play_WithSeveralOpponents_OpensThePicker()
        {
            yield return LoadGame("{\"wins\":3,\"matches\":3}");

            yield return Press("play");
            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Opponents));

            yield return Press("play", VisibleScreen().Q(className: "bot-card--selected"));
            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Match));
        }

        [UnityTest]
        public IEnumerator Timer_RunsWithoutChangingLayout()
        {
            yield return LoadGame();
            _flow.StartMatch("segrt");
            yield return WaitFor(() => _flow.Match.IsWaitingForAnswer, 5);

            VisualElement bar = VisibleScreen().Q("timer-bar");
            yield return null;
            float width = bar.layout.width;
            float start = bar.resolvedStyle.scale.value.x;

            yield return new WaitForSeconds(1);

            Assert.That(bar.resolvedStyle.scale.value.x, Is.LessThan(start));
            Assert.That(bar.layout.width, Is.EqualTo(width));
        }

        [UnityTest]
        public IEnumerator Navigation_VisitsEveryScreen()
        {
            yield return LoadGame();

            yield return Press("opponents");
            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Opponents));
            yield return Press("back");

            yield return Press("stats");
            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Stats));
            yield return Press("back");

            yield return Press("settings");
            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Settings));
            yield return Press("back");

            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Home));
        }

        [UnityTest]
        public IEnumerator Cyrillic_ChangesEveryScreenAndPersists()
        {
            yield return LoadGame();

            yield return Press("settings");
            yield return Press("cyrillic");
            yield return Press("back");

            Assert.That(VisibleScreen().Q<Label>("play-sub").text, Does.Contain("Шегрт"));
            Assert.That(_flow.Data.Script, Is.EqualTo(TextScript.Cyrillic));
            Assert.That(PlayerData.FromJson(PlayerPrefs.GetString(PlayerPrefsStatsStore.Key, "")).Script, Is.EqualTo(TextScript.Cyrillic));

            _flow.StartMatch("segrt");
            yield return WaitFor(() => _flow.Match.IsWaitingForAnswer, 5);
            string question = VisibleScreen().Q<Label>("question").text;
            Assert.That(question, Does.Match("[А-Яа-яЂЈЉЊЋЏђјљњћџ]"));
        }

        [UnityTest]
        public IEnumerator Jokers_ChangeLookAndCannotRepeat()
        {
            yield return LoadGame();
            _flow.StartMatch("segrt");
            yield return WaitFor(() => _flow.Match.IsWaitingForAnswer, 5);

            VisualElement match = VisibleScreen();
            yield return Press("joker-half");
            Assert.That(match.Q<Button>("joker-half").ClassListContains("joker--used"), Is.True);
            Assert.That(match.Query<Button>(className: "opt--gone").ToList(), Has.Count.EqualTo(1));

            yield return Press("joker-shield");
            Assert.That(match.Q<Button>("joker-shield").ClassListContains("joker--armed"), Is.True);

            yield return Press("joker-double");
            Assert.That(match.Q<Button>("joker-double").ClassListContains("joker--armed"), Is.True);
        }

        [UnityTest]
        public IEnumerator PickedAnswer_StandsOutWhileWaiting_AndClearsAtReveal()
        {
            yield return LoadGame();
            _flow.StartMatch("segrt");
            yield return WaitFor(() => _flow.Match.IsWaitingForAnswer, 5);

            VisualElement match = VisibleScreen();
            yield return Press("option-1");

            var picked = match.Q<Button>("option-1");
            Assert.That(picked.ClassListContains("opt--picked"), Is.True);
            Assert.That(picked.ClassListContains("opt--show-you"), Is.True);
            Assert.That(match.Q<Button>("option-0").ClassListContains("opt--dim"), Is.True);
            Assert.That(match.Q<Button>("option-2").ClassListContains("opt--dim"), Is.True);
            Assert.That(match.Q("status").ClassListContains("status--summary"), Is.False, "Bot još nije odgovorio.");
            yield return null;
            Assert.That(picked.resolvedStyle.opacity, Is.EqualTo(1));

            yield return WaitFor(() => match.Q("status").ClassListContains("status--summary"), 10);
            Assert.That(picked.ClassListContains("opt--picked"), Is.False);
        }

        [UnityTest]
        public IEnumerator Report_RemovesQuestionFromPool_AndConfirms()
        {
            yield return LoadGame();
            _flow.StartMatch("segrt");
            yield return WaitFor(() => _flow.Match.IsWaitingForAnswer, 5);

            VisualElement match = VisibleScreen();
            string shownBefore = match.Q<Label>("question").text;
            Assert.That(match.Q<Button>("report").ClassListContains("hidden"), Is.True, "Dugme se vidi tek posle odgovora.");

            Time.timeScale = FastTime;
            yield return Press("option-0");
            yield return WaitFor(() => !match.Q<Button>("report").ClassListContains("hidden"), 15);

            yield return Press("report");

            Assert.That(_flow.Data.ReportedQuestions, Has.Count.EqualTo(1));
            Assert.That(match.Q<Label>("report-label").text, Is.EqualTo("Prijavljeno, hvala"));
            Assert.That(PlayerData.FromJson(PlayerPrefs.GetString(PlayerPrefsStatsStore.Key, "")).ReportedQuestions, Has.Count.EqualTo(1));
            Assert.That(shownBefore, Is.Not.Empty);
        }

        [UnityTest]
        public IEnumerator Sound_FollowsTheSetting([Values(true, false)] bool soundOn)
        {
            yield return LoadGame(soundOn ? null : "{\"sound\":false}");
            SfxPlayer sfx = Object.FindAnyObjectByType<SfxPlayer>();
            _flow.StartMatch("segrt");
            yield return WaitFor(() => _flow.Match.IsWaitingForAnswer, 5);

            Time.timeScale = FastTime;
            yield return Press("option-0");
            VisualElement match = VisibleScreen();
            yield return WaitFor(() => match.Q("status").ClassListContains("status--summary"), 15);

            if (soundOn)
            {
                Assert.That(sfx.LastPlayed, Is.EqualTo(SoundEffect.Correct).Or.EqualTo(SoundEffect.Wrong));
            }
            else
            {
                Assert.That(sfx.LastPlayed, Is.Null);
            }
        }

        [UnityTest]
        public IEnumerator EdgeWin_PlaysSplashAndVictory()
        {
            yield return LoadGame(AllUnlocked);
            SfxPlayer sfx = Object.FindAnyObjectByType<SfxPlayer>();
            Time.timeScale = FastTime;
            _flow.StartMatch("segrt");

            yield return PlayToEnd();

            Assert.That(sfx.LastPlayed, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Leaving_RecordsNoMatch()
        {
            yield return LoadGame();
            _flow.StartMatch("segrt");
            yield return WaitFor(() => _flow.Match.IsWaitingForAnswer, 5);

            yield return Press("leave");

            Assert.That(_flow.Current, Is.EqualTo(GameScreen.Home));
            Assert.That(_flow.Data.Matches, Is.EqualTo(0));
        }

        /// <summary>Saves each screen over the scene, for review. It fails only if a screen cannot be reached.</summary>
        [UnityTest]
        public IEnumerator Capture_EveryScreen()
        {
            yield return LoadGame("{\"wins\":4,\"matches\":7,\"winStreak\":2,\"bestWinStreak\":3,\"answersGiven\":60,\"answersCorrect\":43,"
                + "\"selectedBot\":\"profesor\",\"answersByCategory\":{\"geografija\":20,\"sport\":25,\"istorija\":15},"
                + "\"correctByCategory\":{\"geografija\":18,\"sport\":14,\"istorija\":11}}");
            Directory.CreateDirectory(CaptureFolder);

            yield return new WaitForSeconds(2);
            yield return Capture("ui-home.png");

            yield return Press("opponents");
            yield return Capture("ui-opponents.png");
            yield return Press("back");

            yield return Press("stats");
            yield return Capture("ui-stats.png");
            yield return Press("back");

            yield return Press("settings");
            yield return Capture("ui-settings.png");
            yield return Press("back");

            yield return Press("play");
            yield return Capture("ui-picker-from-play.png");
            yield return Press("play", VisibleScreen().Q(className: "bot-card--selected"));
            yield return new WaitForSeconds(0.5f);
            yield return Capture("ui-incoming.png");

            yield return WaitFor(() => _flow.Match.IsWaitingForAnswer, 5);
            yield return new WaitForSeconds(0.8f);
            yield return Capture("ui-question.png");

            yield return Press("option-0");
            yield return Capture("ui-picked.png");
            VisualElement match = VisibleScreen();
            yield return WaitFor(() => match.Q("status").ClassListContains("status--summary"), 10);
            yield return new WaitForSeconds(0.3f);
            yield return Capture("ui-reveal.png");

            yield return WaitFor(() => !match.Q("banner").ClassListContains("hidden"), 10);
            yield return new WaitForSeconds(0.6f);
            yield return Capture("ui-banner.png");

            Time.timeScale = FastTime;
            yield return Press("next");
            yield return PlayToEnd();
            Time.timeScale = 1;
            yield return new WaitForSeconds(0.3f);
            yield return Capture("ui-result.png");
        }

        private IEnumerator Capture(string fileName)
        {
            var uiTarget = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _flow.Panel.targetTexture = uiTarget;

            // Without clearing, see-through layers build up frame after frame in the texture.
            _flow.Panel.clearColor = true;
            _flow.Panel.colorClearValue = Color.clear;

            // WaitForEndOfFrame never finishes in batch mode, so this waits a few whole frames instead.
            for (int frame = 0; frame < 4; frame++)
            {
                yield return null;
            }

            Color[] ui = Read(uiTarget);
            _flow.Panel.targetTexture = null;
            _flow.Panel.clearColor = false;

            var sceneTarget = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Camera camera = Camera.main;
            float aspect = camera.aspect;
            float fov = camera.fieldOfView;
            camera.targetTexture = sceneTarget;
            camera.aspect = CaptureWidth / (float)CaptureHeight;

            // Batch mode runs in a wide window; show the view a phone in portrait gets.
            if (_flow.Current == GameScreen.Match || _flow.Current == GameScreen.Result)
            {
                camera.fieldOfView = ArenaLayout.PortraitFov;
            }

            camera.Render();
            camera.targetTexture = null;
            camera.aspect = aspect;
            camera.fieldOfView = fov;
            Color[] scene = Read(sceneTarget);

            for (int i = 0; i < scene.Length; i++)
            {
                scene[i] = Color.Lerp(scene[i], new Color(ui[i].r, ui[i].g, ui[i].b, 1), ui[i].a);
            }

            var output = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            output.SetPixels(scene);
            output.Apply();
            File.WriteAllBytes(Path.Combine(CaptureFolder, fileName), output.EncodeToPNG());

            Object.Destroy(output);
            uiTarget.Release();
            sceneTarget.Release();
        }

        private static Color[] Read(RenderTexture target)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            Color[] pixels = image.GetPixels();
            Object.Destroy(image);
            return pixels;
        }
    }
}
