using System;
using System.Collections.Generic;

namespace Cubotris.Gameplay
{
    /// <summary>Four persistent boards, one active piece, one bag and shared progression.</summary>
    public sealed partial class CubotrisSession
    {
        public const int FaceCount = 4;
        public IReadOnlyList<BoardState> Faces { get; }
        public int ActiveFace { get; private set; }
        public TetrisGame Game { get; }
        public double SurvivalSeconds { get; private set; }
        public DifficultyMode Mode { get; }
        public bool IsPractice { get; }
        public string RunId { get; } = Guid.NewGuid().ToString("N");
        private readonly double[] totalFaceTime = new double[FaceCount];
        private readonly int[] totalFaceLines = new int[FaceCount];
        public double TotalFaceSeconds(int face)=>totalFaceTime[face];
        public int TotalFaceLines(int face)=>totalFaceLines[face];
        public const int RotationBonus = 400;
        public int CycleClearMask { get; private set; }
        public int CompletedRotations { get; private set; }
        public int SuccessfulRotations { get; private set; }
        public int LastRotationBonus { get; private set; }
        public int CycleBonusScore => SuccessfulRotations * RotationBonus;
        private readonly double[] elapsed = new double[FaceCount];
        private readonly double[] intervals = new double[FaceCount];
        private readonly int[] pendingRows = new int[FaceCount];
        private readonly StatefulRandom garbageRandom;

        public CubotrisSession(IPieceSource source = null, FallSettings settings = null, GarbageSettings garbageSettings = null, int? garbageSeed = null, DifficultyMode mode = DifficultyMode.Normal, int practiceFace = -1)
        {
            if(!Enum.IsDefined(typeof(DifficultyMode),mode))throw new ArgumentOutOfRangeException(nameof(mode));
            if(practiceFace < -1 || practiceFace >= FaceCount)throw new ArgumentOutOfRangeException(nameof(practiceFace));
            IsPractice=practiceFace>=0;ActiveFace=IsPractice?practiceFace:0;
            Mode=mode;
            garbageSettings ??= new GarbageSettings();
            garbageRandom = new StatefulRandom(garbageSeed);
            var boards = new BoardState[FaceCount];
            for (int i = 0; i < boards.Length; i++)
            {
                boards[i] = new BoardState();
                intervals[i] = DifficultyRules.GarbageInterval(mode,garbageSettings.IntervalFor(i));
            }
            Faces = Array.AsReadOnly(boards);
            Game = new TetrisGame(boards[ActiveFace], source, DifficultyRules.Fall(mode,settings), AdvanceFace, ApplyPendingGarbage);
        }

        public double FaceElapsedSeconds(int face) => elapsed[face];
        public double SecondsUntilGarbage(int face) => face == 0 ? 0 : intervals[face] - elapsed[face];
        public int PendingGarbageRows(int face) => pendingRows[face];

        // The controller supplies only playable time, once per frame, before locking or gravity.
        public void AdvanceActiveTime(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds) || Game.IsGameOver || !Game.HasActivePiece) return;
            SurvivalSeconds += seconds;
            totalFaceTime[ActiveFace] += seconds;
            if (ActiveFace == 0 || double.IsInfinity(intervals[ActiveFace])) return;
            double total = elapsed[ActiveFace] + seconds;
            double completed = Math.Floor(total / intervals[ActiveFace]);
            elapsed[ActiveFace] = total % intervals[ActiveFace];
            pendingRows[ActiveFace] = (int)Math.Min(int.MaxValue, pendingRows[ActiveFace] + completed * ActiveFace);
        }

        private bool ApplyPendingGarbage()
        {
            totalFaceLines[ActiveFace] += Game.LastClearCount;
            if(!IsPractice)
            {
                if(Game.LastClearCount>0)CycleClearMask|=1<<ActiveFace;
                if(ActiveFace==FaceCount-1)
                {
                    CompletedRotations++;
                    LastRotationBonus=CycleClearMask==15?RotationBonus:0;
                    if(LastRotationBonus>0){SuccessfulRotations++;Game.AwardBonus(LastRotationBonus);}
                    CycleClearMask=0;
                }
            }
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
            if(!IsPractice) ActiveFace = (ActiveFace + 1) % FaceCount;
            return Faces[ActiveFace];
        }
    }
}
