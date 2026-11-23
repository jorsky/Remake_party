using System;
using System.Collections.Generic;
using System.Linq;

namespace RemakeParty.Core.Cpu
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
    }

    /// <summary>
    /// Makes Board decisions for one CPU Player.
    /// Easy: random dice and forks. Normal: plans forks, gambles on the Risky Die only when the Crown is far.
    /// Hard: previews every face of both dice and picks the better one, so it will roll backward onto a Crown.
    /// </summary>
    public sealed class CpuBrain
    {
        private const int LookAheadSpaces = 6;
        private const int NormalRiskyDistance = 8;

        private readonly IRandom _random;

        public Difficulty Difficulty { get; }

        public CpuBrain(Difficulty difficulty, IRandom random)
        {
            Difficulty = difficulty;
            _random = random;
        }

        /// <summary>Answers any Board decision for this CPU Player's own Turn.</summary>
        public void Answer(DecisionRequest request, Match match)
        {
            switch (request)
            {
                case ChooseDieRequest die:
                    die.Answer = ChooseDie(die.Player, match);
                    break;
                case ChooseForkRequest fork:
                    fork.Answer = ChooseFork(fork.Player, fork.Fork, match);
                    break;
                case BuyCrownRequest buy:
                    buy.Answer = true; // A Crown is always worth buying.
                    break;
                default:
                    throw new ArgumentException($"CpuBrain cannot answer {request.GetType().Name}.");
            }
        }

        public DieKind ChooseDie(PlayerState player, Match match)
        {
            switch (Difficulty)
            {
                case Difficulty.Easy:
                    return _random.Next(2) == 0 ? DieKind.Safe : DieKind.Risky;

                case Difficulty.Normal:
                    int distance = match.Board.ForwardDistance(player.SpaceId, match.CrownSpaceId);
                    return distance >= NormalRiskyDistance ? DieKind.Risky : DieKind.Safe;

                default:
                    return ExpectedScore(player, match, DieKind.Risky) > ExpectedScore(player, match, DieKind.Safe)
                        ? DieKind.Risky
                        : DieKind.Safe;
            }
        }

        public int ChooseFork(PlayerState player, Space fork, Match match)
        {
            if (Difficulty == Difficulty.Easy)
                return fork.Next[_random.Next(fork.Next.Count)];

            return PlannedFork(player.Coins, fork, match);
        }

        /// <summary>Heads for the Crown when it can afford one; otherwise takes the path with the most Coins.</summary>
        private static int PlannedFork(int coins, Space fork, Match match)
        {
            if (coins >= Rules.CrownCost)
            {
                return fork.Next
                    .OrderBy(next => DistanceOrFar(match.Board, next, match.CrownSpaceId))
                    .First();
            }

            return fork.Next
                .OrderByDescending(next => CoinValueAhead(match.Board, next))
                .First();
        }

        private static int DistanceOrFar(Board board, int from, int to)
        {
            int d = board.ForwardDistance(from, to);
            return d < 0 ? int.MaxValue : d;
        }

        /// <summary>Rough Coin value of the next few Spaces down a path (ignoring further forks).</summary>
        private static int CoinValueAhead(Board board, int start)
        {
            int value = 0;
            int current = start;
            for (int i = 0; i < LookAheadSpaces; i++)
            {
                value += SpaceValue(board[current].Type);
                current = board[current].Next[0];
            }
            return value;
        }

        private static int SpaceValue(SpaceType type)
        {
            switch (type)
            {
                case SpaceType.Gain: return Rules.GainSpaceCoins;
                case SpaceType.Lose: return -Rules.LoseSpaceCoins;
                default: return 0;
            }
        }

        private static double ExpectedScore(PlayerState player, Match match, DieKind die)
        {
            int[] faces = Dice.Faces(die);
            double total = 0;
            foreach (int face in faces)
            {
                List<int> path = MovePlanner.Plan(match.Board, player.SpaceId, match.TrailOf(player), face,
                    fork => PlannedFork(player.Coins, fork, match));
                total += ScorePath(player, match, path);
            }
            return total / faces.Length;
        }

        private static double ScorePath(PlayerState player, Match match, List<int> path)
        {
            bool canBuy = player.Coins >= Rules.CrownCost;
            if (canBuy && path.Contains(match.CrownSpaceId))
                return 100;

            int stop = path[path.Count - 1];
            double score = SpaceValue(match.Board[stop].Type);

            int distance = match.Board.ForwardDistance(stop, match.CrownSpaceId);
            if (distance > 0)
                score -= distance * (canBuy ? 1.0 : 0.25);

            return score;
        }
    }
}
