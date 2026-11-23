using System.Collections;
using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace Cubotris.Tests
{
    public sealed partial class GameIntegrationTests
    {
        [UnityTest] public IEnumerator ComboHudKeepsChainDuringPauseAndClearsItOnRestart()
        {
            var game=Object.FindAnyObjectByType<GameController>();
            for(int i=0;i<2;i++){game.Model.Board.TryLock(TetrominoType.I,Enumerable.Range(0,10).Select(x=>new Vector2Int(x,0)).ToArray());game.HardDrop();yield return Settled(game);}
            var view=Object.FindAnyObjectByType<ComboView>();Assert.That(view.value.text,Is.EqualTo("RACHA 2"));Assert.That(view.detail.text,Does.Contain("+50"));Assert.That(game.Model.Score,Is.EqualTo(250));
            yield return Tap(Key.Escape);yield return new WaitForSecondsRealtime(.1f);Assert.That(game.Model.ComboChain,Is.EqualTo(2));
            ButtonAt("PausePanel/PanelBackground/RestartButton").onClick.Invoke();yield return null;yield return null;Assert.That(game.Model.ComboChain,Is.Zero);Assert.That(game.Model.ComboBonusScore,Is.Zero);Assert.That(view.value.text,Is.EqualTo("SIN RACHA"));
        }
    }
}
