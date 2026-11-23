using System;
using System.Collections.Generic;
using System.Linq;
using RemakeParty.Core.Cpu;

namespace RemakeParty.Core.Minigames
{
    /// <summary>
    /// Stands in for real Minigames until Coconut Catch, Tiki Tap and Raft Push exist.
    /// Ranks Players at random, with higher Difficulty CPU Players more likely to place well.
    /// </summary>
    public static class PlaceholderMinigame
    {
        public static List<PlayerState> Rank(IReadOnlyList<PlayerState> players, Func<PlayerState, Difficulty> difficultyOf,
            IRandom random)
        {
            return players
                .Select(p => new { Player = p, Score = random.NextDouble() + SkillBonus(difficultyOf(p)) })
                .OrderByDescending(x => x.Score)
                .Select(x => x.Player)
                .ToList();
        }

        private static double SkillBonus(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Hard: return 0.6;
                case Difficulty.Normal: return 0.3;
                default: return 0.0;
            }
        }
    }
}
