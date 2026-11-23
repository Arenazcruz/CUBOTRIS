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
        [UnityTest] public IEnumerator CompletedRunIsCountedWithoutSavingARecordAndStatisticsNavigateWithKeyboard()
        {
            var game=Object.FindAnyObjectByType<GameController>();game.Session.AdvanceActiveTime(60);yield return LoseGame(game);yield return null;
            var totals=PersonalStats.Store.Summarize();Assert.That(totals.Played,Is.EqualTo(1));Assert.That(totals.Completed,Is.EqualTo(1));Assert.That(totals.AverageCompletedSeconds,Is.GreaterThanOrEqualTo(60));Assert.That(BestScoreStore.History.Records,Is.Empty);
            ButtonAt("GameOverNotice/PanelBackground/MainMenuButton").onClick.Invoke();yield return null;yield return null;
            ButtonAt("GameOverNotice/UnsavedDialog/PanelBackground/DiscardButton").onClick.Invoke();yield return null;yield return null;
            GameObject.Find("Canvas/MenuButtons/StatisticsButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            var panel=Object.FindAnyObjectByType<StatisticsPanel>();Assert.That(panel.totals.text,Does.Contain("PARTIDAS JUGADAS: 1"));Assert.That(panel.hardest.text,Does.Contain("CARA"));
            yield return Tap(Key.Enter);Assert.That(panel.filter.text,Does.Contain("PRÁCTICA"));Assert.That(panel.totals.text,Does.Contain("PARTIDAS JUGADAS: 0"));
            yield return Tap(Key.Escape);Selected("Canvas/MenuButtons/StatisticsButton");
        }
        [UnityTest] public IEnumerator ContinuingAndCompletingOneCheckpointDoesNotCountTwoGames()
        {
            var game=Object.FindAnyObjectByType<GameController>();game.Session.AdvanceActiveTime(15);string id=game.Session.RunId;
            Assert.That(GameSave.Store.TrySave(game.Session,out _),Is.True);game.MarkSaved();yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            GameObject.Find("Canvas/MenuButtons/ContinueButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            yield return Tap(Key.Enter);game=Object.FindAnyObjectByType<GameController>();yield return Tap(Key.Enter);yield return LoseGame(game);yield return null;
            Assert.That(game.Session.RunId,Is.EqualTo(id));Assert.That(PersonalStats.Store.Summarize().Played,Is.EqualTo(1));Assert.That(PersonalStats.Store.Summarize().Completed,Is.EqualTo(1));
            Assert.That(new StatisticsStore(PersonalStats.Store.FilePath).Summarize().Completed,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator RestartCountsAnAbandonmentAndStartsANewRun()
        {
            var game=Object.FindAnyObjectByType<GameController>();string old=game.Session.RunId;game.Restart();yield return null;
            Assert.That(game.Session.RunId,Is.Not.EqualTo(old));var s=PersonalStats.Store.Summarize();Assert.That(s.Played,Is.EqualTo(2));Assert.That(s.Abandoned,Is.EqualTo(1));Assert.That(s.Suspended,Is.EqualTo(1));
        }
    }
}
