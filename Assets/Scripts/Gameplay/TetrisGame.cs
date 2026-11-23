using System;
using UnityEngine;

namespace Cubotris.Gameplay
{
    [Serializable]
    public sealed class FallSettings
    {
        [Min(0.01f)] public float initialInterval = 0.8f;
        [Min(0.01f)] public float minimumInterval = 0.12f;
        [Min(0.01f)] public float softDropInterval = 0.045f;
        [Range(0.1f, 1f)] public float speedMultiplier = 0.85f;
        [Min(1)] public int linesPerStep = 10;
    }

    public sealed partial class TetrisGame
    {
        private static readonly Color[] PieceColors =
        {
            new Color32(0, 210, 255, 255),
            new Color32(255, 75, 120, 255),
            new Color32(255, 205, 55, 255),
            new Color32(110, 230, 120, 255),
            new Color32(175, 115, 255, 255),
            new Color32(255, 130, 45, 255),
            new Color32(70, 145, 255, 255)
        };
        private static readonly Vector2Int[] Kicks =
        {
            Vector2Int.zero, Vector2Int.left, Vector2Int.right,
            new Vector2Int(-2, 0), new Vector2Int(2, 0), Vector2Int.up,
            new Vector2Int(-1, 1), new Vector2Int(1, 1), Vector2Int.down
        };
        private readonly IPieceSource source;
        private readonly FallSettings settings;
        private readonly Func<BoardState> nextBoard;
        private readonly Func<bool> afterLock;
        private readonly StatefulRandom colorRandom = new StatefulRandom();
        private int lastPieceColorIndex = -1;
        private Color nextPieceColor;
        private float fallTime;
        private bool wasSoftDropping;

        public BoardState Board { get; private set; }
        public ActivePiece Active { get; private set; }
        public bool HasActivePiece { get; private set; }
        public bool IsGameOver { get; private set; }
        public int LinesCleared { get; private set; }
        public int Score { get; private set; }
        public int LineScore { get; private set; }
        public int LastClearCount { get; private set; }
        public int ComboChain { get; private set; }
        public int MaxCombo { get; private set; }
        public int LastComboBonus { get; private set; }
        public int ComboBonusScore { get; private set; }
        public int PiecesLocked { get; private set; }
        public int Revision { get; private set; }
        public TetrominoType NextPiece => source.Peek();
        public Color NextPieceColor => nextPieceColor;
        public TetrominoType? HeldPiece { get; private set; }
        public Color HeldPieceColor { get; private set; }
        public bool HoldUsed { get; private set; }
        public bool CanHold => !IsGameOver && HasActivePiece && !HoldUsed;
        public float GravityInterval => Mathf.Max(0.01f, settings.minimumInterval,
            settings.initialInterval * Mathf.Pow(Mathf.Clamp(settings.speedMultiplier, 0.1f, 1f), LinesCleared / Mathf.Max(1, settings.linesPerStep)));

        public TetrisGame(BoardState board = null, IPieceSource source = null, FallSettings settings = null, Func<BoardState> nextBoard = null, Func<bool> afterLock = null)
        {
            Board = board ?? new BoardState();
            this.source = source ?? new SevenBag();
            this.settings = settings ?? new FallSettings();
            this.nextBoard = nextBoard;
            this.afterLock = afterLock;
            nextPieceColor = RandomPieceColor();
            Spawn();
        }

        public bool Move(int direction)
        {
            if (IsGameOver || !HasActivePiece || (direction != -1 && direction != 1)) return false;
            return TryActive(Active.Rotation, Active.Position + new Vector2Int(direction, 0));
        }

        public bool Hold()
        {
            if (!CanHold) return false;
            var previous = HeldPiece;
            var previousColor = HeldPieceColor;
            HeldPiece = Active.Type;
            HeldPieceColor = Active.Color;
            HoldUsed = true;
            fallTime = 0;
            wasSoftDropping = false;
            if (previous.HasValue) SpawnPiece(previous.Value, previousColor);
            else Spawn();
            return true;
        }

        public bool RotateClockwise()
        {
            if (IsGameOver || !HasActivePiece) return false;
            int rotation = (Active.Rotation + 1) % 4;
            foreach (var kick in Kicks)
                if (TryActive(rotation, Active.Position + kick)) return true;
            return false;
        }

        public bool StepDown()
        {
            if (IsGameOver || !HasActivePiece) return false;
            if (TryActive(Active.Rotation, Active.Position + Vector2Int.down)) return true;
            LockAndSpawn();
            return false;
        }

        public int HardDrop()
        {
            if (IsGameOver || !HasActivePiece) return 0;
            int distance = 0;
            while (TryActive(Active.Rotation, Active.Position + Vector2Int.down)) distance++;
            LockAndSpawn();
            return distance;
        }

        public void Tick(float deltaTime, bool softDrop = false)
        {
            if (IsGameOver || !HasActivePiece || deltaTime <= 0) return;
            if (softDrop != wasSoftDropping)
            {
                wasSoftDropping = softDrop;
                fallTime = 0;
                if (softDrop)
                {
                    int lockedBefore = PiecesLocked;
                    StepDown();
                    if (IsGameOver || PiecesLocked != lockedBefore) return;
                }
            }
            float interval = softDrop ? Mathf.Min(GravityInterval, Mathf.Max(0.01f, settings.softDropInterval)) : GravityInterval;
            fallTime += deltaTime;
            while (fallTime >= interval)
            {
                fallTime -= interval;
                int lockedBefore = PiecesLocked;
                StepDown();
                // A newly spawned piece always receives its own full fall interval.
                if (IsGameOver || PiecesLocked != lockedBefore) break;
            }
        }

        public ActivePiece? GetLandingPiece()
        {
            if (!HasActivePiece || IsGameOver) return null;
            var position = Active.Position;
            while (Board.CanPlace(TetrominoData.Cells(Active.Type, Active.Rotation, position + Vector2Int.down)))
                position += Vector2Int.down;
            return new ActivePiece(Active.Type, Active.Rotation, position, Active.Color);
        }

        private bool TryActive(int rotation, Vector2Int position)
        {
            var candidate = new ActivePiece(Active.Type, rotation, position, Active.Color);
            if (!Board.CanPlace(candidate.Cells)) return false;
            Active = candidate;
            Revision++;
            return true;
        }

        private void LockAndSpawn()
        {
            if (!Board.TryLock(Active.Type, Active.Cells, Active.Color))
            {
                EndGame();
                return;
            }
            PiecesLocked++;
            HoldUsed = false;
            int cleared = Board.ClearFullRows();
            LastClearCount = cleared;
            LinesCleared += cleared;
            int points = cleared switch { 1 => 100, 2 => 300, 3 => 500, 4 => 800, _ => 0 };
            LineScore += points;
            Score += points;
            ComboChain = cleared > 0 ? ComboChain + 1 : 0;
            MaxCombo = Math.Max(MaxCombo, ComboChain);
            LastComboBonus = ComboChain > 1 ? 50 * (ComboChain - 1) : 0;
            ComboBonusScore += LastComboBonus;
            if (LastComboBonus > 0) AwardBonus(LastComboBonus);
            fallTime = 0;
            if (afterLock != null && !afterLock()) { EndGame(); return; }
            if (nextBoard != null) Board = nextBoard();
            Spawn();
        }

        private void Spawn()
        {
            var type = source.Next();
            var color = nextPieceColor;
            nextPieceColor = RandomPieceColor();
            SpawnPiece(type, color);
        }

        internal void AwardBonus(int points)
        {
            if (points < 0) throw new ArgumentOutOfRangeException(nameof(points));
            Score += points;
            Revision++;
        }

        private void SpawnPiece(TetrominoType type, Color color)
        {
            Active = new ActivePiece(type, 0, new Vector2Int(Board.Width / 2 - 1, Board.Height - 2), color);
            HasActivePiece = Board.CanPlace(Active.Cells);
            if (!HasActivePiece) EndGame();
            Revision++;
        }

        private Color RandomPieceColor()
        {
            int colorIndex = colorRandom.Next(PieceColors.Length - (lastPieceColorIndex < 0 ? 0 : 1));
            if (lastPieceColorIndex >= 0 && colorIndex >= lastPieceColorIndex) colorIndex++;
            lastPieceColorIndex = colorIndex;
            return PieceColors[colorIndex];
        }

        private void EndGame()
        {
            IsGameOver = true;
            HasActivePiece = false;
            Revision++;
        }
    }
}
