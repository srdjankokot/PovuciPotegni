using System;

namespace PovuciPotegni.Core
{
    public enum JokerType
    {
        FiftyFifty,
        Shield,
        Double,
    }

    /// <summary>One side's jokers: which are still available and which are armed.</summary>
    public readonly struct JokerState
    {
        public bool FiftyFiftyAvailable { get; }
        public bool ShieldAvailable { get; }
        public bool DoubleAvailable { get; }
        public bool ShieldArmed { get; }
        public bool DoubleArmed { get; }

        public static JokerState Full => new JokerState(true, true, true, false, false);

        private JokerState(bool fiftyFifty, bool shield, bool doublePull, bool shieldArmed, bool doubleArmed)
        {
            FiftyFiftyAvailable = fiftyFifty;
            ShieldAvailable = shield;
            DoubleAvailable = doublePull;
            ShieldArmed = shieldArmed;
            DoubleArmed = doubleArmed;
        }

        public bool IsAvailable(JokerType joker)
        {
            switch (joker)
            {
                case JokerType.FiftyFifty:
                    return FiftyFiftyAvailable;
                case JokerType.Shield:
                    return ShieldAvailable;
                case JokerType.Double:
                    return DoubleAvailable;
                default:
                    throw new ArgumentOutOfRangeException(nameof(joker), joker, null);
            }
        }

        public JokerState Used(JokerType joker)
        {
            switch (joker)
            {
                case JokerType.FiftyFifty:
                    return new JokerState(false, ShieldAvailable, DoubleAvailable, ShieldArmed, DoubleArmed);
                case JokerType.Shield:
                    return new JokerState(FiftyFiftyAvailable, false, DoubleAvailable, true, DoubleArmed);
                case JokerType.Double:
                    return new JokerState(FiftyFiftyAvailable, ShieldAvailable, false, ShieldArmed, true);
                default:
                    throw new ArgumentOutOfRangeException(nameof(joker), joker, null);
            }
        }

        public JokerState WithShieldSpent()
        {
            return new JokerState(FiftyFiftyAvailable, ShieldAvailable, DoubleAvailable, false, DoubleArmed);
        }

        public JokerState WithDoubleSpent()
        {
            return new JokerState(FiftyFiftyAvailable, ShieldAvailable, DoubleAvailable, ShieldArmed, false);
        }
    }
}
