using System.Collections;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace Cubotris.Tests
{
    public sealed partial class GameIntegrationTests
    {
        [UnityTest] public IEnumerator CustomControlsCaptureDoesNotMoveBoardAndEscClosesOnlyTheOverlay()
        {
            var game=Object.FindAnyObjectByType<GameController>();
            yield return Tap(Key.Escape);
            ButtonAt("PausePanel/PanelBackground/OptionsButton").onClick.Invoke();yield return null;yield return null;
            ButtonAt("PauseOptionsPanel/PanelBackground/ControlsButton").onClick.Invoke();yield return null;yield return null;
            var panel=Object.FindAnyObjectByType<ControlsPanel>();Assert.That(panel,Is.Not.Null);
            int revision=game.Model.Revision;panel.primaryButtons[0].onClick.Invoke();yield return null;
            yield return Tap(Key.J);
            Assert.That(ControlBindings.Current[0].primary,Is.EqualTo(Key.J));
            Assert.That(game.Model.Revision,Is.EqualTo(revision));Assert.That(game.IsPaused,Is.True);
            yield return Tap(Key.Escape);
            Assert.That(panel.gameObject.activeSelf,Is.False);
            Assert.That(GameObject.Find("GameCanvas/PauseOptionsPanel"),Is.Not.Null);
            yield return Tap(Key.Escape);yield return Tap(Key.Escape);
            int x=game.Model.Active.Position.x;yield return Tap(Key.A);Assert.That(game.Model.Active.Position.x,Is.EqualTo(x));
            yield return Tap(Key.J);Assert.That(game.Model.Active.Position.x,Is.EqualTo(x-1));
        }
        [UnityTest] public IEnumerator MainMenuControlsRestoreAndCancelCaptureWithKeyboard()
        {
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            GameObject.Find("Canvas/MenuButtons/OptionsButton").GetComponent<Button>().onClick.Invoke();yield return null;
            GameObject.Find("Canvas/OptionsPanel/PanelBackground/ControlsButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            var panel=Object.FindAnyObjectByType<ControlsPanel>();
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(panel.primaryButtons[0].gameObject));
            yield return Tap(Key.Enter);yield return Tap(Key.Escape);
            Assert.That(panel.gameObject.activeSelf,Is.True);Assert.That(ControlBindings.Capturing,Is.False);
            ControlBindings.TryRebind(GameAction.Left,false,Key.J,out _);panel.RestoreDefaults();
            Assert.That(ControlBindings.Current[0].primary,Is.EqualTo(Key.A));
            yield return Tap(Key.Escape);Assert.That(SettingsOverlay.OpenCount,Is.Zero);
        }
    }
}
