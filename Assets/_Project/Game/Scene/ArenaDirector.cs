using System;
using PovuciPotegni.Core;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>
    /// Plays the arena's animations and keeps the rope, both characters, the camera, the crocodile and the
    /// effects in step. It only shows results; the rules decide them in Core.
    /// </summary>
    public sealed class ArenaDirector : MonoBehaviour
    {
        private const int Edge = 3;
        private const float PlayerYaw = 0f;
        private const float OpponentYaw = 180f;
        private const float DustHeight = 0.08f;
        private const float ConfettiHeight = 2.6f;
        private const float ConfettiAhead = 3f;

        private enum Activity
        {
            Idle,
            Pull,
            Tie,
            Fall,
            Finished,
        }

        [SerializeField] private PoseController player;
        [SerializeField] private PoseController opponent;
        [SerializeField] private RopeView rope;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField] private CrocodileView crocodile;
        [SerializeField] private FxPool fx;

        private Activity _activity;
        private int _k;
        private float _time;
        private float _elapsed;
        private float _offset;
        private float _pullFrom;
        private float _pullTo;
        private float _nextDust;
        private bool _splashed;
        private PoseController _winner;
        private PoseController _loser;
        private Action _onDone;

        /// <summary>Raised when someone hits the water.</summary>
        public event Action Splashed;

        /// <summary>The rope position k: 0 is the middle, positive is toward the player.</summary>
        public int RopePosition => _k;

        public bool IsBusy => _activity == Activity.Pull || _activity == Activity.Tie || _activity == Activity.Fall;

        public PoseController Player => player;
        public PoseController Opponent => opponent;
        public CrocodileView Crocodile => crocodile;

        public void Bind(
            PoseController playerView,
            PoseController opponentView,
            RopeView ropeView,
            CameraRig camera,
            CrocodileView crocodileView,
            FxPool effects)
        {
            player = playerView;
            opponent = opponentView;
            rope = ropeView;
            cameraRig = camera;
            crocodile = crocodileView;
            fx = effects;
        }

        /// <summary>Puts a different character on the far side of the rope.</summary>
        public void SetOpponent(PoseController view)
        {
            opponent = view;
            rope.SetOpponent(view.GetComponent<CharacterRig>());
        }

        /// <summary>Shows a rope position at once, without animation.</summary>
        public void ShowRopePosition(int k)
        {
            _k = Mathf.Clamp(k, -Edge, Edge);
            _offset = ArenaLayout.RopeOffset(_k);
        }

        /// <summary>Moves the rope by signed steps, positive toward the player; zero plays the tie.</summary>
        public void PlayPull(int steps, Action onDone = null)
        {
            if (IsBusy)
            {
                return;
            }

            _elapsed = 0;
            _onDone = onDone;
            _pullFrom = _offset = ArenaLayout.RopeOffset(_k);

            int target = Mathf.Clamp(_k + steps, -Edge, Edge);
            if (target == _k)
            {
                _activity = Activity.Tie;
                _winner = null;
                _loser = null;
                return;
            }

            _k = target;
            _pullTo = ArenaLayout.RopeOffset(_k);
            _nextDust = 0;
            _activity = Activity.Pull;
            _winner = steps > 0 ? player : opponent;
            _loser = steps > 0 ? opponent : player;
            _loser.SetMood(CharacterMood.Oh);
        }

        /// <summary>The loser teeters at the edge and falls in. The rope must be at that side's edge.</summary>
        public void PlayFall(Side loser, Action onDone = null)
        {
            if (IsBusy)
            {
                return;
            }

            _elapsed = 0;
            _onDone = onDone;
            _splashed = false;
            _activity = Activity.Fall;
            _loser = loser == Side.Player ? player : opponent;
            _winner = loser == Side.Player ? opponent : player;
            _offset = ArenaLayout.RopeOffset(_k);
            _loser.SetMood(CharacterMood.Oh);
        }

        public void PlayCelebration(Side winner)
        {
            PoseController view = winner == Side.Player ? player : opponent;
            view.ShowCelebration();
            view.SetMood(CharacterMood.Smile);

            if (winner == Side.Player)
            {
                Vector3 position = view.transform.position;
                fx.Confetti(new Vector3(position.x, ConfettiHeight, position.z + ConfettiAhead));
            }
        }

        public void ResetArena()
        {
            _activity = Activity.Idle;
            _onDone = null;
            _winner = null;
            _loser = null;
            ShowRopePosition(0);

            player.ShowIdle();
            opponent.ShowIdle();
            player.SetMood(CharacterMood.Grit);
            opponent.SetMood(CharacterMood.Grit);
            crocodile.ClearFocus();
            fx.Clear();
        }

        /// <summary>Advances everything by one step. Public so the editor capture can run it without Play mode.</summary>
        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            _elapsed += deltaTime;

            float shake = 0;
            float fovKick = 0;
            float wave = ArenaMotion.IdleWave;

            switch (_activity)
            {
                case Activity.Pull:
                    TickPull(ref shake, ref fovKick, ref wave);
                    break;
                case Activity.Tie:
                    TickTie(ref wave);
                    break;
                case Activity.Fall:
                    TickFall();
                    break;
                case Activity.Idle:
                    _offset = ArenaMotion.IdleOffset(ArenaLayout.RopeOffset(_k), _time);
                    break;
            }

            player.SetFacing(PlayerYaw);
            opponent.SetFacing(OpponentYaw);
            player.UpdateView(ArenaLayout.PlayerZ(_offset), _time);
            opponent.UpdateView(ArenaLayout.OpponentZ(_offset), _time);

            rope.Wave = wave;
            rope.Rebuild(_time);
            cameraRig.Tick(deltaTime, shake, fovKick);
            crocodile.Tick(deltaTime, _time, _k);
            fx.Tick(deltaTime);

            FinishIfDone();
        }

        private void Awake()
        {
            ShowRopePosition(0);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void TickPull(ref float shake, ref float fovKick, ref float wave)
        {
            float p = Mathf.Min(1, _elapsed / ArenaMotion.PullSeconds);
            _offset = ArenaMotion.PullOffset(p, _pullFrom, _pullTo);
            wave = ArenaMotion.PullWave(p);
            fovKick = ArenaMotion.FovKick(p);
            shake = ArenaMotion.CameraShake(p, _time);

            _winner.ShowPull(PullRole.Winner, p);
            _loser.ShowPull(PullRole.Loser, p);

            if (p > ArenaMotion.DustStart && p < ArenaMotion.DustEnd && _elapsed >= _nextDust)
            {
                _nextDust = _elapsed + ArenaMotion.DustIntervalSeconds;
                Vector3 feet = _loser.transform.position;
                fx.Dust(new Vector3(feet.x, DustHeight, feet.z));
            }
        }

        private void TickTie(ref float wave)
        {
            float p = Mathf.Min(1, _elapsed / ArenaMotion.TieSeconds);
            _offset = ArenaMotion.TieOffset(p, _pullFrom);
            wave = ArenaMotion.TieWave(p);
            player.ShowPull(PullRole.Tie, p);
            opponent.ShowPull(PullRole.Tie, p);
        }

        private void TickFall()
        {
            float p = Mathf.Min(1, _elapsed / ArenaMotion.FallSeconds);
            _loser.ShowFall(p);

            if (!_splashed && p > ArenaMotion.SplashAt)
            {
                _splashed = true;
                Vector3 position = _loser.transform.position;
                fx.Splash(position.x, position.z);
                crocodile.FocusOn(position);
                Splashed?.Invoke();
            }
        }

        private void FinishIfDone()
        {
            Activity finished = _activity;
            bool done =
                (finished == Activity.Pull && _elapsed >= ArenaMotion.PullSeconds)
                || (finished == Activity.Tie && _elapsed >= ArenaMotion.TieSeconds)
                || (finished == Activity.Fall && _elapsed >= ArenaMotion.FallSeconds);

            if (!done)
            {
                return;
            }

            if (finished == Activity.Fall)
            {
                _loser.ShowSwimming();
                _activity = Activity.Finished;
            }
            else
            {
                _offset = finished == Activity.Pull ? _pullTo : _pullFrom;
                player.ShowIdle();
                opponent.ShowIdle();
                _loser?.SetMood(CharacterMood.Grit);
                _activity = Activity.Idle;
            }

            Action callback = _onDone;
            _onDone = null;
            callback?.Invoke();
        }
    }
}
