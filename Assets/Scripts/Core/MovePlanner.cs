using System;
using System.Collections.Generic;

namespace RemakeParty.Core
{
    /// <summary>
    /// Works out which Spaces a Roll walks through, without changing anything.
    /// The Match uses it to move Players; CPU Players use it to preview a Roll before choosing a die.
    /// </summary>
    public static class MovePlanner
    {
        /// <param name="chooseFork">
        /// Called at each fork when moving forward. Receives the fork Space and returns the chosen next Space id.
        /// </param>
        /// <returns>Every Space entered, in order. The last one is where the Player stops.</returns>
        public static List<int> Plan(Board board, int startSpaceId, IReadOnlyList<int> trail, int roll,
            Func<Space, int> chooseFork)
        {
            var path = new List<int>();
            int current = startSpaceId;

            if (roll >= 0)
            {
                for (int i = 0; i < roll; i++)
                {
                    Space space = board[current];
                    current = space.IsFork ? chooseFork(space) : space.Next[0];
                    path.Add(current);
                }
            }
            else
            {
                // Retrace the Player's own path; once it runs out, fall back to the Board's default way back.
                int trailIndex = trail.Count - 1;
                for (int i = 0; i < -roll; i++)
                {
                    current = trailIndex >= 0 ? trail[trailIndex--] : board[current].DefaultPrevious;
                    path.Add(current);
                }
            }

            return path;
        }
    }
}
