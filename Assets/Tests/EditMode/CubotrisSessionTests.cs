using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
namespace Cubotris.Tests
{
    public sealed class CubotrisSessionTests
    {
        private sealed class PieceSource : IPieceSource
        {
            private readonly TetrominoType type;
            public int Count { get; private set; }
            public PieceSource(TetrominoType type = TetrominoType.O) { this.type = type; }
            public TetrominoType Peek() => type;
            public TetrominoType Next() { Count++; return type; }
        }
        [Test]
        public void FourBoardsStartEmptyAndRoundTripPreservesEveryCell()
        {
            var source = new PieceSource(); var session = new CubotrisSession(source);
            Assert.That(session.Faces.Count, Is.EqualTo(4));
            Assert.That(session.ActiveFace, Is.Zero);
            Assert.That(session.Faces.Distinct().Count(), Is.EqualTo(4));
            var saved = new string[4];
            for (int i = 0; i < 4; i++)
            {
                var board = session.Game.Board;
                Assert.That(board.Width, Is.EqualTo(10)); Assert.That(board.Height, Is.EqualTo(20));
                Assert.That(Count(board), Is.Zero);
                for (int x = 0; x < i; x++) session.Game.Move(-1);
                session.Game.HardDrop(); saved[i] = Snapshot(board);
                Assert.That(Count(board), Is.EqualTo(4));
                Assert.That(session.ActiveFace, Is.EqualTo((i + 1) % 4));
            }
            for (int i = 0; i < 4; i++) Assert.That(Snapshot(session.Faces[i]), Is.EqualTo(saved[i]));
            Assert.That(session.Game.Board, Is.SameAs(session.Faces[0]));
            Assert.That(source.Count, Is.EqualTo(5));
        }
        [Test]
        public void MovementRotationAndNonLockingSoftDropDoNotChangeFace()
        {
            var session = new CubotrisSession(new PieceSource(TetrominoType.T));
            session.Game.Move(-1); session.Game.RotateClockwise(); session.Game.Tick(0.19f, true);
            Assert.That(session.ActiveFace, Is.Zero); Assert.That(session.Game.PiecesLocked, Is.Zero);
            session.Game.Tick(100, true);
            Assert.That(session.ActiveFace, Is.EqualTo(1)); Assert.That(session.Game.PiecesLocked, Is.EqualTo(1));
            Assert.That(session.Game.Active.Position.y, Is.EqualTo(18));
        }
        [Test]
        public void LineClearsAreLocalWhileScoreAndDifficultyAccumulateAcrossFaces()
        {
            var settings = new FallSettings { initialInterval = 1, minimumInterval = 0.2f, speedMultiplier = 0.8f, linesPerStep = 1 };
            var session = new CubotrisSession(new PieceSource(TetrominoType.I), settings);
            for (int face = 0; face < 4; face++)
                for (int row = 0; row <= face; row++)
                    session.Faces[face].TryLock(TetrominoType.L, Enumerable.Range(0, 10).Where(x => x != 5).Select(x => new Vector2Int(x, row)).ToArray());
            int score = 0, lines = 0;
            int[] points = { 100, 300, 500, 800 };
            for (int face = 0; face < 4; face++)
            {
                string[] before = session.Faces.Select(Snapshot).ToArray();
                session.Game.RotateClockwise(); session.Game.HardDrop();
                lines += face + 1; score += points[face];
                Assert.That(session.Game.LinesCleared, Is.EqualTo(lines));
                Assert.That(session.Game.Score, Is.EqualTo(score));
                Assert.That(Count(session.Faces[face]), Is.EqualTo(3 - face));
                for (int other = 0; other < 4; other++)
                    if (other != face) Assert.That(Snapshot(session.Faces[other]), Is.EqualTo(before[other]));
                Assert.That(session.Game.GravityInterval, Is.EqualTo(Mathf.Max(0.2f, Mathf.Pow(0.8f, lines))).Within(0.0001f));
            }
            Assert.That(session.Game.Score, Is.EqualTo(1700));
            Assert.That(session.Game.LinesCleared, Is.EqualTo(10));
        }
        [Test]
        public void SevenBagSequenceContinuesAcrossTwentyEightFaceChanges()
        {
            var session = new CubotrisSession(new SevenBag(812)); var expected = new SevenBag(812);
            for (int i = 0; i < 28; i++)
            {
                Assert.That(session.Game.IsGameOver, Is.False);
                Assert.That(session.Game.Active.Type, Is.EqualTo(expected.Next()));
                session.Game.HardDrop();
                Assert.That(session.ActiveFace, Is.EqualTo((i + 1) % 4));
            }
            Assert.That(session.Game.Active.Type, Is.EqualTo(expected.Next()));
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void BlockedSpawnOnAnyDestinationEndsTheWholeSession(int face)
        {
            var source = new PieceSource(); var session = new CubotrisSession(source);
            while ((session.ActiveFace + 1) % 4 != face) session.Game.HardDrop();
            session.Faces[face].TryLock(TetrominoType.Z, new[] { new Vector2Int(4, 18) });
            session.Game.HardDrop();
            Assert.That(session.ActiveFace, Is.EqualTo(face));
            Assert.That(session.Game.IsGameOver, Is.True);
            Assert.That(session.Game.HasActivePiece, Is.False);
            int draws = source.Count, locked = session.Game.PiecesLocked;
            var snapshots = session.Faces.Select(Snapshot).ToArray();
            session.Game.Move(1); session.Game.RotateClockwise(); session.Game.Tick(100, true); session.Game.HardDrop();
            CollectionAssert.AreEqual(snapshots, session.Faces.Select(Snapshot).ToArray());
            Assert.That(source.Count, Is.EqualTo(draws)); Assert.That(session.Game.PiecesLocked, Is.EqualTo(locked));
        }
        [Test]
        public void SessionsDoNotShareBoardsProgressOrGenerator()
        {
            var first = new CubotrisSession(new SevenBag(3)); var second = new CubotrisSession(new SevenBag(3));
            first.Game.HardDrop(); first.Game.HardDrop();
            Assert.That(second.ActiveFace, Is.Zero); Assert.That(second.Game.PiecesLocked, Is.Zero);
            Assert.That(second.Faces.Sum(Count), Is.Zero);
        }
        private static int Count(BoardState board) => Enumerable.Range(0, 200).Count(i => board[i % 10, i / 10].HasValue);
        private static string Snapshot(BoardState board) => string.Join(",", Enumerable.Range(0, 200).Select(i => board[i % 10, i / 10]?.ToString() ?? "-"));
    }
}
