using UnityEngine;

namespace Cubotris.Gameplay
{
    public enum TetrominoType { I, O, T, S, Z, J, L }

    public static class TetrominoData
    {
        private static readonly Vector2Int[][] Shapes =
        {
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
            new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1) },
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 1) },
            new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) }
        };

        public static Vector2Int[] Cells(TetrominoType type, int rotation, Vector2Int position)
        {
            var result = new Vector2Int[4];
            int turns = ((rotation % 4) + 4) % 4;
            for (int i = 0; i < 4; i++)
            {
                Vector2Int cell = Shapes[(int)type][i];
                if (type != TetrominoType.O)
                {
                    // I rotates around its half-cell pivot; the other pieces use an integer pivot.
                    for (int turn = 0; turn < turns; turn++)
                        cell = type == TetrominoType.I
                            ? new Vector2Int(cell.y + 1, -cell.x)
                            : new Vector2Int(cell.y, -cell.x);
                }
                result[i] = cell + position;
            }
            return result;
        }
    }

    public readonly struct ActivePiece
    {
        public TetrominoType Type { get; }
        public int Rotation { get; }
        public Vector2Int Position { get; }
        public Vector2Int[] Cells => TetrominoData.Cells(Type, Rotation, Position);

        public ActivePiece(TetrominoType type, int rotation, Vector2Int position)
        {
            Type = type;
            Rotation = rotation;
            Position = position;
        }
    }
}
