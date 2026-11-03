using PovuciPotegni.Core;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>Editable wrapper around the plain <see cref="GameConfig"/> used by Core.</summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Povuci-Potegni/Game Config")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        [SerializeField] private GameConfig config = new GameConfig();

        public GameConfig Config => config;
    }
}
