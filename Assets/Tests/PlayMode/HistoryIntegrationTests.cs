using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Cubotris.Gameplay;
using NUnit.Framework;
using TMPro;
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
        private static TMP_InputField NameField => GameObject.Find("GameCanvas/GameOverNotice/PanelBackground/PlayerNameInput").GetComponent<TMP_InputField>();
        private static Button HistoryButton(string path) => GameObject.Find("Canvas").transform.Find("RecordsPanel/"+path).GetComponent<Button>();
        private static Text HistoryText(string name) => GameObject.Find("Canvas/RecordsPanel/Content/"+name).GetComponent<Text>();
        private static IEnumerator FinishUnsaved(GameController game)
        {
            game.Session.AdvanceActiveTime(135);
            game.Model.Board.TryLock(TetrominoType.I,Enumerable.Range(0,10).Select(x=>new Vector2Int(x,0)).ToArray());
            game.HardDrop();yield return Settled(game);yield return LoseGame(game);yield return null;yield return null;
        }
        [UnityTest] public IEnumerator UnsavedExitCanCancelOrSaveWithKeyboardBeforeContinuingToMainMenu()
        {
            var game=Object.FindAnyObjectByType<GameController>();yield return FinishUnsaved(game);
            var result=game.FinishedRecord;int revision=game.Model.Revision;
            ButtonAt("GameOverNotice/PanelBackground/RetryButton").onClick.Invoke();yield return null;yield return null;
            Assert.That(GameObject.Find("GameCanvas/GameOverNotice/UnsavedDialog"),Is.Not.Null);
            Assert.That(GameObject.Find("GameCanvas/GameOverNotice/PanelBackground").GetComponent<CanvasGroup>().interactable,Is.False);
            yield return Tap(Key.Escape);Assert.That(game.Model.IsGameOver,Is.True);Assert.That(BestScoreStore.History.Records,Is.Empty);
            ButtonAt("GameOverNotice/PanelBackground/MainMenuButton").onClick.Invoke();yield return null;yield return null;
            yield return Tap(Key.Enter);yield return null;
            Assert.That(NameField.isFocused,Is.True);
            // TMP consumes native Unity text events, not virtual Input System TextEvents.
            foreach(char c in "Ana 7") NameField.ProcessEvent(new Event { type = EventType.KeyDown, character = c });
            yield return null;yield return null;
            Assert.That(NameField.text,Is.EqualTo("Ana 7"));
            Assert.That(game.Model.Revision,Is.EqualTo(revision));Assert.That(game.Session.SurvivalSeconds,Is.EqualTo(result.survivalSeconds));
            yield return Tap(Key.Enter); // Finish editing and focus GUARDAR.
            Assert.That(EventSystem.current.currentSelectedGameObject.name,Is.EqualTo("SaveButton"));
            yield return Tap(Key.Enter);yield return null;
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("MainMenu"));
            var saved=BestScoreStore.History.Records.Single();Assert.That(saved.id,Is.EqualTo(result.id));Assert.That(saved.score,Is.EqualTo(100));
            Assert.That(saved.survivalSeconds,Is.EqualTo(result.survivalSeconds));Assert.That(saved.playerName,Is.EqualTo("Ana 7"));
            Assert.That(Time.timeScale,Is.EqualTo(1));Assert.That(GameObject.Find("Canvas/BestScorePanel/PlayerName").GetComponent<Text>().text,Is.EqualTo("Ana 7"));
        }
        [UnityTest] public IEnumerator UnsavedDiscardRestartsWithoutARecordAndDoubleClickCannotRestartAgain()
        {
            var game=Object.FindAnyObjectByType<GameController>();yield return LoseGame(game);yield return null;
            ButtonAt("GameOverNotice/PanelBackground/RetryButton").onClick.Invoke();yield return null;yield return null;
            var discard=ButtonAt("GameOverNotice/UnsavedDialog/PanelBackground/DiscardButton");discard.onClick.Invoke();var fresh=game.Model;discard.onClick.Invoke();
            yield return null;yield return null;
            Assert.That(game.Model,Is.SameAs(fresh));Assert.That(game.Model.IsGameOver,Is.False);Assert.That(BestScoreStore.History.Records,Is.Empty);
            Assert.That(game.AcceptsInput,Is.True);Assert.That(game.Session.SurvivalSeconds,Is.LessThan(1));
        }
        [UnityTest] public IEnumerator NameValidationDuplicateSavingAndWriteFailureKeepGameOverUsable()
        {
            var game=Object.FindAnyObjectByType<GameController>();yield return FinishUnsaved(game);
            var save=ButtonAt("GameOverNotice/PanelBackground/SaveButton");
            save.onClick.Invoke();yield return null;yield return null;
            Assert.That(BestScoreStore.History.Records,Is.Empty);
            Assert.That(GameObject.Find("GameCanvas/GameOverNotice/PanelBackground/SaveStatus").GetComponent<Text>().text,Does.Contain("nombre"));
            NameField.text="Ana!";save.onClick.Invoke();yield return null;yield return null;Assert.That(BestScoreStore.History.Records,Is.Empty);
            NameField.text="  Ana 7  ";System.IO.Directory.CreateDirectory(BestScoreStore.History.FilePath+".tmp");
            LogAssert.Expect(LogType.Error,new Regex("No se pudo guardar el historial:"));save.onClick.Invoke();yield return null;yield return null;
            Assert.That(save.interactable,Is.True);Assert.That(game.Model.IsGameOver,Is.True);Assert.That(BestScoreStore.History.Records,Is.Empty);
            Assert.That(GameObject.Find("GameCanvas/GameOverNotice/PanelBackground/SaveStatus").GetComponent<Text>().text,Does.Contain("No se pudo guardar"));
            System.IO.Directory.Delete(BestScoreStore.History.FilePath+".tmp");
            save.onClick.Invoke();save.onClick.Invoke();yield return null;yield return null;save.onClick.Invoke();
            Assert.That(BestScoreStore.History.Records.Count,Is.EqualTo(1));Assert.That(BestScoreStore.PlayerName,Is.EqualTo("Ana 7"));Assert.That(save.interactable,Is.False);
            ButtonAt("GameOverNotice/PanelBackground/RetryButton").onClick.Invoke();yield return null;yield return null;
            Assert.That(game.Model.IsGameOver,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator HistoryPagesKeyboardSelectionDeletionCancelAndClearUpdateTheMainMenu()
        {
            for(int i=0;i<19;i++)SeedRecord(i*100);
            yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
            GameObject.Find("Canvas/MenuButtons/RecordsButton").GetComponent<Button>().onClick.Invoke();yield return null;yield return null;
            Assert.That(HistoryText("Page").text,Does.Contain("1 / 3"));
            Assert.That(GameObject.Find("Canvas/RecordsPanel/Content/Row1/Score").GetComponent<Text>().text,Is.EqualTo("1800"));
            yield return Tap(Key.DownArrow);Assert.That(EventSystem.current.currentSelectedGameObject.name,Is.EqualTo("Row2"));
            Assert.That(HistoryText("Detail").text,Does.Contain("1700 puntos"));
            HistoryButton("Content/DeleteButton").onClick.Invoke();yield return null;yield return null;
            Assert.That(GameObject.Find("Canvas/RecordsPanel/Content").GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
            yield return Tap(Key.Enter);Assert.That(BestScoreStore.History.Records.Count,Is.EqualTo(19)); // Default CANCELAR.
            HistoryButton("Content/DeleteButton").onClick.Invoke();yield return null;yield return null;
            yield return Tap(Key.DownArrow);yield return Tap(Key.Enter);
            Assert.That(BestScoreStore.History.Records.Count,Is.EqualTo(18));Assert.That(BestScoreStore.History.Records.Any(r=>r.score==1700),Is.False);
            HistoryButton("Content/NextButton").onClick.Invoke();yield return null;yield return null;
            Assert.That(HistoryText("Page").text,Does.Contain("2 / 3"));
            HistoryButton("Content/NextButton").onClick.Invoke();yield return null;yield return null;
            Assert.That(HistoryText("Page").text,Does.Contain("3 / 3"));
            Assert.That(GameObject.Find("Canvas").transform.Find("RecordsPanel/Content/Row3").gameObject.activeSelf,Is.False);
            HistoryButton("Content/PreviousButton").onClick.Invoke();yield return null;yield return null;
            HistoryButton("Content/PreviousButton").onClick.Invoke();yield return null;yield return null;
            HistoryButton("Content/DeleteButton").onClick.Invoke();yield return null;yield return null;
            HistoryButton("DeleteDialog/PanelBackground/ConfirmButton").onClick.Invoke();yield return null;yield return null;
            Assert.That(BestScoreStore.Best,Is.EqualTo(1600));
            HistoryButton("Content/ClearButton").onClick.Invoke();yield return null;yield return null;
            yield return Tap(Key.Escape);Assert.That(BestScoreStore.History.Records.Count,Is.EqualTo(17));
            HistoryButton("Content/ClearButton").onClick.Invoke();yield return null;yield return null;
            HistoryButton("DeleteDialog/PanelBackground/ConfirmButton").onClick.Invoke();yield return null;yield return null;
            Assert.That(BestScoreStore.History.Records,Is.Empty);Assert.That(HistoryText("EmptyMessage").text,Is.EqualTo("Todavía no hay récords registrados"));
            yield return Tap(Key.Escape);Assert.That(GameObject.Find("Canvas/BestScorePanel/Value").GetComponent<Text>().text,Is.EqualTo("0"));
            Assert.That(GameObject.Find("Canvas/BestScorePanel/PlayerName").GetComponent<Text>().text,Is.EqualTo("Sin récords"));
        }
        [UnityTest] public IEnumerator PointerCanOpenHistoryAndCancelItsDeleteDialog()
        {
            SeedRecord(500);var mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();
            try
            {
                yield return SceneManager.LoadSceneAsync("MainMenu");yield return null;
                yield return Click(mouse,GameObject.Find("Canvas/MenuButtons/RecordsButton").GetComponent<Button>());
                yield return Click(mouse,HistoryButton("Content/Row1"));
                yield return Click(mouse,HistoryButton("Content/DeleteButton"));
                yield return Click(mouse,HistoryButton("DeleteDialog/PanelBackground/CancelButton"));
                Assert.That(BestScoreStore.History.Records.Count,Is.EqualTo(1));
                yield return Click(mouse,HistoryButton("Content/BackButton"));Assert.That(GameObject.Find("Canvas/MenuButtons/RecordsButton"),Is.Not.Null);
            }
            finally{InputSystem.RemoveDevice(mouse);}
        }
    }
}
