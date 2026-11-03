using System;
using System.IO;
using PovuciPotegni.Game;
using UnityEditor;
using UnityEngine;

namespace PovuciPotegni.EditorTools
{
    /// <summary>Synthesizes the game's sounds into WAV files, so no paid or licensed audio is needed. Fixed seed, same result each time.</summary>
    public static class SoundBaker
    {
        private const string Folder = "Assets/_Project/Audio";
        private const int SampleRate = 44100;
        private const float Peak = 0.8f;

        public static AudioClip[] BakeAll()
        {
            var names = (SoundEffect[])Enum.GetValues(typeof(SoundEffect));
            var clips = new AudioClip[names.Length];

            foreach (SoundEffect effect in names)
            {
                string path = $"{Folder}/{effect}.wav";
                WriteWav(path, Synthesize(effect));
                AssetDatabase.ImportAsset(path);
                clips[(int)effect] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }

            return clips;
        }

        private static float[] Synthesize(SoundEffect effect)
        {
            switch (effect)
            {
                case SoundEffect.Tick:
                    return Tone(0.05f, 1400, 1400, 60);
                case SoundEffect.Correct:
                    return Join(Tone(0.09f, 660, 660, 12), Tone(0.18f, 990, 990, 10));
                case SoundEffect.Wrong:
                    return Tone(0.32f, 220, 130, 7, square: true);
                case SoundEffect.Pull:
                    return Mix(Noise(0.4f, 9, 0.1f, 1), Tone(0.4f, 120, 240, 6));
                case SoundEffect.Splash:
                    return Mix(Noise(0.9f, 5, 0.03f, 2), Tone(0.22f, 300, 90, 14));
                case SoundEffect.Victory:
                    return Join(Tone(0.12f, 523, 523, 8), Tone(0.12f, 659, 659, 8), Tone(0.12f, 784, 784, 8), Tone(0.4f, 1047, 1047, 5));
                default:
                    throw new ArgumentOutOfRangeException(nameof(effect), effect, null);
            }
        }

        /// <summary>A note gliding between two pitches, fading out exponentially.</summary>
        private static float[] Tone(float seconds, float fromHz, float toHz, float decay, bool square = false)
        {
            var samples = new float[(int)(seconds * SampleRate)];
            double phase = 0;

            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)samples.Length;
                phase += 2 * Math.PI * Mathf.Lerp(fromHz, toHz, t) / SampleRate;

                float wave = Mathf.Sin((float)phase);
                wave = square ? Mathf.Clamp(wave * 3, -1, 1) * 0.6f : wave;

                float attack = Mathf.Min(1, i / (0.003f * SampleRate));
                samples[i] = wave * attack * Mathf.Exp(-decay * t * seconds * 10);
            }

            return samples;
        }

        /// <summary>Filtered noise, for rope strain and water. A lower smoothing gives a darker sound.</summary>
        private static float[] Noise(float seconds, float decay, float smoothing, int seed)
        {
            var random = new System.Random(seed);
            var samples = new float[(int)(seconds * SampleRate)];
            float filtered = 0;

            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)samples.Length;
                filtered += ((float)random.NextDouble() * 2 - 1 - filtered) * smoothing;
                samples[i] = filtered * Mathf.Exp(-decay * t);
            }

            return Normalize(samples);
        }

        private static float[] Join(params float[][] parts)
        {
            int length = 0;
            foreach (float[] part in parts)
            {
                length += part.Length;
            }

            var joined = new float[length];
            int offset = 0;
            foreach (float[] part in parts)
            {
                part.CopyTo(joined, offset);
                offset += part.Length;
            }

            return joined;
        }

        private static float[] Mix(float[] a, float[] b)
        {
            var mixed = new float[Mathf.Max(a.Length, b.Length)];
            for (int i = 0; i < mixed.Length; i++)
            {
                mixed[i] = (i < a.Length ? a[i] : 0) * 0.6f + (i < b.Length ? b[i] : 0) * 0.5f;
            }

            return mixed;
        }

        private static float[] Normalize(float[] samples)
        {
            float loudest = 0.0001f;
            foreach (float sample in samples)
            {
                loudest = Mathf.Max(loudest, Mathf.Abs(sample));
            }

            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] /= loudest;
            }

            return samples;
        }

        private static void WriteWav(string path, float[] samples)
        {
            float[] normalized = Normalize(samples);

            using (var writer = new BinaryWriter(File.Create(path)))
            {
                int dataSize = normalized.Length * 2;
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);

                foreach (float sample in normalized)
                {
                    writer.Write((short)(sample * Peak * short.MaxValue));
                }
            }
        }
    }
}
