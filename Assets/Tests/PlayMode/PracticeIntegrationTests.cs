using System.Collections;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Cubotris.Tests
{
    public sealed partial class GameIntegrationTests
    {
        [UnityTest] public IEnumerator PracticeCanStartRestartAndLeaveWithoutCreatingARecord()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            GameObject.Find("Canvas/MenuButtons/ModeButton").GetComponent<Button>().onClick.Invoke();yield return null;
            GameObject.Find("Canvas/DifficultyPanel/Panel/Practice").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            Selected("Canvas/PracticePanel/Panel/Face");yield return Tap(Key.Enter);yield return Tap(Key.Enter);
            yield return Tap(Key.DownArrow);yield return Tap(Key.Enter);yield return Tap(Key.DownArrow);yield return Tap(Key.Enter);
            var game=Object.FindAnyObjectByType<GameController>();Assert.That(game.Session.IsPractice,Is.True);Assert.That(game.Session.ActiveFace,Is.EqualTo(2));Assert.That(game.Session.Mode,Is.EqualTo(DifficultyMode.Relaxed));
            game.HardDrop();yield return null;yield return null;Assert.That(game.IsTransitioning,Is.False);Assert.That(game.Session.ActiveFace,Is.EqualTo(2));
            ButtonAt("PracticeRestartButton").onClick.Invoke();yield return null;yield return null;Assert.That(game.Model.PiecesLocked,Is.Zero);Assert.That(game.Session.ActiveFace,Is.EqualTo(2));
            yield return LoseGame(game);yield return null;yield return null;
            Assert.That(game.FinishedRecord.practice,Is.True);Assert.That(BestScoreStore.History.Records,Is.Empty);
            yield return Tap(Key.DownArrow);yield return Tap(Key.Enter);Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));
            GameObject.Find("Canvas/MenuButtons/PlayButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            Assert.That(Object.FindAnyObjectByType<GameController>().Session.IsPractice,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));
        }
    }
}
