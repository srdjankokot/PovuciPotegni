using System;

namespace PovuciPotegni.Core
{
    public sealed class SeededRandom : IRandom
    {
        private readonly Random _random;

        public SeededRandom(int seed)
        {
            _random = new Random(seed);
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }

        public int Next(int maxExclusive)
        {
            return _random.Next(maxExclusive);
        }
    }
}
