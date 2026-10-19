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
        private bool hadBestScore;
        private int bestScoreBefore;
        private InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            hadBestScore = PlayerPrefs.HasKey(BestScoreStore.Key);
            bestScoreBefore = PlayerPrefs.GetInt(BestScoreStore.Key);
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
            Time.timeScale = 1;
            var live = Object.FindAnyObjectByType<GameController>();
            if (live != null) Object.DestroyImmediate(live.gameObject);
            Restore(BestScoreStore.Key, hadBestScore, bestScoreBefore);
            PlayerPrefs.Save();
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
            ButtonAt("PauseButton").onClick.Invoke();
            Assert.That(game.IsPaused, Is.False);
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

        [UnityTest]
        public IEnumerator EveryShapeAndRotationFitsBothWallsAndFloorVisually()
        {
            var live = Object.FindAnyObjectByType<GameController>();
            live.enabled = false;
            Object.FindAnyObjectByType<TetrisInput>().enabled = false;
            var view = Object.FindAnyObjectByType<BoardView>();
            var board = view.transform;
            var grid = board.Find("BoardGrid").GetComponent<SpriteRenderer>().bounds;
            foreach (TetrominoType type in System.Enum.GetValues(typeof(TetrominoType)))
                for (int rotation = 0; rotation < 4; rotation++)
                    foreach (int side in new[] { -1, 1 })
                    {
                        var model = new TetrisGame(source: new SinglePiece(type));
                        for (int i = 0; i < rotation; i++) Assert.That(model.RotateClockwise(), Is.True);
                        while (model.Move(side)) { }
                        Assert.That(model.Move(side), Is.False);
                        Assert.That(side < 0 ? model.Active.Cells.Min(c => c.x) : model.Active.Cells.Max(c => c.x), Is.EqualTo(side < 0 ? 0 : 9));
                        while (model.Board.CanPlace(model.Active.Cells.Select(c => c + Vector2Int.down).ToArray())) model.StepDown();
                        Assert.That(model.Active.Cells.Min(c => c.y), Is.Zero);
                        view.Render(model);
                        CheckBounds(board.Find("ActivePieceRoot"), grid);
                        model.RotateClockwise();
                        Assert.That(model.Board.CanPlace(model.Active.Cells), Is.True, $"{type}/{rotation}/{side} after rotation at corner");
                        view.Render(model);
                        CheckBounds(board.Find("ActivePieceRoot"), grid);
                        model.HardDrop(); view.Render(model);
                        CheckBounds(board.Find("PlacedBlocksRoot"), grid);
                        yield return null;
                    }
        }

        [UnityTest]
        public IEnumerator PauseAndEscapeFreezeExactlyAndHeldKeysCannotLeakOnResume()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            Press(Key.A); yield return null; yield return null;
            Press(Key.A, Key.Escape, Key.Space, Key.S, Key.W); yield return null; yield return null;
            Assert.That(game.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            int revision = game.Model.Revision, locked = game.Model.PiecesLocked;
            var blocks = GameObject.Find("BoardRoot/ActivePieceRoot").GetComponentsInChildren<SpriteRenderer>().Select(r => r.bounds).ToArray();
            yield return new WaitForSecondsRealtime(0.9f);
            game.Move(1); game.Rotate(); game.HardDrop();
            Assert.That(game.Model.Revision, Is.EqualTo(revision));
            CollectionAssert.AreEqual(blocks, GameObject.Find("BoardRoot/ActivePieceRoot").GetComponentsInChildren<SpriteRenderer>().Select(r => r.bounds).ToArray());
            Press(Key.A, Key.Space, Key.S); yield return null; yield return null;
            Press(Key.A, Key.Space, Key.S, Key.Escape); yield return null; yield return null;
            Assert.That(game.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(game.Model.Revision, Is.EqualTo(revision));
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(locked));
            Press(); yield return null; yield return null;
            Press(Key.Space); yield return null; yield return null;
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(locked + 1));
        }

        [UnityTest]
        public IEnumerator PauseButtonsResumeAndRestartWithoutDoubleTransitions()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            game.HardDrop(); yield return null;
            var oldModel = game.Model;
            ButtonAt("PauseButton").onClick.Invoke(); yield return null;
            int revision = oldModel.Revision;
            ButtonAt("PausePanel/PanelBackground/ResumeButton").onClick.Invoke();
            ButtonAt("PausePanel/PanelBackground/RestartButton").onClick.Invoke();
            Assert.That(game.Model, Is.SameAs(oldModel));
            Assert.That(game.Model.Revision, Is.EqualTo(revision));
            yield return null; yield return null;
            ButtonAt("PauseButton").onClick.Invoke(); yield return null;
            ButtonAt("PausePanel/PanelBackground/RestartButton").onClick.Invoke();
            ButtonAt("PauseButton").onClick.Invoke();
            yield return null; yield return null;
            Assert.That(game.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(game.Model, Is.Not.SameAs(oldModel));
            Assert.That(game.Model.LinesCleared, Is.Zero);
            Assert.That(game.Model.Score, Is.Zero);
            Assert.That(game.Model.PiecesLocked, Is.Zero);
            Assert.That(game.Model.HasActivePiece, Is.True);
            Assert.That(GameObject.Find("BoardRoot/PlacedBlocksRoot").transform.childCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PauseOptionsSharePreferencesAndBackOrEscapeReturnsToPause()
        {
            const string musicKey = "Cubotris.Audio.MusicVolumePercent", effectsKey = "Cubotris.Audio.EffectsVolumePercent";
            bool hadMusic = PlayerPrefs.HasKey(musicKey), hadEffects = PlayerPrefs.HasKey(effectsKey);
            int oldMusic = PlayerPrefs.GetInt(musicKey), oldEffects = PlayerPrefs.GetInt(effectsKey);
            try
            {
                ButtonAt("PauseButton").onClick.Invoke(); yield return null;
                ButtonAt("PausePanel/PanelBackground/OptionsButton").onClick.Invoke(); yield return null;
                var panel = GameObject.Find("GameCanvas").transform.Find("PauseOptionsPanel");
                Assert.That(panel.gameObject.activeSelf, Is.True);
                var music = panel.Find("PanelBackground/MusicVolume/Slider").GetComponent<Slider>();
                var effects = panel.Find("PanelBackground/EffectsVolume/Slider").GetComponent<Slider>();
                music.value = 17; effects.value = 93;
                Assert.That(panel.Find("PanelBackground/MusicVolume/ValueText").GetComponent<Text>().text, Is.EqualTo("17 %"));
                Assert.That(PlayerPrefs.GetInt(musicKey), Is.EqualTo(17));
                Assert.That(PlayerPrefs.GetInt(effectsKey), Is.EqualTo(93));
                Press(Key.RightArrow); yield return null; yield return null;
                Assert.That(music.value, Is.EqualTo(18), "UI navigation must work at timeScale zero");
                Press(); yield return null; yield return null;
                ButtonAt("PauseOptionsPanel/PanelBackground/BackButton").onClick.Invoke(); yield return null;
                Assert.That(panel.gameObject.activeSelf, Is.False);
                Assert.That(Object.FindAnyObjectByType<GameController>().IsPaused, Is.True);
                ButtonAt("PausePanel/PanelBackground/OptionsButton").onClick.Invoke(); yield return null;
                Press(Key.Escape); yield return null; yield return null;
                Assert.That(panel.gameObject.activeSelf, Is.False);
                Assert.That(GameObject.Find("GameCanvas/PausePanel"), Is.Not.Null);
                Assert.That(Time.timeScale, Is.Zero);
                Press(); yield return null; yield return null;
                ButtonAt("PausePanel/PanelBackground/MainMenuButton").onClick.Invoke(); yield return null; yield return null;
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
                Assert.That(Time.timeScale, Is.EqualTo(1));
                GameObject.Find("Canvas/MenuButtons/OptionsButton").GetComponent<Button>().onClick.Invoke();
                var mainPanel = GameObject.Find("Canvas/OptionsPanel").transform;
                Assert.That(mainPanel.Find("PanelBackground/MusicVolume/Slider").GetComponent<Slider>().value, Is.EqualTo(18));
                Assert.That(mainPanel.Find("PanelBackground/EffectsVolume/Slider").GetComponent<Slider>().value, Is.EqualTo(93));
            }
            finally
            {
                Restore(musicKey, hadMusic, oldMusic); Restore(effectsKey, hadEffects, oldEffects);
                PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator ScoreAndRecordUpdateImmediatelySurviveRestartAndAppearInMainMenu()
        {
            bool had = PlayerPrefs.HasKey(BestScoreStore.Key); int old = PlayerPrefs.GetInt(BestScoreStore.Key);
            GameController game = null;
            try
            {
                PlayerPrefs.DeleteKey(BestScoreStore.Key);
                Assert.That(BestScoreStore.Best, Is.Zero);
                game = Object.FindAnyObjectByType<GameController>();
                for (int run = 1; run <= 2; run++)
                {
                    game.Model.Board.TryLock(TetrominoType.I, Enumerable.Range(0, 10).Where(x => !game.Model.Board[x, 0].HasValue).Select(x => new Vector2Int(x, 0)).ToArray());
                    game.HardDrop(); yield return null; yield return null;
                    Assert.That(game.Model.LinesCleared, Is.EqualTo(run));
                    Assert.That(game.Model.Score, Is.EqualTo(run * 100));
                    Assert.That(BestScoreStore.Best, Is.EqualTo(run * 100));
                    Assert.That(GameObject.Find("GameCanvas/GameInfoPanel/ScoreValue").GetComponent<Text>().text, Is.EqualTo((run * 100).ToString()));
                }
                ButtonAt("PauseButton").onClick.Invoke(); yield return null;
                ButtonAt("PausePanel/PanelBackground/RestartButton").onClick.Invoke(); yield return null; yield return null;
                Assert.That(game.Model.Score, Is.Zero);
                Assert.That(BestScoreStore.Best, Is.EqualTo(200));
                BestScoreStore.Record(100);
                Assert.That(BestScoreStore.Best, Is.EqualTo(200));
                ButtonAt("PauseButton").onClick.Invoke(); yield return null;
                ButtonAt("PausePanel/PanelBackground/MainMenuButton").onClick.Invoke(); yield return null; yield return null;
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(GameObject.Find("Canvas/BestScorePanel/Value").GetComponent<Text>().text, Is.EqualTo("200"));
                yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
                Assert.That(GameObject.Find("Canvas/BestScorePanel/Value").GetComponent<Text>().text, Is.EqualTo("200"));
            }
            finally
            {
                if (game != null) Object.DestroyImmediate(game.gameObject);
                Restore(BestScoreStore.Key, had, old); PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator PointerCanClickPauseAndResumeThroughTheCanvas()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            mouse.MakeCurrent();
            try
            {
                yield return Click(mouse, ButtonAt("PauseButton"));
                Assert.That(Object.FindAnyObjectByType<GameController>().IsPaused, Is.True);
                yield return Click(mouse, ButtonAt("PausePanel/PanelBackground/ResumeButton"));
                Assert.That(Object.FindAnyObjectByType<GameController>().IsPaused, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }

        [UnityTest]
        public IEnumerator GameOverDisplaysRealScoresAndPointerRetryCreatesOneCleanGame()
        {
            PlayerPrefs.DeleteKey(BestScoreStore.Key);
            var game = Object.FindAnyObjectByType<GameController>();
            yield return FinishScoredGame(game);
            var panel = GameObject.Find("GameCanvas/GameOverNotice").transform;
            Assert.That(game.Model.Score, Is.EqualTo(100));
            Assert.That(panel.Find("PanelBackground/FinalScoreValue").GetComponent<Text>().text, Is.EqualTo("100"));
            Assert.That(panel.Find("PanelBackground/BestScoreValue").GetComponent<Text>().text, Is.EqualTo("100"));
            Assert.That(BestScoreStore.Best, Is.EqualTo(100));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(ButtonAt("GameOverNotice/PanelBackground/RetryButton").gameObject));
            int revision = game.Model.Revision;
            Press(Key.Escape, Key.A, Key.W, Key.Space); yield return null; yield return null;
            Assert.That(game.Model.Revision, Is.EqualTo(revision));
            Assert.That(game.IsPaused, Is.False);
            Press(); yield return null; yield return null;
            var mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            try
            {
                var old = game.Model;
                var retry = ButtonAt("GameOverNotice/PanelBackground/RetryButton");
                var menu = ButtonAt("GameOverNotice/PanelBackground/MainMenuButton");
                Time.timeScale = 0;
                yield return Click(mouse, retry);
                Assert.That(game.Model, Is.Not.SameAs(old));
                var restarted = game.Model;
                retry.onClick.Invoke(); menu.onClick.Invoke();
                Assert.That(game.Model, Is.SameAs(restarted), "A duplicate callback must not restart twice or leave Game");
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Game"));
                Assert.That(Time.timeScale, Is.EqualTo(1));
                Assert.That(panel.gameObject.activeSelf, Is.False);
                Assert.That(game.Model.Score, Is.Zero);
                Assert.That(game.Model.LinesCleared, Is.Zero);
                Assert.That(game.Model.PiecesLocked, Is.Zero);
                Assert.That(game.Model.HasActivePiece, Is.True);
                Assert.That(game.AcceptsInput, Is.True);
                Assert.That(GameObject.Find("BoardRoot/PlacedBlocksRoot").transform.childCount, Is.Zero);
                Assert.That(GameObject.Find("BoardRoot/ActivePieceRoot").transform.childCount, Is.EqualTo(4));
                Assert.That(BestScoreStore.Best, Is.EqualTo(100));
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }

        [UnityTest]
        public IEnumerator GameOverKeyboardReturnsToMenuWithRecordAndPlayStartsAgain()
        {
            PlayerPrefs.SetInt(BestScoreStore.Key, 800); PlayerPrefs.Save();
            var game = Object.FindAnyObjectByType<GameController>();
            yield return FinishScoredGame(game);
            Assert.That(GameObject.Find("GameCanvas/GameOverNotice/PanelBackground/BestScoreValue").GetComponent<Text>().text, Is.EqualTo("800"));
            Press(Key.DownArrow); yield return null; yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(ButtonAt("GameOverNotice/PanelBackground/MainMenuButton").gameObject));
            Press(); yield return null; yield return null;
            Time.timeScale = 0;
            Press(Key.Enter); yield return null; yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(GameObject.Find("Canvas/BestScorePanel/Value").GetComponent<Text>().text, Is.EqualTo("800"));
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"), "Holding Enter must not also activate JUGAR");
            Press(); yield return null; yield return null;
            GameObject.Find("Canvas/MenuButtons/PlayButton").GetComponent<Button>().onClick.Invoke();
            yield return null; yield return null;
            var fresh = Object.FindAnyObjectByType<GameController>();
            Assert.That(fresh.Model.Score, Is.Zero);
            Assert.That(fresh.Model.HasActivePiece, Is.True);
            Assert.That(fresh.AcceptsInput, Is.True);
            Assert.That(BestScoreStore.Best, Is.EqualTo(800));
        }

        [UnityTest]
        public IEnumerator HeldEnterRetriesOnlyOnceAndPanelWorksOnTheNextLoss()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            yield return FinishScoredGame(game);
            Press(Key.Enter); yield return null; yield return null;
            var restarted = game.Model;
            Assert.That(restarted.IsGameOver, Is.False);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(game.Model, Is.SameAs(restarted));
            Assert.That(game.Model.PiecesLocked, Is.Zero);
            Press(); yield return null; yield return null;
            int x = game.Model.Active.Position.x;
            Press(Key.A); yield return null; yield return null;
            Assert.That(game.Model.Active.Position.x, Is.EqualTo(x - 1));
            Press(); yield return null; yield return null;
            for (int i = 0; i < 25 && !game.Model.IsGameOver; i++) { game.HardDrop(); yield return null; }
            yield return null;
            Assert.That(game.Model.IsGameOver, Is.True);
            var retry = ButtonAt("GameOverNotice/PanelBackground/RetryButton");
            Assert.That(retry.interactable, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(retry.gameObject));
            retry.onClick.Invoke(); yield return null; yield return null;
            Assert.That(game.Model.IsGameOver, Is.False);
            Assert.That(game.Model.PiecesLocked, Is.Zero);
        }

        private static IEnumerator FinishScoredGame(GameController game)
        {
            game.Model.Board.TryLock(TetrominoType.I, Enumerable.Range(0, 10).Select(x => new Vector2Int(x, 0)).ToArray());
            game.HardDrop(); yield return null; yield return null;
            Assert.That(game.Model.Score, Is.EqualTo(100));
            for (int i = 0; i < 25 && !game.Model.IsGameOver; i++) { game.HardDrop(); yield return null; }
            yield return null; yield return null;
            Assert.That(game.Model.IsGameOver, Is.True);
        }
        private static IEnumerator Click(Mouse mouse, Button button)
        {
            Canvas.ForceUpdateCanvases();
            var position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            yield return null; yield return null;
        }

        private static Button ButtonAt(string path) => GameObject.Find("GameCanvas").transform.Find(path).GetComponent<Button>();
        private static void Restore(string key, bool had, int value) { if (had) PlayerPrefs.SetInt(key, value); else PlayerPrefs.DeleteKey(key); }
        private sealed class SinglePiece : IPieceSource
        {
            private readonly TetrominoType type;
            public SinglePiece(TetrominoType type) { this.type = type; }
            public TetrominoType Next() => type;
        }
        private static void CheckBounds(Transform root, Bounds area)
        {
            foreach (var r in root.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!r.enabled) continue;
                Assert.That(r.bounds.min.x, Is.GreaterThanOrEqualTo(area.min.x - 0.001f), r.name + " left");
                Assert.That(r.bounds.max.x, Is.LessThanOrEqualTo(area.max.x + 0.001f), r.name + " right");
                Assert.That(r.bounds.min.y, Is.GreaterThanOrEqualTo(area.min.y - 0.001f), r.name + " floor");
                Assert.That(r.bounds.max.y, Is.LessThanOrEqualTo(area.max.y + 0.001f), r.name + " top");
            }
        }
        private void Press(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    }
}
