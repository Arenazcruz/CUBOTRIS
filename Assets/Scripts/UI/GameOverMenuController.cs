using Cubotris.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cubotris.UI
{
    public sealed class GameOverMenuController : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private Text finalScoreText, bestScoreText, survivalTimeText, statusText;
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button saveButton, retryButton, mainMenuButton;
        [SerializeField] private CanvasGroup content;
        [SerializeField] private GameObject unsavedDialog;
        [SerializeField] private Button confirmSaveButton;
        private enum Destination { None, Retry, MainMenu }
        private Destination destination;
        private bool transitioning, saved;
        private int actionFrame;
        private string shownId;
        private ScoreRecord result;

        private void OnEnable()
        {
            if (game == null || game.Model == null) return;
            result = game.FinishedRecord;
            if (result == null) return;
            if (shownId != result.id) { playerNameInput.text = ""; shownId = result.id; }
            saved = BestScoreStore.History.Contains(result.id);
            finalScoreText.text = result.score.ToString();
            bestScoreText.text = BestScoreStore.Best.ToString();
            survivalTimeText.text = GameHudView.FormatTime(result.survivalSeconds);
            statusText.text = saved ? "Récord guardado." : BestScoreStore.History.Notice ?? "Escribe tu nombre para registrar esta partida.";
            transitioning = false; destination = Destination.None; actionFrame = Time.frameCount;
            retryButton.interactable = mainMenuButton.interactable = true;
            saveButton.interactable = playerNameInput.interactable = !saved;
            SetDialog(false); Select(retryButton);
        }

        private bool Ready() => isActiveAndEnabled && !transitioning && result != null
            && game.Model.IsGameOver && Time.frameCount != actionFrame;
        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (unsavedDialog.activeSelf) Cancel();
            else if (playerNameInput.isFocused) { playerNameInput.DeactivateInputField(); Select(retryButton); }
            else if (destination != Destination.None) { destination = Destination.None; statusText.text = "Salida cancelada. Puedes seguir aquí."; }
        }
        public void NameSubmitted(string value)
        { actionFrame = Time.frameCount; playerNameInput.DeactivateInputField(); Select(saveButton); }
        public void Save()
        {
            if (!Ready() || saved || unsavedDialog.activeSelf) return;
            actionFrame = Time.frameCount;
            if (!BestScoreStore.History.TryAdd(result, playerNameInput.text, out string error))
            { statusText.text = error; Select(playerNameInput); playerNameInput.ActivateInputField(); return; }
            saved = true; statusText.text = "Récord guardado correctamente.";
            saveButton.interactable = playerNameInput.interactable = false;
            bestScoreText.text = BestScoreStore.Best.ToString();
            if (destination != Destination.None) Continue(); else Select(retryButton);
        }
        public void Retry() => Request(Destination.Retry);
        public void MainMenu() => Request(Destination.MainMenu);
        private void Request(Destination target)
        {
            if (!Ready() || unsavedDialog.activeSelf) return;
            actionFrame = Time.frameCount; destination = target;
            if (saved) Continue(); else { SetDialog(true); Select(confirmSaveButton); }
        }
        public void SaveBeforeLeaving()
        {
            if (!Ready() || !unsavedDialog.activeSelf) return;
            actionFrame = Time.frameCount; SetDialog(false);
            statusText.text = "Completa tu nombre y pulsa GUARDAR RÉCORD para continuar.";
            Select(playerNameInput); playerNameInput.ActivateInputField();
        }
        public void Discard()
        {
            if (!Ready() || !unsavedDialog.activeSelf) return;
            actionFrame = Time.frameCount; SetDialog(false); Continue();
        }
        public void Cancel()
        {
            if (!Ready() || !unsavedDialog.activeSelf) return;
            actionFrame = Time.frameCount; destination = Destination.None; SetDialog(false); Select(retryButton);
        }
        private void SetDialog(bool open)
        { unsavedDialog.SetActive(open); content.interactable = content.blocksRaycasts = !open; }
        private void Continue()
        {
            transitioning = true; retryButton.interactable = mainMenuButton.interactable = false;
            var target = destination; destination = Destination.None;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Time.timeScale = 1;
            if (target == Destination.Retry) game.Restart();
            else SceneManager.LoadScene("MainMenu");
        }
        private static void Select(Selectable item)
        { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(item.gameObject); }
    }
}
