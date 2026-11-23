using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public class SettingsOverlay : MonoBehaviour
    {
        public static int OpenCount { get; private set; }
        public Selectable firstControl, returnControl;
        protected int openedFrame;
        protected virtual void OnEnable()
        { OpenCount++; openedFrame = Time.frameCount; Select(firstControl); }
        protected virtual void OnDisable() { OpenCount = Mathf.Max(0, OpenCount - 1); }
        public void Open() { gameObject.SetActive(true); transform.SetAsLastSibling(); }
        public virtual void Close() { gameObject.SetActive(false); Select(returnControl); }
        protected virtual void Update()
        { if (Time.frameCount != openedFrame && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close(); }
        protected static void Select(Selectable item)
        { if (item != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(item.gameObject); }
    }
}
