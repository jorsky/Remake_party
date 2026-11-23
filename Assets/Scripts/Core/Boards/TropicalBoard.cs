using System;

namespace RemakeParty.Core.Boards
{
    /// <summary>
    /// The first Board: a tropical island loop of 40 Spaces with two forks.
    ///
    /// Fork 1: the short Beach path (3 Spaces, mostly Lose) or the long Jungle path (6 Spaces, mostly Gain).
    /// Fork 2: the short Ridge path (3 Spaces, risky) or the long Lagoon path (6 Spaces, safe).
    /// </summary>
    public static class TropicalBoard
    {
        // One letter per Space: G = Gain, L = Lose, E = Event.
        private const string Ring = "GGLGGLGLGEGGLGEGGLGLELGGELGG"; // 28 Spaces around the island
        private const string Jungle = "GGEGGG";
        private const string Lagoon = "GGGLGG";

        private const int Fork1 = 4, Rejoin1 = 8;   // Ring 5-7 is the Beach
        private const int Fork2 = 18, Rejoin2 = 22; // Ring 19-21 is the Ridge

        private const float RadiusX = 14f, RadiusZ = 9f;
        private const float BranchRadiusScale = 1.4f;

        public static Board Create()
        {
            var board = new Board("Tropical Island");
            int ringCount = Ring.Length;

            for (int i = 0; i < ringCount; i++)
            {
                (float x, float z) = OnEllipse(RingAngle(i, ringCount), 1f);
                board.AddSpace(Parse(Ring[i]), x, z, RingPathName(i));
            }

            // Link the ring first so every ring Space's DefaultPrevious is its ring neighbour.
            for (int i = 0; i < ringCount; i++)
                board.Link(i, (i + 1) % ringCount);

            AddBranch(board, Jungle, "Jungle", Fork1, Rejoin1, ringCount);
            AddBranch(board, Lagoon, "Lagoon", Fork2, Rejoin2, ringCount);

            board.StartSpaceId = 0;
            board.Validate();
            return board;
        }

        private static void AddBranch(Board board, string types, string pathName, int fork, int rejoin, int ringCount)
        {
            float startAngle = RingAngle(fork, ringCount);
            float endAngle = RingAngle(rejoin, ringCount);
            int previous = fork;

            for (int i = 0; i < types.Length; i++)
            {
                float t = (i + 1f) / (types.Length + 1f);
                (float x, float z) = OnEllipse(startAngle + (endAngle - startAngle) * t, BranchRadiusScale);
                Space space = board.AddSpace(Parse(types[i]), x, z, pathName);
                board.Link(previous, space.Id);
                previous = space.Id;
            }

            board.Link(previous, rejoin);
        }

        private static string RingPathName(int i)
        {
            if (i > Fork1 && i < Rejoin1) return "Beach";
            if (i > Fork2 && i < Rejoin2) return "Ridge";
            return "Shore";
        }

        private static float RingAngle(int index, int count) => (float)(2 * Math.PI * index / count);

        private static (float x, float z) OnEllipse(float angle, float scale) =>
            ((float)Math.Cos(angle) * RadiusX * scale, (float)Math.Sin(angle) * RadiusZ * scale);

        private static SpaceType Parse(char c)
        {
            switch (c)
            {
                case 'G': return SpaceType.Gain;
                case 'L': return SpaceType.Lose;
                case 'E': return SpaceType.Event;
                default: throw new ArgumentException($"Unknown Space letter '{c}'.");
            }
        }
    }
}
