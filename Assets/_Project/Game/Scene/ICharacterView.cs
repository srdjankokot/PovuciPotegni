using UnityEngine;

namespace PovuciPotegni.Game
{
    public enum PullRole
    {
        Winner,
        Loser,
        Tie,
    }

    /// <summary>
    /// Hides whether a character is built from primitives or is a rigged model. ArenaDirector owns the timing
    /// and reports progress, so the rope, the camera and both characters stay in step.
    /// </summary>
    public interface ICharacterView
    {
        Vector3 HandsPoint { get; }

        void SetMood(CharacterMood mood);
        void ShowIdle();
        void ShowPull(PullRole role, float progress);
        void ShowFall(float progress);
        void ShowSwimming();
        void ShowCelebration();

        /// <summary>Applies the current pose at this time and stands the figure at the given rope position.</summary>
        void UpdateView(float baseZ, float time);
    }
}
