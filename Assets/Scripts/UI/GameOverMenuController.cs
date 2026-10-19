using Cubotris.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cubotris.UI
{
    public sealed class GameOverMenuController : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text bestScoreText;
        [SerializeField] private Text survivalTimeText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;
        private bool transitioning;
        private int shownFrame;

        private void OnEnable()
        {
            if (game == null || game.Model == null) return;
            game.SaveRecord();
            finalScoreText.text = game.Model.Score.ToString();
            bestScoreText.text = BestScoreStore.Best.ToString();
            survivalTimeText.text = GameHudView.FormatTime(game.Session.SurvivalSeconds);
            transitioning = false;
            shownFrame = Time.frameCount;
            retryButton.interactable = true;
            mainMenuButton.interactable = true;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
        }

        private bool BeginTransition()
        {
            // Ignore the input that opened the panel and consume each choice only once.
            if (!isActiveAndEnabled || transitioning || Time.frameCount == shownFrame
                || game.Model == null || !game.Model.IsGameOver) return false;
            transitioning = true;
            retryButton.interactable = false;
            mainMenuButton.interactable = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            return true;
        }

        public void Retry()
        {
            if (!BeginTransition()) return;
            game.Restart();
        }

        public void MainMenu()
        {
            if (!BeginTransition()) return;
            game.SaveRecord();
            Time.timeScale = 1;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
