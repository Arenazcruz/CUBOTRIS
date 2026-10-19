using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cubotris.UI
{
    /// <summary>Controls the saved menu UI; future screens are deliberately not loaded yet.</summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup notificationPanel;
        [SerializeField] private Text notificationText;
        [SerializeField] private Button firstButton;
        [SerializeField, Min(0.1f)] private float messageDuration = 3.5f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.15f;

        private Coroutine notificationRoutine;

        private void OnEnable()
        {
            HideNotification();
        }

        private void Start()
        {
            if (EventSystem.current != null && firstButton != null)
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }

        public void Play()
        {
            ShowNotification("El juego se implementará próximamente.");
        }

        public void OpenOptions()
        {
            ShowNotification("Las opciones se implementarán en la siguiente fase.");
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
