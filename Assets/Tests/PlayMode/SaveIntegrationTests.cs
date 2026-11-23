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
        [UnityTest] public IEnumerator SaveDuringTransitionContinuesPausedWithTheSameRunAndHold()
        {
            var game=Object.FindAnyObjectByType<GameController>();yield return Tap(Key.C);game.HardDrop();yield return null;yield return Tap(Key.Escape);
            Assert.That(game.IsPaused,Is.True);string id=game.Session.RunId;
            // Normalize through the same JSON precision used on disk (sub-nanosecond double rounding).
            var snapshot=JsonUtility.ToJson(JsonUtility.FromJson<SessionSnapshot>(JsonUtility.ToJson(game.Session.Capture())));
            ButtonAt("PausePanel/PanelBackground/SaveButton").onClick.Invoke();yield return null;yield return null;
            Selected("GameCanvas/SaveAndExitPanel/Panel/Cancel");yield return Tap(Key.UpArrow);yield return Tap(Key.Enter);
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));Assert.That(GameSave.Store.HasSave,Is.True);Assert.That(Time.timeScale,Is.EqualTo(1));
            GameObject.Find("Canvas/MenuButtons/ContinueButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            Selected("Canvas/ContinuePanel/Panel/Resume");yield return Tap(Key.Enter);
            game=Object.FindAnyObjectByType<GameController>();Assert.That(game.Session.RunId,Is.EqualTo(id));Assert.That(game.IsPaused,Is.True);Assert.That(Time.timeScale,Is.Zero);
            Assert.That(JsonUtility.ToJson(game.Session.Capture()),Is.EqualTo(snapshot));Assert.That(game.IsTransitioning,Is.False);
            Selected("GameCanvas/PausePanel/PanelBackground/ResumeButton");yield return Tap(Key.Enter);Assert.That(game.AcceptsInput,Is.True);Assert.That(Time.timeScale,Is.EqualTo(1));
            yield return LoseGame(game);Assert.That(GameSave.Store.HasSave,Is.False);Assert.That(game.FinishedRecord.id,Is.EqualTo(id));
        }
        [UnityTest] public IEnumerator SaveWriteFailureKeepsThePausedGameAndCancelReturnsFocus()
        {
            var game=Object.FindAnyObjectByType<GameController>();GameSave.Store.TrySave(game.Session,out _);string old=System.IO.File.ReadAllText(GameSave.Store.FilePath);
            System.IO.Directory.CreateDirectory(GameSave.Store.FilePath+".tmp");yield return Tap(Key.Escape);
            ButtonAt("PausePanel/PanelBackground/SaveButton").onClick.Invoke();yield return null;yield return null;
            var panel=Object.FindAnyObjectByType<SaveAndExitPanel>();panel.SaveAndExit();yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("Game"));Assert.That(game.IsPaused,Is.True);Assert.That(panel.message.text,Does.Contain("No se pudo guardar"));
            Assert.That(System.IO.File.ReadAllText(GameSave.Store.FilePath),Is.EqualTo(old));yield return Tap(Key.Escape);Selected("GameCanvas/PausePanel/PanelBackground/SaveButton");
        }
    }
}
