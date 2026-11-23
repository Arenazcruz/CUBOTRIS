using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
namespace Cubotris.Tests
{
    public class ComboTests
    {
        private static void Clear(CubotrisSession session,int rows=1)
        {for(int y=0;y<rows;y++)session.Game.Board.TryLock(TetrominoType.I,Enumerable.Range(0,10).Select(x=>new Vector2Int(x,y)).ToArray());session.Game.HardDrop();}
        [Test] public void ConsecutivePlacementsAcrossFacesAwardIncreasingBonusWithoutMultiplyingBaseScore()
        {
            var session=new CubotrisSession();Clear(session);Assert.That(session.Game.ComboChain,Is.EqualTo(1));Assert.That(session.Game.LastComboBonus,Is.Zero);
            Clear(session,2);Assert.That(session.Game.ComboChain,Is.EqualTo(2));Assert.That(session.Game.LastComboBonus,Is.EqualTo(50));Assert.That(session.Game.LineScore,Is.EqualTo(400));
            Clear(session);Assert.That(session.Game.LastComboBonus,Is.EqualTo(100));Assert.That(session.Game.ComboBonusScore,Is.EqualTo(150));Assert.That(session.Game.Score,Is.EqualTo(650));
            session.Game.HardDrop();Assert.That(session.Game.ComboChain,Is.Zero);Assert.That(session.Game.LastComboBonus,Is.Zero);Assert.That(session.Game.MaxCombo,Is.EqualTo(3));
        }
        [Test] public void MovementHoldAndTimeDoNotBreakOrAwardACombo()
        {
            var session=new CubotrisSession();Clear(session);int score=session.Game.Score;
            session.Game.Move(-1);session.Game.RotateClockwise();session.Game.Hold();session.AdvanceActiveTime(1);
            Assert.That(session.Game.Score,Is.EqualTo(score));Assert.That(session.Game.ComboChain,Is.EqualTo(1));
            Clear(session);Assert.That(session.Game.ComboChain,Is.EqualTo(2));
        }
    }
}
