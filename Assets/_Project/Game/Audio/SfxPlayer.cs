using PovuciPotegni.Core;
using UnityEngine;

namespace PovuciPotegni.Game
{
    public enum SoundEffect
    {
        Tick,
        Correct,
        Wrong,
        Pull,
        Splash,
        Victory,
    }

    /// <summary>Plays one-shot sounds. Clips are indexed by <see cref="SoundEffect"/>.</summary>
    public sealed class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip[] clips = new AudioClip[0];

        public SoundEffect? LastPlayed { get; private set; }

        public void Bind(AudioSource audioSource, AudioClip[] soundClips)
        {
            source = audioSource;
            clips = soundClips;
        }

        public void Play(SoundEffect effect)
        {
            LastPlayed = effect;
            source.PlayOneShot(clips[(int)effect]);
        }
    }

    /// <summary>Short vibrations. iOS offers Unity only one fixed vibration, so there the lengths are ignored.</summary>
    public static class Haptics
    {
        private const string VibratorService = "vibrator";

        public static void Vibrate(int milliseconds)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject vibrator = activity.Call<AndroidJavaObject>("getSystemService", VibratorService))
                {
                    vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (AndroidJavaException exception)
            {
                Debug.LogWarning($"Vibracija nije uspela: {exception.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID
        /// <summary>Never called. Mentioning Handheld.Vibrate is what makes Unity add the VIBRATE permission on Android.</summary>
        private static void RequestVibratePermission()
        {
            Handheld.Vibrate();
        }
#endif
    }

    /// <summary>Sound and vibration for what happens in a match, each following the player's settings.</summary>
    public sealed class GameFeedback
    {
        private const int LightMilliseconds = 25;
        private const int MediumMilliseconds = 50;
        private const int StrongMilliseconds = 120;

        private readonly SfxPlayer _sfx;
        private readonly PlayerData _data;

        public GameFeedback(SfxPlayer sfx, PlayerData data)
        {
            _sfx = sfx;
            _data = data;
        }

        public void TimerTick()
        {
            Send(SoundEffect.Tick, 0);
        }

        public void Correct()
        {
            Send(SoundEffect.Correct, LightMilliseconds);
        }

        public void Wrong()
        {
            Send(SoundEffect.Wrong, MediumMilliseconds);
        }

        public void Pull()
        {
            Send(SoundEffect.Pull, MediumMilliseconds);
        }

        public void Fall()
        {
            Send(SoundEffect.Splash, StrongMilliseconds);
        }

        public void Victory()
        {
            Send(SoundEffect.Victory, MediumMilliseconds);
        }

        private void Send(SoundEffect effect, int vibration)
        {
            if (_data.SoundOn)
            {
                _sfx.Play(effect);
            }

            if (_data.HapticsOn && vibration > 0)
            {
                Haptics.Vibrate(vibration);
            }
        }
    }
}
