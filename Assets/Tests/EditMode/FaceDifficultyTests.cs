using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Cubotris.Tests
{
    public sealed class FaceDifficultyTests
    {
        private sealed class ConstantPiece : IPieceSource
        {
            private readonly TetrominoType type;
            public int Draws { get; private set; }
            public ConstantPiece(TetrominoType type = TetrominoType.O) { this.type = type; }
            public TetrominoType Peek() => type;
            public TetrominoType Next() { Draws++; return type; }
        }

        [Test]
        public void FaceOneHasNoPenaltyEvenAfterManyIntervals()
        {
            var session = new CubotrisSession(new ConstantPiece());
            session.AdvanceActiveTime(1000);
            Assert.That(session.SurvivalSeconds, Is.EqualTo(1000));
            Assert.That(session.FaceElapsedSeconds(0), Is.Zero);
            Assert.That(session.PendingGarbageRows(0), Is.Zero);
            session.Game.HardDrop();
            Assert.That(Count(session.Faces[0]), Is.EqualTo(4));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void ThirtySecondsQueuesExactRowsAndOnlyLockAppliesThemToThatFace(int face)
        {
            var session = AtFace(face);
            var snapshots = session.Faces.Select(Snapshot).ToArray();
            var active = session.Game.Active;
            int revision = session.Game.Revision;
            session.AdvanceActiveTime(29.5);
            Assert.That(session.PendingGarbageRows(face), Is.Zero);
            session.AdvanceActiveTime(0.5);
            Assert.That(session.PendingGarbageRows(face), Is.EqualTo(face));
            CollectionAssert.AreEqual(snapshots, session.Faces.Select(Snapshot).ToArray());
            Assert.That(session.Game.Active.Position, Is.EqualTo(active.Position));
            Assert.That(session.Game.Revision, Is.EqualTo(revision));
            session.Game.HardDrop();
            Assert.That(Count(session.Faces[face]), Is.EqualTo(4 + face * 9));
            Assert.That(session.ActiveFace, Is.EqualTo((face + 1) % 4));
            Assert.That(session.PendingGarbageRows(face), Is.Zero);
            Assert.That(session.Game.Score, Is.Zero);
            Assert.That(session.Game.LinesCleared, Is.Zero);
            Assert.That(session.Game.GravityInterval, Is.EqualTo(0.8f));
            for (int other = 0; other < 4; other++)
                if (other != face) Assert.That(Snapshot(session.Faces[other]), Is.EqualTo(snapshots[other]));
            CheckHoles(session.Faces[face], face);
        }

        [Test]
        public void InactiveFacesKeepTheirPartialTimeAndOnlyTheActiveFaceAccumulates()
        {
            var session = AtFace(2);
            session.AdvanceActiveTime(12); session.Game.HardDrop();
            session.AdvanceActiveTime(7); session.Game.HardDrop();
            session.AdvanceActiveTime(9); session.Game.HardDrop();
            session.AdvanceActiveTime(3); session.Game.HardDrop();
            Assert.That(session.ActiveFace, Is.EqualTo(2));
            Assert.That(session.FaceElapsedSeconds(2), Is.EqualTo(12));
            Assert.That(session.FaceElapsedSeconds(3), Is.EqualTo(7));
            Assert.That(session.FaceElapsedSeconds(1), Is.EqualTo(3));
            Assert.That(session.SurvivalSeconds, Is.EqualTo(31));
            session.AdvanceActiveTime(18);
            Assert.That(session.PendingGarbageRows(2), Is.EqualTo(2));
            Assert.That(session.SecondsUntilGarbage(2), Is.EqualTo(30));
            Assert.That(session.SecondsUntilGarbage(3), Is.EqualTo(23));
            Assert.That(session.SecondsUntilGarbage(1), Is.EqualTo(27));
        }

        [Test]
        public void MultipleIntervalsKeepRemainderAndApplyExactlyOnce()
        {
            var session = AtFace(3);
            session.AdvanceActiveTime(95);
            Assert.That(session.PendingGarbageRows(3), Is.EqualTo(9));
            Assert.That(session.FaceElapsedSeconds(3), Is.EqualTo(5));
            Assert.That(session.SecondsUntilGarbage(3), Is.EqualTo(25));
            session.Game.HardDrop();
            Assert.That(Count(session.Faces[3]), Is.EqualTo(85));
            CheckHoles(session.Faces[3], 9);
            for (int i = 0; i < 3; i++) session.Game.HardDrop();
            session.Game.HardDrop();
            Assert.That(Count(session.Faces[3]), Is.EqualTo(89));
            Assert.That(session.PendingGarbageRows(3), Is.Zero);
            Assert.That(session.FaceElapsedSeconds(3), Is.EqualTo(5));
        }

        [Test]
        public void IntervalsCanBeConfiguredIndependently()
        {
            var session = new CubotrisSession(new ConstantPiece(), garbageSettings: new GarbageSettings { face2Interval = 10, face3Interval = 20, face4Interval = 40 });
            session.Game.HardDrop(); session.AdvanceActiveTime(25);
            Assert.That(session.PendingGarbageRows(1), Is.EqualTo(2));
            Assert.That(session.SecondsUntilGarbage(1), Is.EqualTo(5));
            session.Game.HardDrop(); session.AdvanceActiveTime(25);
            Assert.That(session.PendingGarbageRows(2), Is.EqualTo(2));
            Assert.That(session.SecondsUntilGarbage(2), Is.EqualTo(15));
            session.Game.HardDrop(); session.AdvanceActiveTime(25);
            Assert.That(session.PendingGarbageRows(3), Is.Zero);
            Assert.That(session.SecondsUntilGarbage(3), Is.EqualTo(15));
        }

        [Test]
        public void GarbageShiftsEveryExistingCellUpWithoutChangingItsType()
        {
            var board = new BoardState();
            board.TryLock(TetrominoType.T, new[] { new Vector2Int(2, 4) });
            board.TryLock(TetrominoType.Z, new[] { new Vector2Int(9, 18) });
            int version = board.Version;
            Assert.That(board.TryPushGarbageRows(new[] { 6 }), Is.True);
            Assert.That(board[2, 5], Is.EqualTo(TetrominoType.T));
            Assert.That(board[9, 19], Is.EqualTo(TetrominoType.Z));
            Assert.That(board[2, 4], Is.Null);
            Assert.That(board[6, 0], Is.Null);
            Assert.That(Count(board), Is.EqualTo(11));
            Assert.That(board.Version, Is.EqualTo(version + 1));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void OverflowDoesNotDeleteOrPartiallyShiftAnyCell(int rows)
        {
            var board = new BoardState();
            board.TryLock(TetrominoType.T, new[] { new Vector2Int(0, 0), new Vector2Int(9, 20 - rows) });
            string before = Snapshot(board); int version = board.Version;
            Assert.That(board.TryPushGarbageRows(Enumerable.Range(0, rows).ToArray()), Is.False);
            Assert.That(Snapshot(board), Is.EqualTo(before));
            Assert.That(board.Version, Is.EqualTo(version));
            Assert.That(new BoardState().TryPushGarbageRows(new int[21]), Is.False);
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
        public void GarbageAwardsNoPointsUntilClearedAndUsesNormalScoringAndSpeed(int rows)
        {
            var board = new BoardState();
            var settings = new FallSettings { linesPerStep = 1 };
            var game = new TetrisGame(board, new ConstantPiece(TetrominoType.I), settings);
            board.TryPushGarbageRows(Enumerable.Repeat(5, rows).ToArray());
            Assert.That(game.Score, Is.Zero); Assert.That(game.LinesCleared, Is.Zero);
            game.RotateClockwise(); game.HardDrop();
            Assert.That(game.Score, Is.EqualTo(new[] { 0, 100, 300, 500, 800 }[rows]));
            Assert.That(game.LinesCleared, Is.EqualTo(rows));
            Assert.That(game.GravityInterval, Is.EqualTo(0.8f * Mathf.Pow(0.85f, rows)).Within(0.00001));
            Assert.That(Count(board), Is.EqualTo(4 - rows));
        }

        [Test]
        public void ClearingLinesBeforePenaltyCanMakeRoomAtTheTop()
        {
            var session = AtFace(1); var board = session.Game.Board;
            board.TryLock(TetrominoType.L, Enumerable.Range(0, 10).Select(x => new Vector2Int(x, 0)).ToArray());
            board.TryLock(TetrominoType.Z, new[] { new Vector2Int(0, 19) });
            session.AdvanceActiveTime(30); session.Game.HardDrop();
            Assert.That(session.Game.IsGameOver, Is.False);
            Assert.That(board[0, 19], Is.EqualTo(TetrominoType.Z));
            Assert.That(session.Game.Score, Is.EqualTo(100));
            Assert.That(session.Game.LinesCleared, Is.EqualTo(1));
        }

        [Test]
        public void OverflowEndsSessionOnThePenalizedFaceWithoutDrawingAnotherPiece()
        {
            var source = new ConstantPiece(); var session = new CubotrisSession(source);
            for (int i = 0; i < 3; i++) session.Game.HardDrop();
            session.Game.Board.TryLock(TetrominoType.Z, new[] { new Vector2Int(0, 19) });
            session.AdvanceActiveTime(30); int draws = source.Draws;
            session.Game.HardDrop();
            Assert.That(session.Game.IsGameOver, Is.True);
            Assert.That(session.ActiveFace, Is.EqualTo(3));
            Assert.That(session.Game.HasActivePiece, Is.False);
            Assert.That(source.Draws, Is.EqualTo(draws));
            Assert.That(session.Faces[3][0, 19], Is.EqualTo(TetrominoType.Z));
            Assert.That(Count(session.Faces[3]), Is.EqualTo(5));
            var before = session.Faces.Select(Snapshot).ToArray();
            session.AdvanceActiveTime(100); session.Game.HardDrop(); session.Game.Tick(100);
            Assert.That(session.SurvivalSeconds, Is.EqualTo(30));
            CollectionAssert.AreEqual(before, session.Faces.Select(Snapshot).ToArray());
        }

        [Test]
        public void ExcessivePendingRowsEndGameSafelyAndANewSessionResetsEverything()
        {
            var session = AtFace(3); session.AdvanceActiveTime(240);
            Assert.That(session.PendingGarbageRows(3), Is.EqualTo(24));
            session.Game.HardDrop();
            Assert.That(session.Game.IsGameOver, Is.True);
            Assert.That(Count(session.Faces[3]), Is.EqualTo(4));
            var fresh = new CubotrisSession();
            Assert.That(fresh.SurvivalSeconds, Is.Zero);
            for (int face = 0; face < 4; face++)
            {
                Assert.That(fresh.PendingGarbageRows(face), Is.Zero);
                Assert.That(fresh.FaceElapsedSeconds(face), Is.Zero);
                Assert.That(Count(fresh.Faces[face]), Is.Zero);
            }
        }

        [Test]
        public void GarbageRandomnessDoesNotChangeTheSevenBagOrItsPreview()
        {
            var source = new SevenBag(95); var expected = new SevenBag(95);
            var session = new CubotrisSession(source, garbageSeed: 95);
            for (int i = 0; i < 12; i++)
            {
                Assert.That(session.Game.Active.Type, Is.EqualTo(expected.Next()));
                if (i == 1) session.AdvanceActiveTime(30);
                var preview = session.Game.NextPiece;
                session.Game.HardDrop();
                Assert.That(session.Game.IsGameOver, Is.False);
                Assert.That(session.Game.Active.Type, Is.EqualTo(preview));
            }
        }

        [TestCase(0, "00:00")] [TestCase(30, "00:30")] [TestCase(135, "02:15")]
        [TestCase(640, "10:40")] [TestCase(3599.9, "59:59")] [TestCase(3600, "60:00")] [TestCase(6000, "100:00")]
        public void SurvivalTimeUsesWholeMinutesAndSeconds(double seconds, string expected)
        { Assert.That(GameHudView.FormatTime(seconds), Is.EqualTo(expected)); }

        private static CubotrisSession AtFace(int face)
        {
            var session = new CubotrisSession(new ConstantPiece(), garbageSeed: 42);
            for (int i = 0; i < face; i++) session.Game.HardDrop();
            return session;
        }
        private static int Count(BoardState board) => Enumerable.Range(0, 200).Count(i => board[i % 10, i / 10].HasValue);
        private static string Snapshot(BoardState board) => string.Join(",", Enumerable.Range(0, 200).Select(i => board[i % 10, i / 10]?.ToString() ?? "-"));
        private static void CheckHoles(BoardState board, int rows)
        {
            int previous = -1;
            for (int y = 0; y < rows; y++)
            {
                var holes = Enumerable.Range(0, 10).Where(x => !board[x, y].HasValue).ToArray();
                Assert.That(holes.Length, Is.EqualTo(1));
                Assert.That(holes[0], Is.Not.EqualTo(previous)); previous = holes[0];
            }
        }
    }
}
