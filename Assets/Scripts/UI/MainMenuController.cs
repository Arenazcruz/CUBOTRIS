using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cubotris.Gameplay;

namespace Cubotris.UI
{
    /// <summary>Controls navigation between the main menu, options and the game.</summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup notificationPanel;
        [SerializeField] private Text notificationText;
        [SerializeField] private Button firstButton;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject[] mainMenuContent;
        [SerializeField] private Selectable firstOption;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Text bestScoreText;
        [SerializeField, Min(0.1f)] private float messageDuration = 3.5f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.15f;

        private Coroutine notificationRoutine;

        private void OnEnable()
        {
            HideNotification();
            if (bestScoreText != null) bestScoreText.text = BestScoreStore.Best.ToString();
        }

        private void Start()
        {
            if (EventSystem.current != null && firstButton != null)
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }

        public void Play()
        {
            SceneManager.LoadScene("Game");
        }

        public void OpenOptions()
        {
            if (optionsPanel == null || optionsPanel.activeSelf)
                return;

            StopAllCoroutines();
            notificationRoutine = null;
            HideNotification();
            foreach (var section in mainMenuContent)
                section.SetActive(false);

            optionsPanel.SetActive(true);
            if (EventSystem.current != null && firstOption != null)
                EventSystem.current.SetSelectedGameObject(firstOption.gameObject);
        }

        public void CloseOptions()
        {
            if (optionsPanel == null || !optionsPanel.activeSelf)
                return;

            optionsPanel.SetActive(false);
            foreach (var section in mainMenuContent)
                section.SetActive(true);

            if (EventSystem.current != null && optionsButton != null)
                EventSystem.current.SetSelectedGameObject(optionsButton.gameObject);
        }

        public void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ShowNotification(string message)
        {
            if (notificationPanel == null || notificationText == null)
                return;

            if (notificationRoutine != null)
                StopCoroutine(notificationRoutine);

            notificationText.text = message;
            notificationPanel.gameObject.SetActive(true);
            notificationRoutine = StartCoroutine(DisplayNotification());
        }

        private IEnumerator DisplayNotification()
        {
            yield return FadeTo(1f);
            yield return new WaitForSecondsRealtime(messageDuration);
            yield return FadeTo(0f);
            notificationPanel.gameObject.SetActive(false);
            notificationRoutine = null;
        }

        private IEnumerator FadeTo(float target)
        {
            float initial = notificationPanel.alpha;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, fadeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                notificationPanel.alpha = Mathf.Lerp(initial, target, elapsed / duration);
                yield return null;
            }
            notificationPanel.alpha = target;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            notificationRoutine = null;
            HideNotification();
        }

        private void HideNotification()
        {
            if (notificationPanel == null)
                return;

            notificationPanel.alpha = 0f;
            notificationPanel.interactable = false;
            notificationPanel.blocksRaycasts = false;
            notificationPanel.gameObject.SetActive(false);
        }
    }
}
