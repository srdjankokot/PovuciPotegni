using System;

namespace PovuciPotegni.Core
{
    /// <summary>Rope marker position: 0 is the middle, positive is toward the player.</summary>
    public readonly struct RopePosition
    {
        public int Value { get; }

        public RopePosition(int value)
        {
            Value = value;
        }

        public RopePosition Moved(int steps, int edge)
        {
            return new RopePosition(Math.Max(-edge, Math.Min(edge, Value + steps)));
        }

        public bool IsAtEdge(int edge)
        {
            return Math.Abs(Value) >= edge;
        }

        public Side FavoredSide
        {
            get
            {
                if (Value > 0)
                {
                    return Side.Player;
                }

                return Value < 0 ? Side.Opponent : Side.None;
            }
        }
    }
}
