using System;
using System.Collections.Generic;
using System.Linq;

namespace RemakeParty.Core
{
    /// <summary>
    /// One full Match: Turn-Order Roll, every Round, every Turn, and the final standings.
    ///
    /// Run() hands back MatchEvents one at a time. When it hands back a DecisionRequest,
    /// the caller must answer it before asking for the next event. This keeps all the rules
    /// here, with no Unity code, so they can be tested and later reused for human Players.
    /// </summary>
    public sealed class Match
    {
        private readonly IRandom _random;
        private readonly List<PlayerState> _players;
        private List<PlayerState> _turnOrder;

        public Board Board { get; }
        public IReadOnlyList<PlayerState> Players => _players;
        public IReadOnlyList<PlayerState> TurnOrder => _turnOrder;
        public int CrownSpaceId { get; internal set; } = -1;
        public int Round { get; private set; }
        public int TotalRounds { get; }

        public Match(Board board, IReadOnlyList<string> characterNames, IRandom random,
            int totalRounds = Rules.RoundsPerMatch)
        {
            if (characterNames.Count != Rules.PlayerCount)
                throw new ArgumentException($"A Match needs exactly {Rules.PlayerCount} Players.");

            board.Validate();
            Board = board;
            _random = random;
            TotalRounds = totalRounds;
            _players = characterNames
                .Select((name, seat) => new PlayerState(seat, name, board.StartSpaceId))
                .ToList();
            _turnOrder = new List<PlayerState>(_players);
        }

        /// <summary>The Spaces this Player could retrace when moving backward, newest last.</summary>
        public IReadOnlyList<int> TrailOf(PlayerState player) => player.Trail;

        public IEnumerable<MatchEvent> Run()
        {
            CrownSpaceId = PickCrownSpace(excludeNearStart: true);
            yield return new CrownMoved(-1, CrownSpaceId);

            yield return RollTurnOrder();

            for (Round = 1; Round <= TotalRounds; Round++)
            {
                yield return new RoundStarted(Round);

                foreach (PlayerState player in _turnOrder)
                {
                    foreach (MatchEvent e in PlayTurn(player))
                        yield return e;
                }

                var minigame = new MinigameRequest(_players);
                yield return minigame;
                Require(minigame);
                yield return PayMinigame(minigame.Ranking);
            }

            Round = TotalRounds;
            yield return new MatchEnded(Standings());
        }

        private TurnOrderRolled RollTurnOrder()
        {
            var rolls = _players
                .Select(p => new { Player = p, Roll = Dice.Roll(DieKind.Safe, _random), TieBreak = _random.NextDouble() })
                .OrderByDescending(r => r.Roll)
                .ThenByDescending(r => r.TieBreak)
                .ToList();

            _turnOrder = rolls.Select(r => r.Player).ToList();
            return new TurnOrderRolled(rolls.Select(r => new KeyValuePair<PlayerState, int>(r.Player, r.Roll)).ToList());
        }

        internal IEnumerable<MatchEvent> PlayTurn(PlayerState player)
        {
            yield return new TurnStarted(player);

            var chooseDie = new ChooseDieRequest(player);
            yield return chooseDie;
            Require(chooseDie);

            int roll = Dice.Roll(chooseDie.Answer, _random);
            yield return new DieRolled(player, chooseDie.Answer, roll);

            bool backward = roll < 0;
            int steps = Math.Abs(roll);
            bool stoppedOnCrownSpace = false;

            for (int i = 0; i < steps; i++)
            {
                int from = player.SpaceId;
                int to;

                if (backward)
                {
                    if (player.Trail.Count > 0)
                    {
                        to = player.Trail[player.Trail.Count - 1];
                        player.Trail.RemoveAt(player.Trail.Count - 1);
                    }
                    else
                    {
                        to = Board[from].DefaultPrevious;
                    }
                }
                else
                {
                    Space space = Board[from];
                    if (space.IsFork)
                    {
                        var chooseFork = new ChooseForkRequest(player, space);
                        yield return chooseFork;
                        Require(chooseFork);
                        if (!space.Next.Contains(chooseFork.Answer))
                            throw new InvalidOperationException($"Space {chooseFork.Answer} is not a path out of fork {space.Id}.");
                        to = chooseFork.Answer;
                    }
                    else
                    {
                        to = space.Next[0];
                    }
                    player.Trail.Add(from);
                }

                player.SpaceId = to;
                int stepsLeft = steps - i - 1;
                yield return new PlayerStepped(player, from, to, backward, stepsLeft);

                // Passing or stopping on the Crown Space, in either direction, lets you buy a Crown.
                if (to == CrownSpaceId)
                {
                    if (stepsLeft == 0) stoppedOnCrownSpace = true;
                    if (player.Coins >= Rules.CrownCost)
                    {
                        var buy = new BuyCrownRequest(player);
                        yield return buy;
                        Require(buy);
                        if (buy.Answer)
                        {
                            player.Coins -= Rules.CrownCost;
                            player.Crowns++;
                            yield return new CrownBought(player);
                            yield return MoveCrown();
                        }
                    }
                }
            }

            // Stopping on the Crown Space replaces the Space's own effect.
            if (stoppedOnCrownSpace)
                yield break;

            foreach (MatchEvent e in ResolveSpace(player))
                yield return e;
        }

        private IEnumerable<MatchEvent> ResolveSpace(PlayerState player)
        {
            Space space = Board[player.SpaceId];
            switch (space.Type)
            {
                case SpaceType.Gain:
                    player.AddCoins(Rules.GainSpaceCoins);
                    yield return new SpaceResolved(player, space, Rules.GainSpaceCoins);
                    break;

                case SpaceType.Lose:
                    int lost = player.LoseCoins(Rules.LoseSpaceCoins);
                    yield return new SpaceResolved(player, space, -lost);
                    break;

                case SpaceType.Event:
                    var kind = (EventKind)_random.Next(3);
                    yield return new EventTriggered(player, kind);
                    foreach (MatchEvent e in ResolveEvent(player, kind))
                        yield return e;
                    break;
            }
        }

        private IEnumerable<MatchEvent> ResolveEvent(PlayerState player, EventKind kind)
        {
            switch (kind)
            {
                case EventKind.TideShift:
                    yield return MoveCrown();
                    break;

                case EventKind.CoconutDrop:
                    foreach (PlayerState p in _players)
                        yield return new CoinsLost(p, p.LoseCoins(Rules.CoconutDropCoins));
                    break;

                case EventKind.SwapCurrent:
                    var others = _players.Where(p => p != player).ToList();
                    PlayerState other = others[_random.Next(others.Count)];

                    int space = player.SpaceId;
                    player.SpaceId = other.SpaceId;
                    other.SpaceId = space;

                    // Swap trails too, so moving backward later retraces the path to the new spot.
                    var trail = new List<int>(player.Trail);
                    player.Trail.Clear();
                    player.Trail.AddRange(other.Trail);
                    other.Trail.Clear();
                    other.Trail.AddRange(trail);

                    yield return new PlayersSwapped(player, other);
                    break;
            }
        }

        private CrownMoved MoveCrown()
        {
            int from = CrownSpaceId;
            CrownSpaceId = PickCrownSpace(excludeNearStart: false);
            return new CrownMoved(from, CrownSpaceId);
        }

        /// <summary>A random Gain Space that isn't where the Crown already is and has nobody standing on it.</summary>
        private int PickCrownSpace(bool excludeNearStart)
        {
            var candidates = Board.Spaces
                .Where(s => s.Type == SpaceType.Gain)
                .Where(s => s.Id != CrownSpaceId)
                .Where(s => _players.All(p => p.SpaceId != s.Id))
                .Where(s => !excludeNearStart || Board.ForwardDistance(Board.StartSpaceId, s.Id) >= 6)
                .ToList();
            return candidates[_random.Next(candidates.Count)].Id;
        }

        internal MinigamePaid PayMinigame(IReadOnlyList<PlayerState> ranking)
        {
            if (ranking.Count != _players.Count || ranking.Distinct().Count() != _players.Count)
                throw new InvalidOperationException("A Minigame ranking must list every Player exactly once.");

            for (int place = 0; place < ranking.Count; place++)
                ranking[place].AddCoins(Rules.MinigamePayouts[place]);

            return new MinigamePaid(ranking);
        }

        /// <summary>Most Crowns first, then most Coins. Players tied on both share a place.</summary>
        public IReadOnlyList<Standing> Standings()
        {
            var sorted = _players
                .OrderByDescending(p => p.Crowns)
                .ThenByDescending(p => p.Coins)
                .ToList();

            return sorted
                .Select(p => new Standing(p,
                    1 + sorted.Count(o => o.Crowns > p.Crowns || (o.Crowns == p.Crowns && o.Coins > p.Coins))))
                .ToList();
        }

        private static void Require(DecisionRequest request)
        {
            if (!request.IsAnswered)
                throw new InvalidOperationException($"{request.GetType().Name} was not answered before continuing the Match.");
        }
    }
}
