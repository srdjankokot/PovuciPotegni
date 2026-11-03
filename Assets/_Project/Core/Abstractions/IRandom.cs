namespace PovuciPotegni.Core
{
    public interface IRandom
    {
        /// <summary>Returns a value in [0, 1).</summary>
        double NextDouble();

        /// <summary>Returns an integer in [0, maxExclusive).</summary>
        int Next(int maxExclusive);
    }
}
