using System.Collections;
using System.Linq;
using Cubotris.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cubotris.Tests
{
    public sealed class GameIntegrationTests
    {
        private Keyboard keyboard;
        private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            // Batch-mode has no focused Game View. Route only this test's simulated keys to the player.
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard = InputSystem.AddDevice<Keyboard>();
            keyboard.MakeCurrent();
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneContainsValidSpritesAndFourActivePrefabBlocks()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            Assert.That(game.Model, Is.Not.Null);
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<Component>(true)).All(c => c != null), Is.True);
            var active = GameObject.Find("BoardRoot/ActivePieceRoot").transform;
            Assert.That(active.childCount, Is.EqualTo(4));
            foreach (var block in active.GetComponentsInChildren<SpriteRenderer>()) Assert.That(block.sprite, Is.Not.Null);
            Assert.That(GameObject.Find("BoardRoot/PlacedBlocksRoot").transform.childCount, Is.Zero);
            Assert.That(Object.FindObjectsByType<AudioSource>().Length, Is.Zero);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HeldHorizontalInputRepeatsAndOpposingKeysCancel()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            int before = game.Model.Active.Position.x;
            Press(Key.A, Key.LeftArrow);
            yield return null;
            yield return null;
            Assert.That(keyboard.aKey.isPressed, Is.True, "Queued key must reach Input System");
            Assert.That(game.Model.Active.Position.x, Is.EqualTo(before - 1), "Aliases should not move twice");
            int first = game.Model.Active.Position.x;
            yield return new WaitForSeconds(0.36f);
            Assert.That(game.Model.Active.Position.x, Is.LessThan(first));
            Press(); yield return null; yield return null;
            int stopped = game.Model.Active.Position.x;
            Press(Key.A, Key.D); yield return null; yield return null;
            yield return new WaitForSeconds(0.2f);
            Assert.That(game.Model.Active.Position.x, Is.EqualTo(stopped));
        }

        [UnityTest]
        public IEnumerator RotationIsEdgeTriggeredAndSoftDropAccelerates()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            Press(Key.W, Key.UpArrow); yield return null; yield return null;
            Assert.That(keyboard.wKey.isPressed, Is.True, "Queued key must reach Input System");
            Assert.That(game.Model.Active.Rotation, Is.EqualTo(1));
            yield return new WaitForSeconds(0.2f);
            Assert.That(game.Model.Active.Rotation, Is.EqualTo(1));
            Press(); yield return null; yield return null;
            int y = game.Model.Active.Position.y;
            Press(Key.S, Key.DownArrow);
            yield return new WaitForSeconds(0.19f);
            Assert.That(game.Model.Active.Position.y, Is.LessThanOrEqualTo(y - 3));
        }

        [UnityTest]
        public IEnumerator HoldingSpaceLocksOnlyOnceAndReleaseAllowsNextDrop()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            Press(Key.Space); yield return null; yield return null;
            Assert.That(keyboard.spaceKey.isPressed, Is.True, "Queued key must reach Input System");
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(1));
            yield return new WaitForSeconds(0.2f);
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(1));
            Assert.That(GameObject.Find("BoardRoot/PlacedBlocksRoot").transform.childCount, Is.EqualTo(4));
            Press(); yield return null; yield return null;
            Press(Key.Space); yield return null; yield return null;
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator GameOverShowsNoticeAndPreventsFurtherInput()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            for (int i = 0; i < 25 && !game.Model.IsGameOver; i++) { game.HardDrop(); yield return null; }
            Assert.That(game.Model.IsGameOver, Is.True);
            Assert.That(GameObject.Find("GameCanvas/GameOverNotice"), Is.Not.Null);
            Assert.That(GameObject.Find("GameCanvas/GameInfoPanel/StateValue").GetComponent<Text>().text, Is.EqualTo("PARTIDA TERMINADA"));
            int locked = game.Model.PiecesLocked;
            int revision = game.Model.Revision;
            Press(Key.Space, Key.A, Key.W, Key.S);
            yield return new WaitForSeconds(0.25f);
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(locked));
            Assert.That(game.Model.Revision, Is.EqualTo(revision));
        }

        [UnityTest]
        public IEnumerator MainMenuOptionsPersistAndPlayLoadsGame()
        {
            const string musicKey = "Cubotris.Audio.MusicVolumePercent";
            const string effectsKey = "Cubotris.Audio.EffectsVolumePercent";
            bool hadMusic = PlayerPrefs.HasKey(musicKey), hadEffects = PlayerPrefs.HasKey(effectsKey);
            int oldMusic = PlayerPrefs.GetInt(musicKey), oldEffects = PlayerPrefs.GetInt(effectsKey);
            try
            {
                yield return SceneManager.LoadSceneAsync("MainMenu");
                yield return null;
                var canvas = GameObject.Find("Canvas").transform;
                var open = canvas.Find("MenuButtons/OptionsButton").GetComponent<Button>();
                var play = canvas.Find("MenuButtons/PlayButton").GetComponent<Button>();
                var exit = canvas.Find("MenuButtons/ExitButton").GetComponent<Button>();
                Assert.That(exit.onClick.GetPersistentMethodName(0), Is.EqualTo("Exit"));
                open.onClick.Invoke();
                var panel = canvas.Find("OptionsPanel");
                Assert.That(panel.gameObject.activeSelf, Is.True);
                var music = panel.Find("PanelBackground/MusicVolume/Slider").GetComponent<Slider>();
                var effects = panel.Find("PanelBackground/EffectsVolume/Slider").GetComponent<Slider>();
                var back = panel.Find("PanelBackground/BackButton").GetComponent<Button>();
                music.value = 29; effects.value = 64;
                Assert.That(panel.Find("PanelBackground/MusicVolume/ValueText").GetComponent<Text>().text, Is.EqualTo("29 %"));
                Assert.That(PlayerPrefs.GetInt(effectsKey), Is.EqualTo(64));
                back.onClick.Invoke();
                Assert.That(panel.gameObject.activeSelf, Is.False);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(open.gameObject));
                open.onClick.Invoke();
                Assert.That(music.value, Is.EqualTo(29));
                Assert.That(effects.value, Is.EqualTo(64));
                back.onClick.Invoke();
                play.onClick.Invoke();
                yield return null;
                yield return null;
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Game"));
                Assert.That(Object.FindAnyObjectByType<GameController>().Model.HasActivePiece, Is.True);
            }
            finally
            {
                if (hadMusic) PlayerPrefs.SetInt(musicKey, oldMusic); else PlayerPrefs.DeleteKey(musicKey);
                if (hadEffects) PlayerPrefs.SetInt(effectsKey, oldEffects); else PlayerPrefs.DeleteKey(effectsKey);
                PlayerPrefs.Save();
            }
        }

        private void Press(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    }
}
