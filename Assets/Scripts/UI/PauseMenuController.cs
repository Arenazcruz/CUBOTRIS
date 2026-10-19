using Cubotris.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cubotris.UI
{
    [DefaultExecutionOrder(-200)]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Selectable firstOption;
        private int transitionFrame = -1;
        private bool leaving;

        private void Update()
        {
            pauseButton.interactable = game.Model != null && !game.Model.IsGameOver && !leaving;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) TogglePause();
            if (EventSystem.current != null)
            {
                // Pointer clicks can still open PAUSA; keyboard UI input belongs only to an open menu.
                bool menuOpen = game.Model != null && (game.IsPaused || game.Model.IsGameOver);
                EventSystem.current.sendNavigationEvents = menuOpen;
                if (!menuOpen && EventSystem.current.currentSelectedGameObject != null)
                    EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private bool CanTransition()
        {
            if (leaving || game.Model == null || game.Model.IsGameOver || transitionFrame == Time.frameCount) return false;
            transitionFrame = Time.frameCount;
            return true;
        }

        public void TogglePause()
        {
            if (optionsPanel.activeSelf) { CloseOptions(); return; }
            if (game.IsPaused) { Resume(); return; }
            if (!CanTransition()) return;
            game.SetPaused(true);
            pausePanel.SetActive(true);
            Select(resumeButton);
        }

        public void Resume()
        {
            if (!game.IsPaused || optionsPanel.activeSelf || !CanTransition()) return;
            pausePanel.SetActive(false);
            game.SetPaused(false);
            Select(null);
        }

        public void Restart()
        {
            if (!game.IsPaused || optionsPanel.activeSelf || !CanTransition()) return;
            pausePanel.SetActive(false);
            game.Restart();
            Select(null);
        }

        public void OpenOptions()
        {
            if (!game.IsPaused || !pausePanel.activeSelf || !CanTransition()) return;
            pausePanel.SetActive(false);
            optionsPanel.SetActive(true);
            Select(firstOption);
        }

        public void CloseOptions()
        {
            if (!optionsPanel.activeSelf || !CanTransition()) return;
            optionsPanel.SetActive(false);
            pausePanel.SetActive(true);
            Select(optionsButton);
        }

        public void MainMenu()
        {
            if (!game.IsPaused || optionsPanel.activeSelf || !CanTransition()) return;
            leaving = true;
            game.SaveRecord();
            Time.timeScale = 1;
            SceneManager.LoadScene("MainMenu");
        }

        private static void Select(Selectable target)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target == null ? null : target.gameObject);
        }
    }
}
