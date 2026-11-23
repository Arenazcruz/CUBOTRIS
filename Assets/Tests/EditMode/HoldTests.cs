using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
namespace Cubotris.Tests
{
    public class HoldTests
    {
        [Test] public void FirstHoldConsumesPreviewAndDoesNotAdvanceFaceOrScore()
        {
            var session=new CubotrisSession(source:new SevenBag(42));var game=session.Game;
            var piece=game.Active;var next=game.NextPiece;var color=game.NextPieceColor;
            Assert.That(game.Hold(),Is.True);Assert.That(game.Active.Type,Is.EqualTo(next));Assert.That(game.Active.Color,Is.EqualTo(color));
            Assert.That(game.HeldPiece,Is.EqualTo(piece.Type));Assert.That(game.HeldPieceColor,Is.EqualTo(piece.Color));
            Assert.That(session.ActiveFace,Is.Zero);Assert.That(game.Score,Is.Zero);Assert.That(game.PiecesLocked,Is.Zero);
            Assert.That(game.Hold(),Is.False);
        }
        [Test] public void SwapAfterPlacementPreservesColorAndDoesNotConsumePreview()
        {
            var session=new CubotrisSession(source:new SevenBag(13));var game=session.Game;var original=game.Active;
            game.Hold();game.HardDrop();game.Move(-1);game.RotateClockwise();
            var outgoing=game.Active;var next=game.NextPiece;
            Assert.That(game.Hold(),Is.True);Assert.That(game.Active.Type,Is.EqualTo(original.Type));Assert.That(game.Active.Color,Is.EqualTo(original.Color));
            Assert.That(game.Active.Rotation,Is.Zero);Assert.That(game.Active.Position,Is.EqualTo(new Vector2Int(4,18)));
            Assert.That(game.HeldPiece,Is.EqualTo(outgoing.Type));Assert.That(game.HeldPieceColor,Is.EqualTo(outgoing.Color));
            Assert.That(game.NextPiece,Is.EqualTo(next));Assert.That(session.ActiveFace,Is.EqualTo(1));Assert.That(game.Hold(),Is.False);
        }
        [Test] public void BlockedReplacementEndsGameWithoutOverwritingBoard()
        {
            var game=new TetrisGame(source:new SevenBag(7));
            var occupied=TetrominoData.Cells(game.NextPiece,0,new Vector2Int(4,18));game.Board.TryLock(TetrominoType.I,occupied);
            Assert.That(game.Hold(),Is.True);Assert.That(game.IsGameOver,Is.True);Assert.That(game.Hold(),Is.False);Assert.That(game.PiecesLocked,Is.Zero);
        }
        [Test] public void MovementRotationGhostAndLockKeepTheAssignedColor()
        {
            var game=new TetrisGame(source:new SevenBag(3));var color=game.Active.Color;
            game.Move(-1);game.RotateClockwise();game.StepDown();Assert.That(game.Active.Color,Is.EqualTo(color));
            var landing=game.GetLandingPiece().Value;Assert.That(landing.Color,Is.EqualTo(color));game.HardDrop();
            foreach(var cell in landing.Cells)Assert.That(game.Board.BlockColorAt(cell.x,cell.y),Is.EqualTo(color));
        }
    }
}
