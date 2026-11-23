using System.Collections.Generic;

namespace RemakeParty.Core
{
    public sealed class PlayerState
    {
        public int Seat { get; }
        public string CharacterName { get; }

        public int Coins { get; internal set; } = Rules.StartingCoins;
        public int Crowns { get; internal set; }
        public int SpaceId { get; internal set; }

        /// <summary>
        /// The Spaces this Player walked forward from, newest last.
        /// Moving backward pops from here so the Player retraces their own path through forks.
        /// </summary>
        internal readonly List<int> Trail = new List<int>();

        public PlayerState(int seat, string characterName, int startSpaceId)
        {
            Seat = seat;
            CharacterName = characterName;
            SpaceId = startSpaceId;
        }

        internal void AddCoins(int amount) => Coins += amount;

        /// <summary>Removes coins without going below zero. Returns how many were actually lost.</summary>
        internal int LoseCoins(int amount)
        {
            int lost = amount < Coins ? amount : Coins;
            Coins -= lost;
            return lost;
        }

        public override string ToString() => $"P{Seat + 1} {CharacterName}";
    }
}
