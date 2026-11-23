namespace RemakeParty.Core
{
    public enum DieKind
    {
        Safe,
        Risky,
    }

    public static class Dice
    {
        /// <summary>Safe Die: always moves forward. Average 3.5.</summary>
        public static readonly int[] SafeFaces = { 1, 2, 3, 4, 5, 6 };

        /// <summary>Risky Die: 1-in-3 chance to move backward. Average 4.</summary>
        public static readonly int[] RiskyFaces = { -3, -1, 4, 6, 8, 10 };

        public static int[] Faces(DieKind kind) => kind == DieKind.Safe ? SafeFaces : RiskyFaces;

        public static int Roll(DieKind kind, IRandom random)
        {
            int[] faces = Faces(kind);
            return faces[random.Next(faces.Length)];
        }
    }
}
