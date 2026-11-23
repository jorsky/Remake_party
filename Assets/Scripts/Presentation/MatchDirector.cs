using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RemakeParty.Core;
using RemakeParty.Core.Boards;
using RemakeParty.Core.Cpu;
using RemakeParty.Core.Minigames;
using UnityEngine;
using UnityEngine.SceneManagement;
using BoardSpace = RemakeParty.Core.Space;

namespace RemakeParty.Presentation
{
    /// <summary>
    /// Grey-box Match: builds the Tropical Board out of simple shapes and plays a full Match
    /// with 4 CPU Players, animating every event. Add this to an empty GameObject and press Play.
    /// </summary>
    public sealed class MatchDirector : MonoBehaviour
    {
        [Tooltip("Difficulty of each of the 4 CPU Players.")]
        [SerializeField] private Difficulty[] seatDifficulties =
            { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard, Difficulty.Normal };

        [Tooltip("0 = a different Match every time. Any other number replays the same Match.")]
        [SerializeField] private int seed;

        [Tooltip("Animation speed. Can also be changed during play with the slider.")]
        [SerializeField, Range(0.25f, 8f)] private float speed = 1f;

        // Placeholder cast until the real Characters are designed.
        private static readonly string[] Cast = { "Red", "Blue", "Green", "Yellow" };

        private static readonly Color[] SeatColors =
        {
            new Color(0.90f, 0.25f, 0.25f),
            new Color(0.25f, 0.45f, 0.95f),
            new Color(0.30f, 0.80f, 0.35f),
            new Color(0.98f, 0.85f, 0.20f),
        };

        private static readonly Color GainColor = new Color(0.20f, 0.75f, 0.30f);
        private static readonly Color LoseColor = new Color(0.85f, 0.20f, 0.20f);
        private static readonly Color EventColor = new Color(0.60f, 0.35f, 0.85f);
        private static readonly Color SandColor = new Color(0.93f, 0.85f, 0.62f);
        private static readonly Color PathColor = new Color(0.80f, 0.70f, 0.50f);
        private static readonly Color WaterColor = new Color(0.15f, 0.55f, 0.75f);
        private static readonly Color CrownColor = new Color(1.00f, 0.80f, 0.10f);

        private const float SpaceHeight = 0.1f;
        private const float HopHeight = 1.0f;

        private Match _match;
        private IRandom _random;
        private CpuBrain[] _brains;

        private readonly List<Vector3> _spacePositions = new List<Vector3>();
        private Transform[] _pawns;
        private Transform _crown;

        private PlayerState _currentPlayer;
        private string _banner = "";
        private readonly List<string> _log = new List<string>();
        private MatchEnded _ended;
        private bool _paused;

        private void Start()
        {
            int actualSeed = seed != 0 ? seed : System.Environment.TickCount;
            _random = new SeededRandom(actualSeed);

            Board board = TropicalBoard.Create();
            _match = new Match(board, Cast, _random);
            _brains = Enumerable.Range(0, Rules.PlayerCount)
                .Select(i => new CpuBrain(DifficultyOf(i), _random))
                .ToArray();

            BuildIsland(board);
            BuildPawns(board);
            BuildCrown();
            PlaceCamera();

            AddLog($"Match seed {actualSeed}");
            StartCoroutine(RunMatch());
        }

        private Difficulty DifficultyOf(int seat) =>
            seat < seatDifficulties.Length ? seatDifficulties[seat] : Difficulty.Normal;

        // ---------------------------------------------------------------- Match flow

        private IEnumerator RunMatch()
        {
            foreach (MatchEvent e in _match.Run())
            {
                switch (e)
                {
                    case MinigameRequest minigame:
                        _currentPlayer = null;
                        _banner = "Minigame time! (placeholder)";
                        yield return Wait(1.5f);
                        minigame.Ranking = PlaceholderMinigame.Rank(minigame.Players, p => DifficultyOf(p.Seat), _random);
                        break;

                    case ChooseDieRequest die:
                        _banner = $"{die.Player} is choosing a die...";
                        yield return Wait(0.6f);
                        _brains[die.Player.Seat].Answer(die, _match);
                        AddLog($"{die.Player} picks the {die.Answer} Die");
                        break;

                    case ChooseForkRequest fork:
                        _banner = $"{fork.Player} reaches a fork...";
                        yield return Wait(0.6f);
                        _brains[fork.Player.Seat].Answer(fork, _match);
                        AddLog($"{fork.Player} takes the {_match.Board[fork.Answer].PathName} path");
                        break;

                    case BuyCrownRequest buy:
                        _brains[buy.Player.Seat].Answer(buy, _match);
                        break;

                    case CrownMoved moved:
                        yield return MoveCrown(moved);
                        break;

                    case TurnOrderRolled order:
                        foreach (KeyValuePair<PlayerState, int> roll in order.Rolls)
                            AddLog($"{roll.Key} rolls {roll.Value} for turn order");
                        _banner = "Turn order: " + string.Join(", ", order.Rolls.Select(r => r.Key.CharacterName));
                        yield return Wait(2f);
                        break;

                    case RoundStarted round:
                        _banner = $"Round {round.Round} of {_match.TotalRounds}";
                        AddLog($"--- Round {round.Round} ---");
                        yield return Wait(1.2f);
                        break;

                    case TurnStarted turn:
                        _currentPlayer = turn.Player;
                        _banner = $"{turn.Player}'s Turn";
                        yield return Wait(0.6f);
                        break;

                    case DieRolled rolled:
                        _banner = $"{rolled.Player} rolled {rolled.Value}!";
                        AddLog(rolled.Value < 0
                            ? $"{rolled.Player} rolled {rolled.Value} - moving BACK"
                            : $"{rolled.Player} rolled {rolled.Value}");
                        yield return Wait(0.8f);
                        break;

                    case PlayerStepped step:
                        yield return Hop(step.Player, step.ToSpaceId);
                        break;

                    case CrownBought bought:
                        _banner = $"{bought.Player} bought a Crown!";
                        AddLog($"{bought.Player} bought a Crown (now {bought.Player.Crowns})");
                        yield return Wait(1.2f);
                        break;

                    case SpaceResolved resolved:
                        string sign = resolved.CoinChange >= 0 ? "+" : "";
                        _banner = $"{resolved.Player}: {sign}{resolved.CoinChange} Coins";
                        AddLog($"{resolved.Player} {resolved.Space.Type} Space: {sign}{resolved.CoinChange} Coins");
                        yield return Wait(0.8f);
                        break;

                    case EventTriggered triggered:
                        _banner = $"Event: {EventName(triggered.Kind)}!";
                        AddLog($"{triggered.Player} triggered {EventName(triggered.Kind)}");
                        yield return Wait(1.2f);
                        break;

                    case CoinsLost lost:
                        if (lost.Amount > 0) AddLog($"{lost.Player} loses {lost.Amount} Coins");
                        break;

                    case PlayersSwapped swapped:
                        SnapPawn(swapped.A);
                        SnapPawn(swapped.B);
                        AddLog($"{swapped.A} and {swapped.B} swap places");
                        yield return Wait(1f);
                        break;

                    case MinigamePaid paid:
                        for (int place = 0; place < paid.Ranking.Count; place++)
                            AddLog($"Minigame #{place + 1}: {paid.Ranking[place]} +{Rules.MinigamePayouts[place]}");
                        _banner = $"{paid.Ranking[0]} wins the Minigame!";
                        yield return Wait(1.5f);
                        break;

                    case MatchEnded ended:
                        _ended = ended;
                        _currentPlayer = null;
                        _banner = "";
                        break;
                }
            }
        }

        private static string EventName(EventKind kind)
        {
            switch (kind)
            {
                case EventKind.TideShift: return "Tide Shift";
                case EventKind.CoconutDrop: return "Coconut Drop";
                default: return "Swap Current";
            }
        }

        private WaitForSeconds Wait(float seconds) => new WaitForSeconds(seconds / speed);

        private void AddLog(string line)
        {
            _log.Add(line);
            if (_log.Count > 14) _log.RemoveAt(0);
        }

        // ---------------------------------------------------------------- Animation

        private IEnumerator Hop(PlayerState player, int toSpaceId)
        {
            Transform pawn = _pawns[player.Seat];
            Vector3 from = pawn.position;
            Vector3 to = PawnPosition(player.Seat, toSpaceId);
            float duration = 0.3f / speed;

            for (float t = 0; t < 1f; t += Time.deltaTime / duration)
            {
                Vector3 p = Vector3.Lerp(from, to, t);
                p.y += Mathf.Sin(t * Mathf.PI) * HopHeight;
                pawn.position = p;
                yield return null;
            }

            pawn.position = to;
        }

        private IEnumerator MoveCrown(CrownMoved moved)
        {
            Vector3 target = _spacePositions[moved.ToSpaceId] + Vector3.up * 1.6f;
            if (moved.FromSpaceId >= 0)
            {
                _banner = "The Crown Space moves!";
                AddLog($"Crown Space moves to the {_match.Board[moved.ToSpaceId].PathName}");
                Vector3 start = _crown.position;
                float duration = 1f / speed;
                for (float t = 0; t < 1f; t += Time.deltaTime / duration)
                {
                    Vector3 p = Vector3.Lerp(start, target, t);
                    p.y += Mathf.Sin(t * Mathf.PI) * 4f;
                    _crown.position = p;
                    yield return null;
                }
            }
            _crown.position = target;
        }

        private void SnapPawn(PlayerState player) =>
            _pawns[player.Seat].position = PawnPosition(player.Seat, player.SpaceId);

        /// <summary>Players sharing a Space stand in its four corners so they don't overlap.</summary>
        private Vector3 PawnPosition(int seat, int spaceId)
        {
            float ox = (seat % 2 == 0 ? -1 : 1) * 0.3f;
            float oz = (seat < 2 ? 1 : -1) * 0.3f;
            return _spacePositions[spaceId] + new Vector3(ox, 0.5f, oz);
        }

        private void Update()
        {
            if (_crown != null)
                _crown.Rotate(0f, 90f * Time.deltaTime, 0f, UnityEngine.Space.World);

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 focus = Vector3.zero;
            if (_currentPlayer != null)
                focus = Vector3.Lerp(Vector3.zero, _pawns[_currentPlayer.Seat].position, 0.4f);

            Vector3 wanted = focus + new Vector3(0f, 26f, -22f);
            cam.transform.position = Vector3.Lerp(cam.transform.position, wanted, Time.deltaTime * 2f);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation,
                Quaternion.LookRotation(focus - wanted), Time.deltaTime * 2f);
        }

        // ---------------------------------------------------------------- Building the grey box

        private void BuildIsland(Board board)
        {
            var root = new GameObject("Board").transform;

            Make(PrimitiveType.Plane, "Water", root, new Vector3(0f, -0.3f, 0f), new Vector3(12f, 1f, 12f), WaterColor);
            Make(PrimitiveType.Cylinder, "Island", root, new Vector3(0f, -0.2f, 0f), new Vector3(46f, 0.1f, 32f), SandColor);

            foreach (BoardSpace space in board.Spaces)
                _spacePositions.Add(new Vector3(space.X, 0f, space.Z));

            foreach (BoardSpace space in board.Spaces)
            {
                Make(PrimitiveType.Cylinder, $"Space {space.Id} ({space.Type}, {space.PathName})", root,
                    _spacePositions[space.Id], new Vector3(1.4f, SpaceHeight, 1.4f), SpaceColor(space.Type));

                foreach (int next in space.Next)
                    MakePathLink(root, _spacePositions[space.Id], _spacePositions[next]);
            }
        }

        private static void MakePathLink(Transform root, Vector3 a, Vector3 b)
        {
            Vector3 mid = (a + b) / 2f + Vector3.down * 0.05f;
            Transform link = Make(PrimitiveType.Cube, "Path", root, mid,
                new Vector3(0.3f, 0.05f, Vector3.Distance(a, b)), PathColor);
            link.rotation = Quaternion.LookRotation(b - a);
        }

        private void BuildPawns(Board board)
        {
            var root = new GameObject("Players").transform;
            _pawns = new Transform[Rules.PlayerCount];
            for (int seat = 0; seat < Rules.PlayerCount; seat++)
            {
                _pawns[seat] = Make(PrimitiveType.Capsule, $"P{seat + 1} {Cast[seat]}", root,
                    PawnPosition(seat, board.StartSpaceId), new Vector3(0.5f, 0.5f, 0.5f), SeatColors[seat]);
            }
        }

        private void BuildCrown()
        {
            _crown = Make(PrimitiveType.Cube, "Crown", null, new Vector3(0f, 10f, 0f), Vector3.one * 0.7f, CrownColor);
            _crown.rotation = Quaternion.Euler(45f, 0f, 45f);
        }

        private static void PlaceCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            cam.transform.position = new Vector3(0f, 26f, -22f);
            cam.transform.LookAt(Vector3.zero);
            cam.backgroundColor = WaterColor;
        }

        private static Color SpaceColor(SpaceType type)
        {
            switch (type)
            {
                case SpaceType.Gain: return GainColor;
                case SpaceType.Lose: return LoseColor;
                default: return EventColor;
            }
        }

        private static Transform Make(PrimitiveType shape, string name, Transform parent, Vector3 position,
            Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(shape);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            Transform t = go.transform;
            t.SetParent(parent, false);
            t.position = position;
            t.localScale = scale;
            go.GetComponent<Renderer>().material.color = color;
            return t;
        }

        // ---------------------------------------------------------------- HUD (temporary, IMGUI)

        private GUIStyle _label, _bigLabel, _small;

        private void OnGUI()
        {
            if (_match == null) return;
            EnsureStyles();
            float s = Screen.height / 1080f;

            // Scoreboard, top left.
            GUILayout.BeginArea(new Rect(20 * s, 20 * s, 560 * s, 400 * s), GUI.skin.box);
            GUILayout.Label($"{_match.Board.Name}  -  Round {Mathf.Max(_match.Round, 1)} / {_match.TotalRounds}", _label);
            foreach (PlayerState p in _match.TurnOrder)
            {
                string marker = p == _currentPlayer ? "> " : "   ";
                Color previous = GUI.color;
                GUI.color = SeatColors[p.Seat];
                GUILayout.Label($"{marker}{p}  ({DifficultyOf(p.Seat)})   Crowns {p.Crowns}   Coins {p.Coins}", _label);
                GUI.color = previous;
            }

            GUILayout.Space(10 * s);
            GUILayout.Label($"Speed {speed:0.##}x", _small);
            speed = GUILayout.HorizontalSlider(speed, 0.25f, 8f);
            if (GUILayout.Button(_paused ? "Resume" : "Pause"))
            {
                _paused = !_paused;
                Time.timeScale = _paused ? 0f : 1f;
            }
            GUILayout.EndArea();

            // Event log, top right.
            GUILayout.BeginArea(new Rect(Screen.width - 600 * s, 20 * s, 580 * s, 560 * s), GUI.skin.box);
            foreach (string line in _log)
                GUILayout.Label(line, _small);
            GUILayout.EndArea();

            // Banner, bottom centre.
            if (!string.IsNullOrEmpty(_banner))
                GUI.Label(new Rect(0, Screen.height - 140 * s, Screen.width, 100 * s), _banner, _bigLabel);

            if (_ended != null)
                DrawResults(s);
        }

        private void DrawResults(float s)
        {
            float w = 700 * s, h = 460 * s;
            GUILayout.BeginArea(new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h), GUI.skin.box);
            GUILayout.Label("Results", _bigLabel);
            foreach (Standing standing in _ended.Standings)
            {
                Color previous = GUI.color;
                GUI.color = SeatColors[standing.Player.Seat];
                GUILayout.Label($"#{standing.Place}  {standing.Player}   Crowns {standing.Player.Crowns}   Coins {standing.Player.Coins}", _label);
                GUI.color = previous;
            }

            var winners = _ended.Standings.Where(st => st.Place == 1).Select(st => st.Player.ToString()).ToList();
            GUILayout.Space(10 * s);
            GUILayout.Label(winners.Count > 1 ? $"Shared win: {string.Join(" & ", winners)}!" : $"{winners[0]} wins!", _label);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Play Again", GUILayout.Height(60 * s)))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            float s = Screen.height / 1080f;
            int size = Mathf.RoundToInt(26 * s);
            if (_label != null && _label.fontSize == size) return;

            _label = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = FontStyle.Bold };
            _small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(20 * s) };
            _bigLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(48 * s),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _bigLabel.normal.textColor = Color.white;
        }
    }
}
