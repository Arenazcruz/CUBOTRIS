using System.Collections;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace Cubotris.Tests
{
    public sealed partial class GameIntegrationTests
    {
        [UnityTest] public IEnumerator DisplayPreviewCanBeCancelledDuringPauseWithoutUnpausing()
        {
            var game=Object.FindAnyObjectByType<GameController>();yield return Tap(Key.Escape);
            ButtonAt("PausePanel/PanelBackground/OptionsButton").onClick.Invoke();yield return null;yield return null;
            ButtonAt("PauseOptionsPanel/PanelBackground/DisplayButton").onClick.Invoke();yield return null;yield return null;
            var panel=Object.FindAnyObjectByType<DisplayPanel>();Assert.That(panel,Is.Not.Null);
            panel.Next();panel.ToggleMode();panel.Apply();yield return null;yield return null;
            Assert.That(panel.keepButton.gameObject.activeSelf,Is.True);Assert.That(Time.timeScale,Is.Zero);
            yield return Tap(Key.Escape);Assert.That(panel.gameObject.activeSelf,Is.False);Assert.That(game.IsPaused,Is.True);
            Assert.That(GameObject.Find("GameCanvas/PauseOptionsPanel"),Is.Not.Null);
        }
        [UnityTest] public IEnumerator DisplayConfirmationSavesAndReloadsTheSelectedMode()
        {
            bool had=PlayerPrefs.HasKey(DisplayPreferences.Key);string previous=PlayerPrefs.GetString(DisplayPreferences.Key);
            try
            {
                yield return Tap(Key.Escape);ButtonAt("PausePanel/PanelBackground/OptionsButton").onClick.Invoke();yield return null;yield return null;
                ButtonAt("PauseOptionsPanel/PanelBackground/DisplayButton").onClick.Invoke();yield return null;yield return null;
                var panel=Object.FindAnyObjectByType<DisplayPanel>();panel.Next();panel.ToggleMode();panel.Apply();panel.Keep();
                Assert.That(PlayerPrefs.HasKey(DisplayPreferences.Key),Is.True);Assert.That(DisplayPreferences.Load(default).IsValid,Is.True);
                panel.Close();
            }
            finally {if(had)PlayerPrefs.SetString(DisplayPreferences.Key,previous);else PlayerPrefs.DeleteKey(DisplayPreferences.Key);PlayerPrefs.Save();}
        }
    }
}
