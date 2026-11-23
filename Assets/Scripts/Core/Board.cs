using System;
using System.Collections.Generic;

namespace RemakeParty.Core
{
    public enum SpaceType
    {
        Gain,
        Lose,
        Event,
    }

    public sealed class Space
    {
        public int Id { get; }
        public SpaceType Type { get; }

        /// <summary>Where the Space sits on the ground plane. Used only for display.</summary>
        public float X { get; }
        public float Z { get; }

        /// <summary>Spaces reachable by moving forward. More than one means this Space is a fork.</summary>
        public IReadOnlyList<int> Next => _next;

        /// <summary>
        /// The Space a Player steps back to when they have no path of their own to retrace
        /// (for example, a Negative Roll on the very first Turn).
        /// </summary>
        public int DefaultPrevious { get; internal set; } = -1;

        /// <summary>A short label for the path this Space belongs to, e.g. "Beach". Display only.</summary>
        public string PathName { get; }

        internal readonly List<int> _next = new List<int>();

        public Space(int id, SpaceType type, float x, float z, string pathName)
        {
            Id = id;
            Type = type;
            X = x;
            Z = z;
            PathName = pathName;
        }

        public bool IsFork => _next.Count > 1;
    }

    public sealed class Board
    {
        private readonly List<Space> _spaces = new List<Space>();

        public string Name { get; }
        public int StartSpaceId { get; set; }
        public IReadOnlyList<Space> Spaces => _spaces;

        public Board(string name) => Name = name;

        public Space this[int id] => _spaces[id];

        public Space AddSpace(SpaceType type, float x, float z, string pathName)
        {
            var space = new Space(_spaces.Count, type, x, z, pathName);
            _spaces.Add(space);
            return space;
        }

        /// <summary>Connects two Spaces forward. The first link into a Space also becomes its DefaultPrevious.</summary>
        public void Link(int from, int to)
        {
            _spaces[from]._next.Add(to);
            if (_spaces[to].DefaultPrevious < 0)
                _spaces[to].DefaultPrevious = from;
        }

        /// <summary>
        /// Fewest forward steps from one Space to another, or -1 if unreachable.
        /// Used by CPU Players to plan which fork leads to the Crown Space.
        /// </summary>
        public int ForwardDistance(int from, int to)
        {
            if (from == to) return 0;
            var distance = new int[_spaces.Count];
            for (int i = 0; i < distance.Length; i++) distance[i] = -1;
            var queue = new Queue<int>();
            distance[from] = 0;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int next in _spaces[current].Next)
                {
                    if (distance[next] >= 0) continue;
                    distance[next] = distance[current] + 1;
                    if (next == to) return distance[next];
                    queue.Enqueue(next);
                }
            }
            return -1;
        }

        /// <summary>Throws if the Board has a dead end or a Space nobody can step back from.</summary>
        public void Validate()
        {
            foreach (var space in _spaces)
            {
                if (space.Next.Count == 0)
                    throw new InvalidOperationException($"Space {space.Id} has no forward link (dead end).");
                if (space.DefaultPrevious < 0)
                    throw new InvalidOperationException($"Space {space.Id} has nothing linking into it.");
            }
        }
    }
}
