using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cubotris.Gameplay
{
    public sealed class BoardState
    {
        private readonly TetrominoType?[,] cells;
        public int Width { get; }
        public int Height { get; }
        public int Version { get; private set; }
        public TetrominoType? this[int x, int y] => cells[x, y];

        public BoardState(int width = 10, int height = 20)
        {
            if (width < 4 || height < 4) throw new ArgumentOutOfRangeException(nameof(width));
            Width = width;
            Height = height;
            cells = new TetrominoType?[width, height];
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
            if (!CanPlace(positions)) return false;
            foreach (var cell in positions) cells[cell.x, cell.y] = type;
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
                    for (int x = 0; x < Width; x++) cells[x, destination] = cells[x, source];
                destination++;
            }
            int cleared = Height - destination;
            for (int y = destination; y < Height; y++)
                for (int x = 0; x < Width; x++) cells[x, y] = null;
            if (cleared > 0) Version++;
            return cleared;
        }
    }
}
