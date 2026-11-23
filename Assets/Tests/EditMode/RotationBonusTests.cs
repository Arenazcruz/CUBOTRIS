using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
namespace Cubotris.Tests
{
    public class RotationBonusTests
    {
        private static void Clear(CubotrisSession session)
        { session.Game.Board.TryLock(TetrominoType.I,Enumerable.Range(0,10).Select(x=>new Vector2Int(x,0)).ToArray());session.Game.HardDrop(); }
        [Test] public void FourSuccessfulFacesAwardOnceAndStartANewObjective()
        {
            var session=new CubotrisSession();
            for(int i=0;i<3;i++){Clear(session);Assert.That(session.CycleClearMask,Is.EqualTo((1<<(i+1))-1));Assert.That(session.CycleBonusScore,Is.Zero);}
            Clear(session);Assert.That(session.CycleBonusScore,Is.EqualTo(400));Assert.That(session.Game.Score,Is.EqualTo(1100));Assert.That(session.Game.LineScore,Is.EqualTo(400));
            Assert.That(session.CompletedRotations,Is.EqualTo(1));Assert.That(session.CycleClearMask,Is.Zero);
            for(int i=0;i<4;i++)session.Game.HardDrop();Assert.That(session.CycleBonusScore,Is.EqualTo(400));Assert.That(session.LastRotationBonus,Is.Zero);
        }
        [Test] public void DifferentRotationsCannotCombineMissingFacesAndHoldDoesNotAdvanceProgress()
        {
            var session=new CubotrisSession();Clear(session);Clear(session);Clear(session);session.Game.HardDrop();
            Assert.That(session.CycleClearMask,Is.Zero);Assert.That(session.CycleBonusScore,Is.Zero);
            session.Game.Hold();Assert.That(session.CompletedRotations,Is.EqualTo(1));
            session.Game.HardDrop();session.Game.HardDrop();session.Game.HardDrop();Clear(session);Assert.That(session.CycleBonusScore,Is.Zero);
        }
        [Test] public void PracticeDoesNotAwardFourFaceBonus()
        {var session=new CubotrisSession(practiceFace:3);for(int i=0;i<4;i++)Clear(session);Assert.That(session.CycleBonusScore,Is.Zero);Assert.That(session.CompletedRotations,Is.Zero);}
    }
}
