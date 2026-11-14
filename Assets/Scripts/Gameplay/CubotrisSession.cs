using System;
using System.Collections.Generic;

namespace Cubotris.Gameplay
{
    /// <summary>Four persistent boards, one active piece, one bag and shared progression.</summary>
    public sealed class CubotrisSession
    {
        public const int FaceCount = 4;
        public IReadOnlyList<BoardState> Faces { get; }
        public int ActiveFace { get; private set; }
        public TetrisGame Game { get; }
        public double SurvivalSeconds { get; private set; }
        private readonly double[] elapsed = new double[FaceCount];
        private readonly double[] intervals = new double[FaceCount];
        private readonly int[] pendingRows = new int[FaceCount];
        private readonly Random garbageRandom;

        public CubotrisSession(IPieceSource source = null, FallSettings settings = null, GarbageSettings garbageSettings = null, int? garbageSeed = null)
        {
            garbageSettings ??= new GarbageSettings();
            garbageRandom = garbageSeed.HasValue ? new Random(garbageSeed.Value) : new Random();
            var boards = new BoardState[FaceCount];
            for (int i = 0; i < boards.Length; i++)
            {
                boards[i] = new BoardState();
                intervals[i] = garbageSettings.IntervalFor(i);
            }
            Faces = Array.AsReadOnly(boards);
            Game = new TetrisGame(boards[0], source, settings, AdvanceFace, ApplyPendingGarbage);
        }

        public double FaceElapsedSeconds(int face) => elapsed[face];
        public double SecondsUntilGarbage(int face) => face == 0 ? 0 : intervals[face] - elapsed[face];
        public int PendingGarbageRows(int face) => pendingRows[face];

        // The controller supplies only playable time, once per frame, before locking or gravity.
        public void AdvanceActiveTime(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds) || Game.IsGameOver || !Game.HasActivePiece) return;
            SurvivalSeconds += seconds;
            if (ActiveFace == 0) return;
            double total = elapsed[ActiveFace] + seconds;
            double completed = Math.Floor(total / intervals[ActiveFace]);
            elapsed[ActiveFace] = total % intervals[ActiveFace];
            pendingRows[ActiveFace] = (int)Math.Min(int.MaxValue, pendingRows[ActiveFace] + completed * ActiveFace);
        }

        private bool ApplyPendingGarbage()
        {
            int rows = pendingRows[ActiveFace];
            if (rows == 0) return true;
            pendingRows[ActiveFace] = 0;
            var board = Faces[ActiveFace];
            if (rows > board.Height) return false;
            var holes = new int[rows];
            for (int i = 0; i < rows; i++)
            {
                // Use a separate random source; garbage must not consume or reshuffle the seven-bag.
                int hole = garbageRandom.Next(i == 0 ? board.Width : board.Width - 1);
                if (i > 0 && hole >= holes[i - 1]) hole++;
                holes[i] = hole;
            }
            return board.TryPushGarbageRows(holes);
        }

        private BoardState AdvanceFace()
        {
            ActiveFace = (ActiveFace + 1) % FaceCount;
            return Faces[ActiveFace];
        }
    }
}
