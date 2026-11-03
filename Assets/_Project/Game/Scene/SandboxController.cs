using PovuciPotegni.Core;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>Development tool for the Sandbox scene: one button for each animation.</summary>
    public sealed class SandboxController : MonoBehaviour
    {
        private const int Edge = 3;
        private const int TargetFrameRate = 60;

        [SerializeField] private ArenaDirector director;

        public void Bind(ArenaDirector arenaDirector)
        {
            director = arenaDirector;
        }

        private void Awake()
        {
            Application.targetFrameRate = TargetFrameRate;
        }

        private void OnGUI()
        {
            float scale = Mathf.Max(1f, Screen.width / 400f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUILayout.BeginArea(new Rect(8, 8, Screen.width / scale - 16, Screen.height / scale - 16));
            GUILayout.Label($"k = {director.RopePosition}");

            GUI.enabled = !director.IsBusy;
            if (GUILayout.Button("Povuci za mene"))
            {
                director.PlayPull(1);
            }

            if (GUILayout.Button("Povuci za njega"))
            {
                director.PlayPull(-1);
            }

            if (GUILayout.Button("Izjednačeno"))
            {
                director.PlayPull(0);
            }

            if (GUILayout.Button("Pad"))
            {
                Fall();
            }

            if (GUILayout.Button("Pobeda"))
            {
                director.PlayCelebration(director.RopePosition >= 0 ? Side.Player : Side.Opponent);
            }

            GUI.enabled = true;
            if (GUILayout.Button("Vrati na početak"))
            {
                director.ResetArena();
            }

            GUILayout.EndArea();
        }

        /// <summary>A fall happens at the edge, so the rope jumps there first when it is not there yet.</summary>
        private void Fall()
        {
            bool opponentFalls = director.RopePosition >= 0;
            director.ShowRopePosition(opponentFalls ? Edge : -Edge);

            Side winner = opponentFalls ? Side.Player : Side.Opponent;
            Side loser = opponentFalls ? Side.Opponent : Side.Player;
            director.PlayFall(loser, () => director.PlayCelebration(winner));
        }
    }
}
