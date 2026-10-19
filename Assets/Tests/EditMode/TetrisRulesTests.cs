using System;
using System.Collections.Generic;
using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Cubotris.Tests
{
    public sealed class TetrisRulesTests
    {
        private sealed class Sequence : IPieceSource
        {
            private readonly TetrominoType[] types;
            public int Count { get; private set; }
            public Sequence(params TetrominoType[] types) { this.types = types; }
            public TetrominoType Next() => types[Count++ % types.Length];
        }

        [TestCase(TetrominoType.I)] [TestCase(TetrominoType.O)] [TestCase(TetrominoType.T)]
        [TestCase(TetrominoType.S)] [TestCase(TetrominoType.Z)] [TestCase(TetrominoType.J)] [TestCase(TetrominoType.L)]
        public void EveryPieceSpawnsAndFourRotationsRestoreItsCells(TetrominoType type)
        {
            var game = new TetrisGame(source: new Sequence(type));
            Assert.That(game.HasActivePiece, Is.True);
            var initial = game.Active.Cells;
            Assert.That(initial.Distinct().Count(), Is.EqualTo(4));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(game.RotateClockwise(), Is.True);
                Assert.That(game.Active.Cells.Distinct().Count(), Is.EqualTo(4));
                Assert.That(game.Board.CanPlace(game.Active.Cells), Is.True);
            }
            CollectionAssert.AreEquivalent(initial, game.Active.Cells);
        }

        [Test]
        public void SevenBagContainsEachPieceOncePerBagAndIsReproducible()
        {
            var bag = new SevenBag(52);
            var replay = new SevenBag(52);
            for (int b = 0; b < 100; b++)
            {
                var pieces = new List<TetrominoType>();
                for (int i = 0; i < 7; i++)
                {
                    var piece = bag.Next();
                    pieces.Add(piece);
                    Assert.That(replay.Next(), Is.EqualTo(piece));
                }
                Assert.That(pieces.Distinct().Count(), Is.EqualTo(7));
            }
        }

        [Test]
        public void HorizontalMovementStopsAtBothWalls()
        {
            var game = new TetrisGame(source: new Sequence(TetrominoType.T));
            Assert.That(game.Move(-1), Is.True);
            Assert.That(game.Active.Position.x, Is.EqualTo(3));
            while (game.Move(-1)) { }
            Assert.That(game.Active.Cells.Min(c => c.x), Is.Zero);
            var stopped = game.Active.Position;
            Assert.That(game.Move(-1), Is.False);
            Assert.That(game.Active.Position, Is.EqualTo(stopped));
            while (game.Move(1)) { }
            Assert.That(game.Active.Cells.Max(c => c.x), Is.EqualTo(9));
            Assert.That(game.Move(1), Is.False);
        }

        [Test]
        public void RotationKicksAwayFromRightWall()
        {
            var game = new TetrisGame(source: new Sequence(TetrominoType.I));
            game.RotateClockwise();
            while (game.Move(1)) { }
            Assert.That(game.Active.Cells.Max(c => c.x), Is.EqualTo(9));
            Assert.That(game.RotateClockwise(), Is.True);
            Assert.That(game.Active.Rotation, Is.EqualTo(2));
            Assert.That(game.Active.Cells.Max(c => c.x), Is.LessThan(10));
        }

        [Test]
        public void RotationCanAdjustUpwardAtFloor()
        {
            var game = new TetrisGame(source: new Sequence(TetrominoType.T));
            for (int i = 0; i < 18; i++) Assert.That(game.StepDown(), Is.True);
            Assert.That(game.Active.Position.y, Is.Zero);
            Assert.That(game.RotateClockwise(), Is.True);
            Assert.That(game.Active.Cells.Min(c => c.y), Is.Zero);
        }

        [Test]
        public void BlockedRotationDoesNotChangePieceOrBoard()
        {
            var game = new TetrisGame(source: new Sequence(TetrominoType.T));
            var current = game.Active.Cells.ToHashSet();
            var occupied = new List<Vector2Int>();
            for (int y = 0; y < 20; y++)
                for (int x = 0; x < 10; x++) if (!current.Contains(new Vector2Int(x, y))) occupied.Add(new Vector2Int(x, y));
            game.Board.TryLock(TetrominoType.O, occupied);
            int version = game.Board.Version;
            Assert.That(game.RotateClockwise(), Is.False);
            CollectionAssert.AreEquivalent(current, game.Active.Cells);
            Assert.That(game.Board.Version, Is.EqualTo(version));
        }

        [TestCase(-1, 0)] [TestCase(10, 0)] [TestCase(0, -1)] [TestCase(0, 20)]
        public void BoardRejectsCoordinatesBeyondItsBounds(int x, int y)
        {
            var board = new BoardState();
            Assert.That(board.CanPlace(new[] { new Vector2Int(x, y) }), Is.False);
        }

        [Test]
        public void LockIsAtomicAndRejectsOccupiedCells()
        {
            var board = new BoardState();
            Assert.That(board.TryLock(TetrominoType.J, new[] { new Vector2Int(2, 2) }), Is.True);
            Assert.That(board.TryLock(TetrominoType.I, new[] { new Vector2Int(1, 1), new Vector2Int(2, 2) }), Is.False);
            Assert.That(board[1, 1], Is.Null);
            Assert.That(board[2, 2], Is.EqualTo(TetrominoType.J));
        }

        [Test]
        public void FallingLocksAtFloorAndSpawnsExactlyOneNewPiece()
        {
            var sequence = new Sequence(TetrominoType.O, TetrominoType.T);
            var game = new TetrisGame(source: sequence);
            for (int i = 0; i < 18; i++) Assert.That(game.StepDown(), Is.True);
            Assert.That(game.PiecesLocked, Is.Zero);
            Assert.That(game.StepDown(), Is.False);
            Assert.That(game.PiecesLocked, Is.EqualTo(1));
            Assert.That(Occupied(game.Board), Is.EqualTo(4));
            Assert.That(game.Active.Type, Is.EqualTo(TetrominoType.T));
            Assert.That(sequence.Count, Is.EqualTo(2));
        }

        [Test]
        public void HardDropLandsAboveOccupiedCellsWithoutOverlap()
        {
            var board = new BoardState();
            board.TryLock(TetrominoType.Z, new[] { new Vector2Int(5, 4) });
            var game = new TetrisGame(board, new Sequence(TetrominoType.I));
            Assert.That(game.HardDrop(), Is.EqualTo(13));
            for (int x = 3; x <= 6; x++) Assert.That(board[x, 5], Is.EqualTo(TetrominoType.I));
            Assert.That(board[5, 4], Is.EqualTo(TetrominoType.Z));
            Assert.That(Occupied(board), Is.EqualTo(5));
        }

        [Test]
        public void GravityWaitsForIntervalAndSoftDropIsFaster()
        {
            var regular = new TetrisGame(source: new Sequence(TetrominoType.O));
            regular.Tick(0.79f);
            Assert.That(regular.Active.Position.y, Is.EqualTo(18));
            regular.Tick(0.02f);
            Assert.That(regular.Active.Position.y, Is.EqualTo(17));
            var soft = new TetrisGame(source: new Sequence(TetrominoType.O));
            soft.Tick(0.19f, true);
            Assert.That(soft.Active.Position.y, Is.EqualTo(13));
        }

        [Test]
        public void SoftDropTapIsImmediateAndDoesNotReuseAccumulatedGravityTime()
        {
            var game = new TetrisGame(source: new Sequence(TetrominoType.O));
            game.Tick(0.7f);
            game.Tick(0.016f, true);
            Assert.That(game.Active.Position.y, Is.EqualTo(17));
            game.Tick(0.016f, false);
            Assert.That(game.Active.Position.y, Is.EqualTo(17));
            game.Tick(0.016f, true);
            Assert.That(game.Active.Position.y, Is.EqualTo(16));
        }

        [Test]
        public void LongFrameCannotDropSeveralNewPieces()
        {
            var game = new TetrisGame(source: new Sequence(TetrominoType.O));
            game.Tick(100f, true);
            Assert.That(game.PiecesLocked, Is.EqualTo(1));
            Assert.That(game.Active.Position.y, Is.EqualTo(18));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void ClearingRowsPreservesAndDropsAllOtherBlocks(int rows)
        {
            var board = new BoardState();
            for (int y = 0; y < rows; y++)
                board.TryLock(TetrominoType.I, Enumerable.Range(0, 10).Select(x => new Vector2Int(x, y)).ToArray());
            board.TryLock(TetrominoType.T, new[] { new Vector2Int(2, rows) });
            board.TryLock(TetrominoType.L, new[] { new Vector2Int(7, rows + 3) });
            Assert.That(board.ClearFullRows(), Is.EqualTo(rows));
            Assert.That(board[2, 0], Is.EqualTo(TetrominoType.T));
            Assert.That(board[7, 3], Is.EqualTo(TetrominoType.L));
            Assert.That(Occupied(board), Is.EqualTo(2));
            Assert.That(board.ClearFullRows(), Is.Zero);
        }

        [Test]
        public void NonAdjacentRowsCompactWithoutDestroyingInterveningBlocks()
        {
            var board = new BoardState();
            foreach (int y in new[] { 0, 2, 19 })
                board.TryLock(TetrominoType.O, Enumerable.Range(0, 10).Select(x => new Vector2Int(x, y)).ToArray());
            board.TryLock(TetrominoType.S, new[] { new Vector2Int(0, 1), new Vector2Int(1, 3) });
            Assert.That(board.ClearFullRows(), Is.EqualTo(3));
            Assert.That(board[0, 0], Is.EqualTo(TetrominoType.S));
            Assert.That(board[1, 1], Is.EqualTo(TetrominoType.S));
            Assert.That(Occupied(board), Is.EqualTo(2));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void LockClearsLinesAndUpdatesTotal(int rows)
        {
            var board = new BoardState();
            for (int y = 0; y < rows; y++)
                board.TryLock(TetrominoType.L, Enumerable.Range(0, 10).Where(x => x != 5).Select(x => new Vector2Int(x, y)).ToArray());
            var game = new TetrisGame(board, new Sequence(TetrominoType.I));
            game.RotateClockwise();
            game.HardDrop();
            Assert.That(game.LinesCleared, Is.EqualTo(rows));
            Assert.That(game.Score, Is.EqualTo(new[] { 0, 100, 300, 500, 800 }[rows]));
            Assert.That(Occupied(board), Is.EqualTo(4 - rows));
        }

        [Test]
        public void GravityProgressesWithLinesAndHonorsMinimum()
        {
            var board = new BoardState();
            board.TryLock(TetrominoType.L, Enumerable.Range(0, 10).Where(x => x < 3 || x > 6).Select(x => new Vector2Int(x, 0)).ToArray());
            var settings = new FallSettings { initialInterval = 1, minimumInterval = 0.3f, speedMultiplier = 0.1f, linesPerStep = 1 };
            var game = new TetrisGame(board, new Sequence(TetrominoType.I), settings);
            Assert.That(game.GravityInterval, Is.EqualTo(1));
            game.HardDrop();
            Assert.That(game.LinesCleared, Is.EqualTo(1));
            Assert.That(game.GravityInterval, Is.EqualTo(0.3f));
        }

        [Test]
        public void BlockedSpawnEndsGameAndAllActionsStop()
        {
            var board = new BoardState();
            board.TryLock(TetrominoType.Z, new[] { new Vector2Int(3, 18) });
            var sequence = new Sequence(TetrominoType.O, TetrominoType.I);
            var game = new TetrisGame(board, sequence);
            Assert.That(game.IsGameOver, Is.False);
            game.HardDrop();
            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.HasActivePiece, Is.False);
            int version = board.Version;
            Assert.That(game.Move(1), Is.False);
            Assert.That(game.RotateClockwise(), Is.False);
            Assert.That(game.StepDown(), Is.False);
            Assert.That(game.HardDrop(), Is.Zero);
            game.Tick(100, true);
            Assert.That(board.Version, Is.EqualTo(version));
            Assert.That(sequence.Count, Is.EqualTo(2));
        }

        [Test]
        public void BlockedInitialSpawnIsGameOver()
        {
            var board = new BoardState();
            board.TryLock(TetrominoType.Z, new[] { new Vector2Int(4, 18) });
            Assert.That(new TetrisGame(board, new Sequence(TetrominoType.O)).IsGameOver, Is.True);
        }

        [Test]
        public void IndependentBoardsDoNotShareStateOrBagProgress()
        {
            var first = new TetrisGame(source: new SevenBag(88));
            var second = new TetrisGame(source: new SevenBag(88));
            var untouched = second.Active;
            first.HardDrop(); first.HardDrop(); first.Move(-1);
            Assert.That(Occupied(second.Board), Is.Zero);
            Assert.That(second.PiecesLocked, Is.Zero);
            Assert.That(second.LinesCleared, Is.Zero);
            Assert.That(second.Active.Type, Is.EqualTo(untouched.Type));
            Assert.That(second.Active.Position, Is.EqualTo(untouched.Position));
        }

        [TestCase(1)] [TestCase(52)] [TestCase(996)]
        public void RepeatedPlayNeverProducesAnInvalidActivePiece(int seed)
        {
            var random = new System.Random(seed);
            var game = new TetrisGame(source: new SevenBag(seed));
            for (int i = 0; i < 1000 && !game.IsGameOver; i++)
            {
                game.Move(random.Next(2) == 0 ? -1 : 1);
                if (random.Next(3) == 0) game.RotateClockwise();
                if (random.Next(5) == 0) game.HardDrop();
                else game.Tick(0.1f, random.Next(2) == 0);
                if (game.HasActivePiece) Assert.That(game.Board.CanPlace(game.Active.Cells), Is.True);
                Assert.That(Occupied(game.Board), Is.LessThanOrEqualTo(200));
            }
        }

        private static int Occupied(BoardState board)
        {
            int count = 0;
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++) if (board[x, y].HasValue) count++;
            return count;
        }
    }
}
