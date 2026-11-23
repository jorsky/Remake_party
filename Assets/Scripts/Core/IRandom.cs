namespace RemakeParty.Core
{
    /// <summary>
    /// All randomness in the rules goes through this, so tests can use a fixed seed
    /// and a Match can be replayed exactly.
    /// </summary>
    public interface IRandom
    {
        /// <summary>Returns a number from 0 (inclusive) to maxExclusive (exclusive).</summary>
        int Next(int maxExclusive);

        /// <summary>Returns a number from 0.0 (inclusive) to 1.0 (exclusive).</summary>
        double NextDouble();
    }

    public sealed class SeededRandom : IRandom
    {
        private readonly System.Random _random;

        public SeededRandom(int seed) => _random = new System.Random(seed);

        public int Next(int maxExclusive) => _random.Next(maxExclusive);

        public double NextDouble() => _random.NextDouble();
    }
}
