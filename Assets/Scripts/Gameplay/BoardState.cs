using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cubotris.Gameplay
{
    public sealed partial class BoardState
    {
        private readonly TetrominoType?[,] cells;
        private readonly Color?[,] blockColors;
        public int Width { get; }
        public int Height { get; }
        public int Version { get; private set; }
        public TetrominoType? this[int x, int y] => cells[x, y];
        public Color? BlockColorAt(int x, int y) => blockColors[x, y];

        public BoardState(int width = 10, int height = 20)
        {
            if (width < 4 || height < 4) throw new ArgumentOutOfRangeException(nameof(width));
            Width = width;
            Height = height;
            cells = new TetrominoType?[width, height];
            blockColors = new Color?[width, height];
        }

        public bool CanPlace(IReadOnlyList<Vector2Int> positions)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                var cell = positions[i];
                if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height || cells[cell.x, cell.y].HasValue)
                    return false;
            }
            return true;
        }

        public bool TryLock(TetrominoType type, IReadOnlyList<Vector2Int> positions)
        {
            return TryLock(type, positions, Color.white);
        }

        public bool TryLock(TetrominoType type, IReadOnlyList<Vector2Int> positions, Color color)
        {
            if (!CanPlace(positions)) return false;
            foreach (var cell in positions)
            {
                cells[cell.x, cell.y] = type;
                blockColors[cell.x, cell.y] = color;
            }
            Version++;
            return true;
        }

        /// <summary>All rows are inserted atomically; overflow leaves every existing cell intact.</summary>
        public bool TryPushGarbageRows(IReadOnlyList<int> holes)
        {
            int count = holes.Count;
            if (count == 0) return true;
            if (count > Height) return false;
            foreach (int hole in holes)
                if (hole < 0 || hole >= Width) throw new ArgumentOutOfRangeException(nameof(holes));
            for (int y = Height - count; y < Height; y++)
                for (int x = 0; x < Width; x++) if (cells[x, y].HasValue) return false;
            for (int y = Height - 1; y >= count; y--)
                for (int x = 0; x < Width; x++)
                {
                    cells[x, y] = cells[x, y - count];
                    blockColors[x, y] = blockColors[x, y - count];
                }
            for (int y = 0; y < count; y++)
                for (int x = 0; x < Width; x++)
                {
                    cells[x, y] = x == holes[y] ? null : TetrominoType.J;
                    blockColors[x, y] = x == holes[y] ? null : Color.white;
                }
            Version++;
            return true;
        }

        public int ClearFullRows()
        {
            int destination = 0;
            for (int source = 0; source < Height; source++)
            {
                bool full = true;
                for (int x = 0; x < Width; x++) full &= cells[x, source].HasValue;
                if (full) continue;
                if (destination != source)
                    for (int x = 0; x < Width; x++)
                    {
                        cells[x, destination] = cells[x, source];
                        blockColors[x, destination] = blockColors[x, source];
                    }
                destination++;
            }
            int cleared = Height - destination;
            for (int y = destination; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    cells[x, y] = null;
                    blockColors[x, y] = null;
                }
            if (cleared > 0) Version++;
            return cleared;
        }
    }
}
