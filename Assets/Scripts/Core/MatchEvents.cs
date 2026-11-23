using System.Collections.Generic;

namespace RemakeParty.Core
{
    /// <summary>
    /// Something that happened in a Match. The screen reads these one at a time and animates each one.
    /// </summary>
    public abstract class MatchEvent
    {
    }

    /// <summary>
    /// A question the Match needs answered before it can continue.
    /// Whoever controls the Player (a CPU Player now, a person later) must set the answer.
    /// </summary>
    public abstract class DecisionRequest : MatchEvent
    {
        public PlayerState Player { get; }
        public bool IsAnswered { get; protected set; }

        protected DecisionRequest(PlayerState player) => Player = player;
    }

    public sealed class ChooseDieRequest : DecisionRequest
    {
        private DieKind _answer;

        public ChooseDieRequest(PlayerState player) : base(player) { }

        public DieKind Answer
        {
            get => _answer;
            set { _answer = value; IsAnswered = true; }
        }
    }

    public sealed class ChooseForkRequest : DecisionRequest
    {
        private int _answer;

        public Space Fork { get; }

        public ChooseForkRequest(PlayerState player, Space fork) : base(player) => Fork = fork;

        /// <summary>The id of the next Space to walk to. Must be one of Fork.Next.</summary>
        public int Answer
        {
            get => _answer;
            set { _answer = value; IsAnswered = true; }
        }
    }

    public sealed class BuyCrownRequest : DecisionRequest
    {
        private bool _answer;

        public BuyCrownRequest(PlayerState player) : base(player) { }

        public bool Answer
        {
            get => _answer;
            set { _answer = value; IsAnswered = true; }
        }
    }

    /// <summary>Asks for the result of the Minigame. Answer is the Players from 1st place to last.</summary>
    public sealed class MinigameRequest : DecisionRequest
    {
        private IReadOnlyList<PlayerState> _ranking;

        public IReadOnlyList<PlayerState> Players { get; }

        public MinigameRequest(IReadOnlyList<PlayerState> players) : base(null) => Players = players;

        public IReadOnlyList<PlayerState> Ranking
        {
            get => _ranking;
            set { _ranking = value; IsAnswered = true; }
        }
    }

    public sealed class TurnOrderRolled : MatchEvent
    {
        /// <summary>Each Player's Turn-Order Roll, in the order they will take Turns.</summary>
        public IReadOnlyList<KeyValuePair<PlayerState, int>> Rolls { get; }

        public TurnOrderRolled(IReadOnlyList<KeyValuePair<PlayerState, int>> rolls) => Rolls = rolls;
    }

    public sealed class RoundStarted : MatchEvent
    {
        public int Round { get; }
        public RoundStarted(int round) => Round = round;
    }

    public sealed class TurnStarted : MatchEvent
    {
        public PlayerState Player { get; }
        public TurnStarted(PlayerState player) => Player = player;
    }

    public sealed class DieRolled : MatchEvent
    {
        public PlayerState Player { get; }
        public DieKind Die { get; }
        public int Value { get; }

        public DieRolled(PlayerState player, DieKind die, int value)
        {
            Player = player;
            Die = die;
            Value = value;
        }
    }

    public sealed class PlayerStepped : MatchEvent
    {
        public PlayerState Player { get; }
        public int FromSpaceId { get; }
        public int ToSpaceId { get; }
        public bool Backward { get; }
        public int StepsLeft { get; }

        public PlayerStepped(PlayerState player, int from, int to, bool backward, int stepsLeft)
        {
            Player = player;
            FromSpaceId = from;
            ToSpaceId = to;
            Backward = backward;
            StepsLeft = stepsLeft;
        }
    }

    public sealed class CrownBought : MatchEvent
    {
        public PlayerState Player { get; }
        public CrownBought(PlayerState player) => Player = player;
    }

    public sealed class CrownMoved : MatchEvent
    {
        public int FromSpaceId { get; }
        public int ToSpaceId { get; }

        public CrownMoved(int from, int to)
        {
            FromSpaceId = from;
            ToSpaceId = to;
        }
    }

    /// <summary>A Player stopped on a Gain or Lose Space and their Coins changed.</summary>
    public sealed class SpaceResolved : MatchEvent
    {
        public PlayerState Player { get; }
        public Space Space { get; }
        public int CoinChange { get; }

        public SpaceResolved(PlayerState player, Space space, int coinChange)
        {
            Player = player;
            Space = space;
            CoinChange = coinChange;
        }
    }

    public enum EventKind
    {
        TideShift,
        CoconutDrop,
        SwapCurrent,
    }

    public sealed class EventTriggered : MatchEvent
    {
        public PlayerState Player { get; }
        public EventKind Kind { get; }

        public EventTriggered(PlayerState player, EventKind kind)
        {
            Player = player;
            Kind = kind;
        }
    }

    public sealed class CoinsLost : MatchEvent
    {
        public PlayerState Player { get; }
        public int Amount { get; }

        public CoinsLost(PlayerState player, int amount)
        {
            Player = player;
            Amount = amount;
        }
    }

    public sealed class PlayersSwapped : MatchEvent
    {
        public PlayerState A { get; }
        public PlayerState B { get; }

        public PlayersSwapped(PlayerState a, PlayerState b)
        {
            A = a;
            B = b;
        }
    }

    public sealed class MinigamePaid : MatchEvent
    {
        public IReadOnlyList<PlayerState> Ranking { get; }
        public MinigamePaid(IReadOnlyList<PlayerState> ranking) => Ranking = ranking;
    }

    public sealed class Standing
    {
        public PlayerState Player { get; }

        /// <summary>1 is first. Players tied on both Crowns and Coins share a place.</summary>
        public int Place { get; }

        public Standing(PlayerState player, int place)
        {
            Player = player;
            Place = place;
        }
    }

    public sealed class MatchEnded : MatchEvent
    {
        public IReadOnlyList<Standing> Standings { get; }
        public MatchEnded(IReadOnlyList<Standing> standings) => Standings = standings;
    }
}
