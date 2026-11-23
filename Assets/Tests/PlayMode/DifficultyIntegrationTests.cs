using System;
using System.Collections;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace Cubotris.Tests
{
    public sealed partial class GameIntegrationTests
    {
        [UnityTest] public IEnumerator DifficultySelectionLaunchesChosenRulesAndFiltersMenuRecord()
        {
            BestScoreStore.History.TryAdd(new ScoreRecord{id=Guid.NewGuid().ToString("N"),mode=DifficultyMode.Relaxed,score=900,finishedAtUtc=DateTimeOffset.UtcNow.ToString("O")},"Ana",out _);
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            GameObject.Find("Canvas/MenuButtons/ModeButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            Selected("Canvas/DifficultyPanel/Panel/Normal");yield return Tap(Key.UpArrow);yield return Tap(Key.Enter);
            Assert.That(GameSelection.Mode,Is.EqualTo(DifficultyMode.Relaxed));Assert.That(BestScoreStore.Best,Is.EqualTo(900));
            yield return Tap(Key.DownArrow);yield return Tap(Key.DownArrow);yield return Tap(Key.Enter);
            Assert.That(GameSelection.Mode,Is.EqualTo(DifficultyMode.Challenge));Assert.That(BestScoreStore.Best,Is.Zero);
            yield return Tap(Key.Escape);GameObject.Find("Canvas/MenuButtons/PlayButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            var game=Object.FindAnyObjectByType<GameController>();Assert.That(game.Session.Mode,Is.EqualTo(DifficultyMode.Challenge));Assert.That(game.Session.SecondsUntilGarbage(1),Is.EqualTo(20).Within(.001));
            game.Restart();yield return null;Assert.That(game.Session.Mode,Is.EqualTo(DifficultyMode.Challenge));
        }
    }
}
