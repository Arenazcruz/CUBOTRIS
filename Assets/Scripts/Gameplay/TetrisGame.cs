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

    public sealed class TetrisGame
    {
        private static readonly Vector2Int[] Kicks =
        {
            Vector2Int.zero, Vector2Int.left, Vector2Int.right,
            new Vector2Int(-2, 0), new Vector2Int(2, 0), Vector2Int.up,
            new Vector2Int(-1, 1), new Vector2Int(1, 1), Vector2Int.down
        };
        private readonly IPieceSource source;
        private readonly FallSettings settings;
        private readonly Func<BoardState> nextBoard;
        private float fallTime;
        private bool wasSoftDropping;

        public BoardState Board { get; private set; }
        public ActivePiece Active { get; private set; }
        public bool HasActivePiece { get; private set; }
        public bool IsGameOver { get; private set; }
        public int LinesCleared { get; private set; }
        public int Score { get; private set; }
        public int PiecesLocked { get; private set; }
        public int Revision { get; private set; }
        public TetrominoType NextPiece => source.Peek();
        public float GravityInterval => Mathf.Max(0.01f, settings.minimumInterval,
            settings.initialInterval * Mathf.Pow(Mathf.Clamp(settings.speedMultiplier, 0.1f, 1f), LinesCleared / Mathf.Max(1, settings.linesPerStep)));

        public TetrisGame(BoardState board = null, IPieceSource source = null, FallSettings settings = null, Func<BoardState> nextBoard = null)
        {
            Board = board ?? new BoardState();
            this.source = source ?? new SevenBag();
            this.settings = settings ?? new FallSettings();
            this.nextBoard = nextBoard;
            Spawn();
        }

        public bool Move(int direction)
        {
            if (IsGameOver || !HasActivePiece || (direction != -1 && direction != 1)) return false;
            return TryActive(Active.Rotation, Active.Position + new Vector2Int(direction, 0));
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
            return new ActivePiece(Active.Type, Active.Rotation, position);
        }

        private bool TryActive(int rotation, Vector2Int position)
        {
            var candidate = new ActivePiece(Active.Type, rotation, position);
            if (!Board.CanPlace(candidate.Cells)) return false;
            Active = candidate;
            Revision++;
            return true;
        }

        private void LockAndSpawn()
        {
            if (!Board.TryLock(Active.Type, Active.Cells))
            {
                EndGame();
                return;
            }
            PiecesLocked++;
            int cleared = Board.ClearFullRows();
            LinesCleared += cleared;
            Score += cleared switch { 1 => 100, 2 => 300, 3 => 500, 4 => 800, _ => 0 };
            fallTime = 0;
            if (nextBoard != null) Board = nextBoard();
            Spawn();
        }

        private void Spawn()
        {
            Active = new ActivePiece(source.Next(), 0, new Vector2Int(Board.Width / 2 - 1, Board.Height - 2));
            HasActivePiece = Board.CanPlace(Active.Cells);
            if (!HasActivePiece) EndGame();
            Revision++;
        }

        private void EndGame()
        {
            IsGameOver = true;
            HasActivePiece = false;
            Revision++;
        }
    }
}
