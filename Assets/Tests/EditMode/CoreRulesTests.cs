using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RemakeParty.Core.Boards;
using RemakeParty.Core.Cpu;
using RemakeParty.Core.Minigames;

namespace RemakeParty.Core.Tests
{
    /// <summary>Returns queued numbers in order, so a test can force exact dice faces.</summary>
    internal sealed class ScriptedRandom : IRandom
    {
        private readonly Queue<int> _ints;
        public ScriptedRandom(params int[] ints) => _ints = new Queue<int>(ints);
        public int Next(int maxExclusive) => _ints.Count > 0 ? _ints.Dequeue() % maxExclusive : 0;
        public double NextDouble() => 0.5;
    }

    public class CoreRulesTests
    {
        private static readonly string[] Cast = { "Red", "Blue", "Green", "Yellow" };

        // Risky Die faces are { -3, -1, 4, 6, 8, 10 }, so index 1 rolls -1 and index 0 rolls -3.
        private const int RiskyMinusOne = 1;
        private const int RiskyMinusThree = 0;

        [Test]
        public void TropicalBoard_HasFortySpacesAndTwoForks()
        {
            Board board = TropicalBoard.Create();

            Assert.AreEqual(40, board.Spaces.Count);
            Assert.AreEqual(2, board.Spaces.Count(s => s.IsFork));
        }

        [Test]
        public void TropicalBoard_EverySpaceCanReachEverySpace()
        {
            Board board = TropicalBoard.Create();

            foreach (Space a in board.Spaces)
                Assert.GreaterOrEqual(board.ForwardDistance(a.Id, board.StartSpaceId), 0, $"Space {a.Id} cannot get back to Start.");
        }

        [Test]
        public void MovingBackward_RetracesThePlayersOwnPath()
        {
            Board board = TropicalBoard.Create();
            var trail = new List<int> { 3, 4, 28, 29 }; // came in through the Jungle

            List<int> path = MovePlanner.Plan(board, 30, trail, -3, _ => -1);

            CollectionAssert.AreEqual(new[] { 29, 28, 4 }, path);
        }

        [Test]
        public void MovingBackward_WithNoTrail_UsesTheBoardsWayBack()
        {
            Board board = TropicalBoard.Create();

            List<int> path = MovePlanner.Plan(board, board.StartSpaceId, new List<int>(), -3, _ => -1);

            CollectionAssert.AreEqual(new[] { 27, 26, 25 }, path);
        }

        [Test]
        public void PassingTheCrownSpaceBackward_OffersACrown()
        {
            Board board = TropicalBoard.Create();
            var match = new Match(board, Cast, new ScriptedRandom(RiskyMinusThree));
            PlayerState player = match.Players[0];
            player.SpaceId = 11;
            player.Trail.AddRange(new[] { 8, 9, 10 });
            player.Coins = 25;
            match.CrownSpaceId = 10;

            var events = new List<MatchEvent>();
            foreach (MatchEvent e in match.PlayTurn(player))
            {
                events.Add(e);
                if (e is ChooseDieRequest die) die.Answer = DieKind.Risky;
                if (e is BuyCrownRequest buy) buy.Answer = true;
            }

            Assert.IsTrue(events.OfType<CrownBought>().Any(), "Passing the Crown Space backward should allow buying a Crown.");
            Assert.AreEqual(1, player.Crowns);
            Assert.AreEqual(8, player.SpaceId);
            Assert.AreEqual(25 - Rules.CrownCost + Rules.GainSpaceCoins, player.Coins); // then lands on Gain Space 8
        }

        [Test]
        public void StoppingOnTheCrownSpace_DoesNotAlsoTriggerTheSpace()
        {
            Board board = TropicalBoard.Create();
            var match = new Match(board, Cast, new ScriptedRandom(RiskyMinusOne));
            PlayerState player = match.Players[0];
            player.SpaceId = 10;
            player.Trail.Add(9);
            player.Coins = 5; // can't afford a Crown
            match.CrownSpaceId = 9;

            var events = new List<MatchEvent>();
            foreach (MatchEvent e in match.PlayTurn(player))
            {
                events.Add(e);
                if (e is ChooseDieRequest die) die.Answer = DieKind.Risky;
            }

            Assert.IsFalse(events.OfType<BuyCrownRequest>().Any(), "No Crown offer without 20 Coins.");
            Assert.IsFalse(events.OfType<SpaceResolved>().Any());
            Assert.AreEqual(5, player.Coins);
        }

        [Test]
        public void LoseSpace_NeverTakesCoinsBelowZero()
        {
            Board board = TropicalBoard.Create();
            // Safe Die index 1 rolls 2: Start (0) -> 1 -> 2, and Space 2 is a Lose Space.
            var match = new Match(board, Cast, new ScriptedRandom(1));
            PlayerState player = match.Players[0];
            player.Coins = 2;
            match.CrownSpaceId = 13;

            foreach (MatchEvent e in match.PlayTurn(player))
                if (e is ChooseDieRequest die) die.Answer = DieKind.Safe;

            Assert.AreEqual(SpaceType.Lose, board[2].Type);
            Assert.AreEqual(0, player.Coins);
        }

        [Test]
        public void MinigamePayout_Is15_8_4_0()
        {
            var match = new Match(TropicalBoard.Create(), Cast, new SeededRandom(1));
            var ranking = match.Players.Reverse().ToList();

            match.PayMinigame(ranking);

            CollectionAssert.AreEqual(new[] { 10 + 0, 10 + 4, 10 + 8, 10 + 15 }, match.Players.Select(p => p.Coins));
        }

        [Test]
        public void Standings_TiedOnCrownsAndCoins_SharePlace()
        {
            var match = new Match(TropicalBoard.Create(), Cast, new SeededRandom(1));
            match.Players[0].Crowns = 2;
            match.Players[1].Crowns = 1; match.Players[1].Coins = 30;
            match.Players[2].Crowns = 1; match.Players[2].Coins = 30;
            match.Players[3].Crowns = 1; match.Players[3].Coins = 5;

            Dictionary<string, int> places = match.Standings().ToDictionary(s => s.Player.CharacterName, s => s.Place);

            Assert.AreEqual(1, places["Red"]);
            Assert.AreEqual(2, places["Blue"]);
            Assert.AreEqual(2, places["Green"]);
            Assert.AreEqual(4, places["Yellow"]);
        }

        [Test]
        public void FullMatch_WithFourCpuPlayers_RunsToTheEnd([Range(1, 50)] int seed)
        {
            var random = new SeededRandom(seed);
            var match = new Match(TropicalBoard.Create(), Cast, random);
            var brains = new[] { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard, Difficulty.Hard }
                .Select(d => new CpuBrain(d, random))
                .ToArray();

            MatchEnded ended = null;
            foreach (MatchEvent e in match.Run())
            {
                if (e is MinigameRequest minigame)
                    minigame.Ranking = PlaceholderMinigame.Rank(minigame.Players, p => brains[p.Seat].Difficulty, random);
                else if (e is DecisionRequest request)
                    brains[request.Player.Seat].Answer(request, match);
                else if (e is MatchEnded end)
                    ended = end;
            }

            Assert.IsNotNull(ended);
            Assert.AreEqual(Rules.PlayerCount, ended.Standings.Count);
            Assert.IsTrue(match.Players.All(p => p.Coins >= 0));
            Assert.AreEqual(1, ended.Standings[0].Place);
        }
    }
}
