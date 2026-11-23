namespace RemakeParty.Core
{
    /// <summary>
    /// Every tunable number in the core rules, in one place.
    /// Change a value here and the whole game follows.
    /// </summary>
    public static class Rules
    {
        public const int PlayerCount = 4;
        public const int RoundsPerMatch = 10;

        public const int StartingCoins = 10;
        public const int CrownCost = 20;

        public const int GainSpaceCoins = 3;
        public const int LoseSpaceCoins = 5;
        public const int CoconutDropCoins = 5;

        /// <summary>Coins paid by Minigame placing: index 0 is 1st place.</summary>
        public static readonly int[] MinigamePayouts = { 15, 8, 4, 0 };
    }
}
