using System.Collections;
using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Cubotris.Tests
{
    public sealed partial class GameIntegrationTests
    {
        [UnityTest] public IEnumerator RotationBonusUpdatesScoreAndHudThenResetsOnRestart()
        {
            var game=Object.FindAnyObjectByType<GameController>();
            for(int i=0;i<4;i++){game.Model.Board.TryLock(TetrominoType.I,Enumerable.Range(0,10).Select(x=>new Vector2Int(x,0)).ToArray());game.HardDrop();yield return Settled(game);}
            var view=Object.FindAnyObjectByType<RotationBonusView>();Assert.That(view.notice.text,Does.Contain("+400"));Assert.That(game.Model.Score,Is.EqualTo(1100));
            game.Restart();yield return null;yield return null;Assert.That(game.Model.Score,Is.Zero);Assert.That(game.Session.CycleClearMask,Is.Zero);Assert.That(view.notice.text,Does.Not.Contain("+400"));
        }
    }
}
