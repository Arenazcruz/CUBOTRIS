using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace Cubotris.Tests
{
    public sealed class PreviewAndGhostTests
    {
        private sealed class ConstantPiece : IPieceSource
        {
            private readonly TetrominoType type;
            public ConstantPiece(TetrominoType type) { this.type = type; }
            public TetrominoType Peek() => type;
            public TetrominoType Next() => type;
        }

        [Test]
        public void RepeatedPeekDoesNotConsumeOrReorderSevenBagsIncludingTheirBoundaries()
        {
            var preview = new SevenBag(93); var reference = new SevenBag(93);
            for (int i = 0; i < 700; i++)
            {
                var expected = reference.Next();
                for (int read = 0; read < 20; read++) Assert.That(preview.Peek(), Is.EqualTo(expected));
                Assert.That(preview.Next(), Is.EqualTo(expected));
            }
        }

        [Test]
        public void PreviewBecomesTheActivePieceAcrossFacesWithoutExtraDraws()
        {
            var session = new CubotrisSession(new SevenBag(812));
            for (int i = 0; i < 20; i++)
            {
                var next = session.Game.NextPiece;
                for (int read = 0; read < 100; read++) Assert.That(session.Game.NextPiece, Is.EqualTo(next));
                session.Game.HardDrop();
                Assert.That(session.Game.IsGameOver, Is.False);
                Assert.That(session.Game.Active.Type, Is.EqualTo(next));
                Assert.That(session.ActiveFace, Is.EqualTo((i + 1) % 4));
            }
        }

        [TestCase(TetrominoType.I)] [TestCase(TetrominoType.O)] [TestCase(TetrominoType.T)]
        [TestCase(TetrominoType.S)] [TestCase(TetrominoType.Z)] [TestCase(TetrominoType.J)] [TestCase(TetrominoType.L)]
        public void GhostMatchesHardDropAtBothWallsAfterEveryRotationWithoutMutatingTheGame(TetrominoType type)
        {
            for (int rotation = 0; rotation < 4; rotation++)
                foreach (int side in new[] { -1, 1 })
                {
                    var board = new BoardState();
                    board.TryLock(TetrominoType.O, new[] { new Vector2Int(0, 2), new Vector2Int(2, 4), new Vector2Int(7, 3), new Vector2Int(9, 1) });
                    var game = new TetrisGame(board, new ConstantPiece(type));
                    for (int r = 0; r < rotation; r++) game.RotateClockwise();
                    while (game.Move(side)) { }
                    int revision = game.Revision, version = board.Version;
                    var active = game.Active;
                    var ghost = game.GetLandingPiece().Value;
                    Assert.That(board.CanPlace(ghost.Cells), Is.True);
                    Assert.That(board.CanPlace(ghost.Cells.Select(c => c + Vector2Int.down).ToArray()), Is.False);
                    Assert.That(game.Revision, Is.EqualTo(revision));
                    Assert.That(board.Version, Is.EqualTo(version));
                    Assert.That(game.Active.Position, Is.EqualTo(active.Position));
                    Assert.That(game.Active.Rotation, Is.EqualTo(active.Rotation));
                    int distance = game.HardDrop();
                    Assert.That(distance, Is.EqualTo(active.Position.y - ghost.Position.y));
                    foreach (var cell in ghost.Cells) Assert.That(board[cell.x, cell.y], Is.EqualTo(type));
                }
        }

        [Test]
        public void GhostQueriesDoNotAdvanceOrResetGravity()
        {
            var game = new TetrisGame(new BoardState(), new ConstantPiece(TetrominoType.O));
            game.Tick(0.4f);
            for (int i = 0; i < 100; i++) game.GetLandingPiece();
            game.Tick(0.39f); Assert.That(game.Active.Position.y, Is.EqualTo(18));
            game.Tick(0.02f); Assert.That(game.Active.Position.y, Is.EqualTo(17));
        }

        [Test]
        public void GameOverHasNoLandingProjection()
        {
            var board = new BoardState(); board.TryLock(TetrominoType.Z, new[] { new Vector2Int(4, 18) });
            var game = new TetrisGame(board, new ConstantPiece(TetrominoType.O));
            Assert.That(game.IsGameOver, Is.True);
            Assert.That(game.GetLandingPiece(), Is.Null);
        }
    }
}
