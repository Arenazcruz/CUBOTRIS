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
            Assert.That(Occupied(game.Session.Faces[0]), Is.EqualTo(4));
            yield return Settled(game);
            Press(); yield return null; yield return null;
            Press(Key.Space); yield return null; yield return null;
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator GameOverShowsNoticeAndPreventsFurtherInput()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            yield return LoseGame(game);
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
            game.HardDrop(); yield return Settled(game);
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
                    game.HardDrop(); yield return Settled(game);
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
            yield return LoseGame(game);
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
            game.HardDrop(); yield return Settled(game);
            Assert.That(game.Model.Score, Is.EqualTo(100));
            yield return LoseGame(game);
            yield return null; yield return null;
            Assert.That(game.Model.IsGameOver, Is.True);
        }
        [UnityTest]
        public IEnumerator PauseKeyboardVisitsEveryButtonAndBothSlidersWithoutMovingTheBoard()
        {
            const string musicKey = "Cubotris.Audio.MusicVolumePercent", effectsKey = "Cubotris.Audio.EffectsVolumePercent";
            bool hadMusic = PlayerPrefs.HasKey(musicKey), hadEffects = PlayerPrefs.HasKey(effectsKey);
            int oldMusic = PlayerPrefs.GetInt(musicKey), oldEffects = PlayerPrefs.GetInt(effectsKey);
            try
            {
                PlayerPrefs.SetInt(musicKey, 50); PlayerPrefs.SetInt(effectsKey, 50);
                var game = Object.FindAnyObjectByType<GameController>();
                yield return Tap(Key.Escape);
                Selected("GameCanvas/PausePanel/PanelBackground/ResumeButton");
                yield return Tap(Key.DownArrow);
                Selected("GameCanvas/PausePanel/PanelBackground/RestartButton");
                var old = game.Model;
                yield return Tap(Key.Enter);
                Assert.That(game.Model, Is.Not.SameAs(old));
                Assert.That(game.IsPaused, Is.False);
                yield return Tap(Key.Escape);
                int revision = game.Model.Revision;
                yield return Tap(Key.DownArrow); yield return Tap(Key.DownArrow);
                Selected("GameCanvas/PausePanel/PanelBackground/OptionsButton");
                yield return Tap(Key.Enter);
                Selected("GameCanvas/PauseOptionsPanel/PanelBackground/MusicVolume/Slider");
                yield return Tap(Key.RightArrow);
                Assert.That(PlayerPrefs.GetInt(musicKey), Is.EqualTo(51));
                yield return Tap(Key.DownArrow);
                Selected("GameCanvas/PauseOptionsPanel/PanelBackground/EffectsVolume/Slider");
                yield return Tap(Key.LeftArrow);
                Assert.That(PlayerPrefs.GetInt(effectsKey), Is.EqualTo(49));
                yield return Tap(Key.DownArrow);
                Selected("GameCanvas/PauseOptionsPanel/PanelBackground/BackButton");
                yield return Tap(Key.Enter);
                Selected("GameCanvas/PausePanel/PanelBackground/OptionsButton");
                yield return Tap(Key.DownArrow);
                Selected("GameCanvas/PausePanel/PanelBackground/MainMenuButton");
                yield return Tap(Key.UpArrow); yield return Tap(Key.UpArrow); yield return Tap(Key.UpArrow);
                Selected("GameCanvas/PausePanel/PanelBackground/ResumeButton");
                Assert.That(game.Model.Revision, Is.EqualTo(revision));
                yield return Tap(Key.Enter);
                Assert.That(game.IsPaused, Is.False);
                Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
                yield return Tap(Key.Escape);
                yield return Tap(Key.UpArrow);
                Selected("GameCanvas/PausePanel/PanelBackground/MainMenuButton");
                yield return Tap(Key.Enter);
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally { Restore(musicKey, hadMusic, oldMusic); Restore(effectsKey, hadEffects, oldEffects); PlayerPrefs.Save(); }
        }

        [UnityTest]
        public IEnumerator ActiveGameDisablesMenuNavigationAndMenusBlockPieceInput()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            Assert.That(EventSystem.current.sendNavigationEvents, Is.False);
            EventSystem.current.SetSelectedGameObject(ButtonAt("PauseButton").gameObject);
            int x = game.Model.Active.Position.x;
            yield return Tap(Key.LeftArrow);
            Assert.That(game.Model.Active.Position.x, Is.EqualTo(x - 1));
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Null);
            yield return Tap(Key.Enter);
            Assert.That(game.IsPaused, Is.False);
            yield return Tap(Key.Escape);
            Assert.That(EventSystem.current.sendNavigationEvents, Is.True);
            int revision = game.Model.Revision;
            yield return Tap(Key.DownArrow);
            Selected("GameCanvas/PausePanel/PanelBackground/RestartButton");
            yield return Tap(Key.UpArrow);
            Selected("GameCanvas/PausePanel/PanelBackground/ResumeButton");
            yield return Tap(Key.LeftArrow); yield return Tap(Key.RightArrow);
            Assert.That(game.Model.Revision, Is.EqualTo(revision));
            yield return Tap(Key.Escape);
            Assert.That(EventSystem.current.sendNavigationEvents, Is.False);
            x = game.Model.Active.Position.x;
            yield return Tap(Key.RightArrow);
            Assert.That(game.Model.Active.Position.x, Is.EqualTo(x + 1));
        }

        [UnityTest]
        public IEnumerator MainMenuKeyboardNavigationAndOptionsRemainFunctional()
        {
            const string musicKey = "Cubotris.Audio.MusicVolumePercent", effectsKey = "Cubotris.Audio.EffectsVolumePercent";
            bool hadMusic = PlayerPrefs.HasKey(musicKey), hadEffects = PlayerPrefs.HasKey(effectsKey);
            int oldMusic = PlayerPrefs.GetInt(musicKey), oldEffects = PlayerPrefs.GetInt(effectsKey);
            try
            {
                PlayerPrefs.SetInt(musicKey, 50); PlayerPrefs.SetInt(effectsKey, 50);
                yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
                Assert.That(EventSystem.current.sendNavigationEvents, Is.True);
                Selected("Canvas/MenuButtons/PlayButton");
                yield return Tap(Key.UpArrow);
                Selected("Canvas/MenuButtons/ExitButton");
                yield return Tap(Key.UpArrow);
                Selected("Canvas/MenuButtons/OptionsButton");
                yield return Tap(Key.Enter);
                Selected("Canvas/OptionsPanel/PanelBackground/MusicVolume/Slider");
                yield return Tap(Key.LeftArrow);
                Assert.That(PlayerPrefs.GetInt(musicKey), Is.EqualTo(49));
                yield return Tap(Key.DownArrow);
                Selected("Canvas/OptionsPanel/PanelBackground/EffectsVolume/Slider");
                yield return Tap(Key.RightArrow);
                Assert.That(PlayerPrefs.GetInt(effectsKey), Is.EqualTo(51));
                yield return Tap(Key.DownArrow);
                Selected("Canvas/OptionsPanel/PanelBackground/BackButton");
                yield return Tap(Key.Enter);
                Selected("Canvas/MenuButtons/OptionsButton");
                yield return Tap(Key.UpArrow);
                yield return Tap(Key.Enter);
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Game"));
                Assert.That(EventSystem.current.sendNavigationEvents, Is.False);
            }
            finally { Restore(musicKey, hadMusic, oldMusic); Restore(effectsKey, hadEffects, oldEffects); PlayerPrefs.Save(); }
        }

        private IEnumerator Tap(Key key)
        {
            Press(key); yield return null; yield return null;
            Press(); yield return null; yield return null;
        }
        private static void Selected(string path)
        { Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(GameObject.Find(path)), "Selection at " + path); }
        [UnityTest]
        public IEnumerator MouseNavigatesMainMenuPauseOptionsAndGameOver()
        {
            const string musicKey = "Cubotris.Audio.MusicVolumePercent", effectsKey = "Cubotris.Audio.EffectsVolumePercent";
            bool hadMusic = PlayerPrefs.HasKey(musicKey), hadEffects = PlayerPrefs.HasKey(effectsKey);
            int oldMusic = PlayerPrefs.GetInt(musicKey), oldEffects = PlayerPrefs.GetInt(effectsKey);
            var mouse = InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
            try
            {
                yield return SceneManager.LoadSceneAsync("MainMenu"); yield return null;
                yield return Click(mouse, GameObject.Find("Canvas/MenuButtons/OptionsButton").GetComponent<Button>());
                var music = GameObject.Find("Canvas/OptionsPanel/PanelBackground/MusicVolume/Slider").GetComponent<Slider>();
                yield return ClickSlider(mouse, music, 0.75f);
                Assert.That(music.value, Is.InRange(70, 80));
                yield return Click(mouse, GameObject.Find("Canvas/OptionsPanel/PanelBackground/BackButton").GetComponent<Button>());
                yield return Click(mouse, GameObject.Find("Canvas/MenuButtons/PlayButton").GetComponent<Button>());
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Game"));
                yield return Click(mouse, ButtonAt("PauseButton"));
                yield return Click(mouse, ButtonAt("PausePanel/PanelBackground/OptionsButton"));
                var effects = GameObject.Find("GameCanvas/PauseOptionsPanel/PanelBackground/EffectsVolume/Slider").GetComponent<Slider>();
                yield return ClickSlider(mouse, effects, 0.3f);
                Assert.That(effects.value, Is.InRange(25, 35));
                yield return Click(mouse, ButtonAt("PauseOptionsPanel/PanelBackground/BackButton"));
                yield return Click(mouse, ButtonAt("PausePanel/PanelBackground/ResumeButton"));
                var game = Object.FindAnyObjectByType<GameController>();
                Assert.That(game.IsPaused, Is.False);
                yield return FinishScoredGame(game);
                yield return Click(mouse, ButtonAt("GameOverNotice/PanelBackground/MainMenuButton"));
                Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("MainMenu"));
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
                Restore(musicKey, hadMusic, oldMusic); Restore(effectsKey, hadEffects, oldEffects); PlayerPrefs.Save();
            }
        }

        [UnityTest]
        public IEnumerator FourFaceCyclePreservesBlocksAndShowsOnePieceWithDistinctColors()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            var transition = Object.FindAnyObjectByType<FaceTransitionView>();
            var board = Object.FindAnyObjectByType<BoardView>();
            var active = board.transform.Find("ActivePieceRoot");
            var placed = board.transform.Find("PlacedBlocksRoot");
            var origin = board.transform.localPosition;
            var colors = new Color[4];
            var label = GameObject.Find("GameCanvas/GameInfoPanel/ActiveFace").GetComponent<Text>();
            Assert.That(game.Session.ActiveFace, Is.Zero);
            for (int face = 0; face < 4; face++)
            {
                colors[face] = label.color;
                game.HardDrop();
                Assert.That(game.IsTransitioning, Is.True);
                Assert.That(game.Session.ActiveFace, Is.EqualTo((face + 1) % 4));
                Assert.That(Occupied(game.Session.Faces[face]), Is.EqualTo(4));
                Assert.That(active.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled), Is.Zero);
                yield return new WaitForSeconds(0.08f);
                Assert.That(board.transform.localPosition, Is.Not.EqualTo(origin), "The board must visibly slide");
                Assert.That(board.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled && r.color.a > 0 && r.color.a < 1), Is.True, "The board must visibly fade");
                float deadline = Time.realtimeSinceStartup + 2;
                while (game.IsTransitioning && Time.realtimeSinceStartup < deadline)
                {
                    Assert.That(active.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled), Is.LessThanOrEqualTo(4));
                    Assert.That(Object.FindObjectsByType<BoardView>().Length, Is.EqualTo(1));
                    yield return null;
                }
                yield return Settled(game);
                Assert.That(transition.DisplayedFace, Is.EqualTo(game.Session.ActiveFace));
                Assert.That(label.text, Is.EqualTo($"CARA {game.Session.ActiveFace + 1} / 4"));
                Assert.That(board.transform.localPosition, Is.EqualTo(origin));
                Assert.That(active.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled), Is.EqualTo(4));
                Assert.That(placed.GetComponentsInChildren<SpriteRenderer>().Count(r => r.enabled), Is.EqualTo(face == 3 ? 4 : 0));
            }
            Assert.That(colors.Distinct().Count(), Is.EqualTo(4));
            Assert.That(game.Session.Faces.All(b => Occupied(b) == 4), Is.True);
        }

        [UnityTest]
        public IEnumerator PauseFreezesTransitionOptionsResumeItAndRestartClearsAllFaces()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            var transition = Object.FindAnyObjectByType<FaceTransitionView>();
            var board = Object.FindAnyObjectByType<BoardView>();
            var origin = board.transform.localPosition;
            PlayerPrefs.SetInt(BestScoreStore.Key, 900); PlayerPrefs.Save();
            for (int i = 0; i < 3; i++) { game.HardDrop(); yield return Settled(game); }
            game.HardDrop(); yield return new WaitForSeconds(0.08f);
            yield return Tap(Key.Escape);
            Assert.That(game.IsPaused, Is.True);
            float progress = transition.Progress;
            var position = board.transform.localPosition;
            int revision = game.Model.Revision;
            Assert.That(progress, Is.InRange(0.01f, 0.99f));
            yield return new WaitForSecondsRealtime(0.65f);
            ButtonAt("PausePanel/PanelBackground/OptionsButton").onClick.Invoke(); yield return null;
            Assert.That(GameObject.Find("GameCanvas/PauseOptionsPanel"), Is.Not.Null);
            yield return Tap(Key.Escape);
            Assert.That(transition.Progress, Is.EqualTo(progress));
            Assert.That(board.transform.localPosition, Is.EqualTo(position));
            Assert.That(game.Model.Revision, Is.EqualTo(revision));
            Assert.That(Time.timeScale, Is.Zero);
            ButtonAt("PausePanel/PanelBackground/ResumeButton").onClick.Invoke();
            yield return Settled(game);
            Assert.That(game.Session.ActiveFace, Is.Zero);
            Assert.That(game.Session.Faces.All(b => Occupied(b) == 4), Is.True);
            Assert.That(board.transform.localPosition, Is.EqualTo(origin));
            game.HardDrop(); yield return new WaitForSeconds(0.08f);
            yield return Tap(Key.Escape);
            ButtonAt("PausePanel/PanelBackground/RestartButton").onClick.Invoke(); yield return null; yield return null;
            Assert.That(game.IsTransitioning, Is.False);
            Assert.That(game.IsPaused, Is.False);
            Assert.That(game.Session.ActiveFace, Is.Zero);
            Assert.That(game.Session.Faces.Sum(Occupied), Is.Zero);
            Assert.That(game.Model.Score, Is.Zero);
            Assert.That(game.Model.LinesCleared, Is.Zero);
            Assert.That(game.Model.PiecesLocked, Is.Zero);
            Assert.That(game.Model.HasActivePiece, Is.True);
            Assert.That(board.transform.localPosition, Is.EqualTo(origin));
            Assert.That(board.GetComponentsInChildren<SpriteRenderer>().Where(r => !r.transform.IsChildOf(board.transform.Find("GhostPieceRoot"))).All(r => r.color.a == 1), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(BestScoreStore.Best, Is.EqualTo(900));
        }

        [UnityTest]
        public IEnumerator TransitionBlocksGravityAndHeldInputUntilKeysAreReleased()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            Press(Key.Space); yield return null; yield return null;
            Assert.That(game.IsTransitioning, Is.True);
            int revision = game.Model.Revision;
            var spawn = game.Model.Active.Position;
            Press(Key.Space, Key.LeftArrow, Key.UpArrow, Key.DownArrow);
            game.Move(-1); game.Rotate(); game.HardDrop();
            yield return new WaitForSeconds(0.2f);
            Assert.That(game.IsTransitioning, Is.True);
            Assert.That(game.Model.Revision, Is.EqualTo(revision));
            Assert.That(game.AcceptsInput, Is.False);
            yield return Settled(game);
            yield return new WaitForSeconds(0.2f);
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(1));
            Assert.That(game.Model.Active.Position, Is.EqualTo(spawn));
            Assert.That(game.Model.Revision, Is.EqualTo(revision));
            Press(); yield return null; yield return null;
            Press(Key.Space); yield return null; yield return null;
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(2));
            Assert.That(game.Session.ActiveFace, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator GravityAndSoftDropLocksEachStartExactlyOneTransition()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            while (game.Model.Board.CanPlace(game.Model.Active.Cells.Select(c => c + Vector2Int.down).ToArray())) game.Model.StepDown();
            float deadline = Time.realtimeSinceStartup + 2;
            while (!game.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game.IsTransitioning, Is.True, "Natural gravity must start the same transition as hard drop");
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(1));
            Assert.That(game.Session.ActiveFace, Is.EqualTo(1));
            yield return Settled(game);
            while (game.Model.Board.CanPlace(game.Model.Active.Cells.Select(c => c + Vector2Int.down).ToArray())) game.Model.StepDown();
            Press(Key.DownArrow); yield return null; yield return null;
            Assert.That(game.IsTransitioning, Is.True, "A soft drop that reaches the floor must advance the face");
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(2));
            Assert.That(game.Session.ActiveFace, Is.EqualTo(2));
            yield return Settled(game);
            Assert.That(game.Model.PiecesLocked, Is.EqualTo(2));
            Assert.That(game.Model.Active.Position.y, Is.EqualTo(18));
        }

        [UnityTest]
        public IEnumerator NextPieceSpritesAndShapePredictTheNextSpawnAcrossAllFaces()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            var preview = GameObject.Find("GameCanvas/NextPiecePanel/PiecePreview").GetComponentsInChildren<Image>();
            Assert.That(preview.Length, Is.EqualTo(4));
            for (int face = 0; face < 8; face++)
            {
                var expected = game.Model.NextPiece;
                var cells = TetrominoData.Cells(expected, 0, Vector2Int.zero);
                for (int i = 0; i < 4; i++)
                {
                    Assert.That(preview[i].enabled, Is.True);
                    Assert.That(preview[i].sprite, Is.Not.Null);
                    Assert.That(preview[i].rectTransform.anchoredPosition - preview[0].rectTransform.anchoredPosition,
                        Is.EqualTo((Vector2)(cells[i] - cells[0]) * 42));
                }
                var sprite = preview[0].sprite;
                game.HardDrop(); yield return Settled(game);
                Assert.That(game.Model.Active.Type, Is.EqualTo(expected));
                Assert.That(GameObject.Find("BoardRoot/ActivePieceRoot").GetComponentInChildren<SpriteRenderer>().sprite, Is.EqualTo(sprite));
            }
        }

        [UnityTest]
        public IEnumerator MiniBoardsDrawExactLockedCellsAndOnlyRebuildWhenTheirBoardChanges()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            var overview = GameObject.Find("GameCanvas/FacesOverview").transform;
            var minis = Enumerable.Range(1, 4).Select(i => overview.Find($"Face{i}/PlacedCells").GetComponent<MiniBoardView>()).ToArray();
            for (int face = 0; face < 4; face++)
            {
                for (int i = 0; i < face; i++) game.Move(-1);
                game.HardDrop(); yield return Settled(game);
                for (int i = 0; i < 4; i++) AssertMini(minis[i], game.Session.Faces[i]);
                Assert.That(overview.Find($"Face{game.Session.ActiveFace + 1}/Marker").GetComponent<Text>().text, Is.EqualTo("ACTUAL"));
                int next = (game.Session.ActiveFace + 1) % 4;
                Assert.That(overview.Find($"Face{next + 1}/Marker").GetComponent<Text>().text, Is.EqualTo("SIGUIENTE"));
                Assert.That(GameObject.Find("GameCanvas/GameInfoPanel/NextFace").GetComponent<Text>().text, Is.EqualTo($"SIGUIENTE: {next + 1} / 4"));
            }
            var board = game.Session.Faces[2];
            board.TryLock(TetrominoType.Z, Enumerable.Range(0, 10).Select(x => new Vector2Int(x, 4)).ToArray());
            yield return null; yield return null; AssertMini(minis[2], board);
            Assert.That(board.ClearFullRows(), Is.EqualTo(1));
            yield return null; yield return null; AssertMini(minis[2], board);
            int rebuilds = 0;
            UnityEngine.Events.UnityAction dirty = () => rebuilds++;
            foreach (var mini in minis) mini.RegisterDirtyVerticesCallback(dirty);
            int objects = overview.GetComponentsInChildren<Transform>(true).Length;
            game.Move(1); game.Rotate();
            for (int i = 0; i < 15; i++) yield return null;
            Assert.That(rebuilds, Is.Zero, "Unchanged mini boards must not rebuild for movement or gravity");
            Assert.That(overview.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objects));
            foreach (var mini in minis) mini.UnregisterDirtyVerticesCallback(dirty);
            Assert.That(overview.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(overview.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), Is.True);
        }

        [UnityTest]
        public IEnumerator GhostFollowsMovementRotationAndDropButIsHiddenThroughoutTransition()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            var ghosts = GameObject.Find("BoardRoot/GhostPieceRoot").GetComponentsInChildren<SpriteRenderer>();
            Assert.That(ghosts.Length, Is.EqualTo(4));
            AssertGhost(game, ghosts);
            game.Move(-1); yield return null; yield return null; AssertGhost(game, ghosts);
            game.Rotate(); yield return null; yield return null; AssertGhost(game, ghosts);
            yield return Tap(Key.DownArrow); AssertGhost(game, ghosts);
            yield return Tap(Key.Escape);
            var frozen = ghosts.Select(r => r.transform.localPosition).ToArray();
            yield return new WaitForSecondsRealtime(0.2f);
            CollectionAssert.AreEqual(frozen, ghosts.Select(r => r.transform.localPosition).ToArray());
            yield return Tap(Key.Escape);
            game.HardDrop();
            Assert.That(ghosts.All(r => !r.enabled), Is.True, "Lock must hide the projection immediately");
            float deadline = Time.realtimeSinceStartup + 2;
            while (game.IsTransitioning && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(ghosts.All(r => !r.enabled), Is.True, "Incoming board must not show a ghost mid-animation");
                yield return null;
            }
            yield return Settled(game); AssertGhost(game, ghosts);
        }

        [UnityTest]
        public IEnumerator PauseRestartAndGameOverKeepHudCoherentAndNonInteractive()
        {
            var game = Object.FindAnyObjectByType<GameController>();
            var overview = GameObject.Find("GameCanvas/FacesOverview").transform;
            var minis = Enumerable.Range(1, 4).Select(i => overview.Find($"Face{i}/PlacedCells").GetComponent<MiniBoardView>()).ToArray();
            var preview = GameObject.Find("GameCanvas/NextPiecePanel").transform;
            Assert.That(preview.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(preview.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), Is.True);
            for (int i = 0; i < 4; i++) { game.HardDrop(); yield return Settled(game); }
            yield return Tap(Key.Escape);
            int revision = game.Model.Revision;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(game.Model.Revision, Is.EqualTo(revision));
            for (int i = 0; i < 4; i++) AssertMini(minis[i], game.Session.Faces[i]);
            ButtonAt("PausePanel/PanelBackground/RestartButton").onClick.Invoke(); yield return null; yield return null;
            for (int i = 0; i < 4; i++) { Assert.That(Occupied(game.Session.Faces[i]), Is.Zero); AssertMini(minis[i], game.Session.Faces[i]); }
            Assert.That(overview.Find("Face1/Marker").GetComponent<Text>().text, Is.EqualTo("ACTUAL"));
            Assert.That(preview.Find("PiecePreview").GetComponentsInChildren<Image>().All(i => i.enabled), Is.True);
            yield return LoseGame(game);
            for (int i = 0; i < 4; i++) AssertMini(minis[i], game.Session.Faces[i]);
            Assert.That(preview.Find("PiecePreview").GetComponentsInChildren<Image>().All(i => !i.enabled), Is.True);
            Assert.That(GameObject.Find("BoardRoot/GhostPieceRoot").GetComponentsInChildren<SpriteRenderer>().All(r => !r.enabled), Is.True);
            ButtonAt("GameOverNotice/PanelBackground/RetryButton").onClick.Invoke(); yield return null; yield return null;
            for (int i = 0; i < 4; i++) AssertMini(minis[i], game.Session.Faces[i]);
            AssertGhost(game, GameObject.Find("BoardRoot/GhostPieceRoot").GetComponentsInChildren<SpriteRenderer>());
            Assert.That(preview.Find("PiecePreview").GetComponentsInChildren<Image>().All(i => i.enabled), Is.True);
        }

        private static void AssertMini(MiniBoardView mini, BoardState board)
        {
            Canvas.ForceUpdateCanvases();
            var mesh = mini.canvasRenderer.GetMesh();
            if (mesh == null) { Assert.That(Occupied(board), Is.Zero); return; }
            Assert.That(mesh.vertexCount, Is.EqualTo(Occupied(board) * 4));
            var vertices = mesh.vertices; var rect = mini.rectTransform.rect;
            for (int i = 0; i < vertices.Length; i += 4)
            {
                var center = (vertices[i] + vertices[i + 1] + vertices[i + 2] + vertices[i + 3]) / 4;
                int x = Mathf.FloorToInt((center.x - rect.xMin) * 10 / rect.width);
                int y = Mathf.FloorToInt((center.y - rect.yMin) * 20 / rect.height);
                Assert.That(board[x, y].HasValue, Is.True, $"Mini view has an unexpected cell at {x}, {y}");
            }
        }

        private static void AssertGhost(GameController game, SpriteRenderer[] ghosts)
        {
            var landing = game.Model.GetLandingPiece().Value;
            if (landing.Position == game.Model.Active.Position) { Assert.That(ghosts.All(r => !r.enabled), Is.True); return; }
            Assert.That(ghosts.All(r => r.enabled && r.color.a >= 0.1f && r.color.a <= 0.5f), Is.True);
            var rendered = ghosts.Select(r => new Vector2Int(Mathf.FloorToInt(r.transform.localPosition.x), Mathf.FloorToInt(r.transform.localPosition.y))).ToArray();
            CollectionAssert.AreEquivalent(landing.Cells, rendered);
            Assert.That(game.Model.Board.CanPlace(rendered), Is.True);
        }

        private static IEnumerator Settled(GameController game)
        {
            float deadline = Time.realtimeSinceStartup + 2;
            while (game.IsTransitioning && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(game.IsTransitioning, Is.False, "Transition must complete within two seconds");
            yield return null; yield return null;
        }
        private static IEnumerator LoseGame(GameController game)
        {
            yield return Settled(game);
            var target = game.Session.Faces[(game.Session.ActiveFace + 1) % 4];
            var spawnZone = Enumerable.Range(3, 4).SelectMany(x => new[] { new Vector2Int(x, 18), new Vector2Int(x, 19) }).Where(c => !target[c.x, c.y].HasValue).ToArray();
            target.TryLock(TetrominoType.Z, spawnZone);
            game.HardDrop(); yield return null; yield return null;
            Assert.That(game.Model.IsGameOver, Is.True);
        }
        private static int Occupied(BoardState board) => Enumerable.Range(0, 200).Count(i => board[i % 10, i / 10].HasValue);
        private static IEnumerator ClickSlider(Mouse mouse, Slider slider, float fraction)
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4]; ((RectTransform)slider.transform).GetWorldCorners(corners);
            var position = RectTransformUtility.WorldToScreenPoint(null, new Vector3(Mathf.Lerp(corners[0].x, corners[2].x, fraction), (corners[0].y + corners[2].y) / 2, corners[0].z));
            yield return ClickAt(mouse, position);
        }
        private static IEnumerator Click(Mouse mouse, Button button)
        {
            Canvas.ForceUpdateCanvases();
            var position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            yield return ClickAt(mouse, position);
        }

        private static IEnumerator ClickAt(Mouse mouse, Vector2 position)
        {
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
            public TetrominoType Peek() => type;
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
