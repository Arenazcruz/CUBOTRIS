using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class ControlsPanel : SettingsOverlay
    {
        public Button[] primaryButtons, secondaryButtons;
        public Text status;
        private int listening = -1, captureFrame;
        private bool secondary, previousNavigation;
        protected override void OnEnable() { base.OnEnable(); listening = -1; Refresh(); status.text = "Selecciona una tecla para cambiarla. ESC cancela."; }
        private void Start()
        {
            for (int i = 0; i < primaryButtons.Length; i++)
            {
                int index = i;
                primaryButtons[i].onClick.AddListener(() => Listen(index, false));
                secondaryButtons[i].onClick.AddListener(() => Listen(index, true));
            }
        }
        private void Listen(int index, bool alternate)
        {
            if (listening >= 0 || Time.frameCount == openedFrame) return;
            listening = index; secondary = alternate; captureFrame = Time.frameCount; ControlBindings.Capturing = true;
            if (EventSystem.current != null) { previousNavigation = EventSystem.current.sendNavigationEvents; EventSystem.current.sendNavigationEvents = false; }
            status.text = "Pulsa la nueva tecla. ESC cancela." + (alternate ? " Supr elimina la alternativa." : "");
        }
        protected override void Update()
        {
            if (listening < 0) { base.Update(); return; }
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;
            if (Keyboard.current == null || Time.frameCount == captureFrame) return;
            foreach (var key in Keyboard.current.allKeys)
            {
                if (!key.wasPressedThisFrame) continue;
                if (key.keyCode == Key.Escape) { StopListening("Cambio cancelado."); return; }
                var value = secondary && key.keyCode == Key.Delete ? Key.None : key.keyCode;
                if (ControlBindings.TryRebind((GameAction)listening, secondary, value, out string error)) StopListening("Tecla guardada.");
                else status.text = error + " Pulsa otra tecla o ESC.";
                return;
            }
        }
        private void StopListening(string message)
        {
            var target = secondary ? secondaryButtons[listening] : primaryButtons[listening];
            RestoreNavigation(); listening = -1; ControlBindings.Capturing = false; openedFrame = Time.frameCount;
            Refresh(); status.text = message; Select(target);
        }
        public void RestoreDefaults()
        { if (listening >= 0) return; ControlBindings.ResetDefaults(); Refresh(); status.text = "Controles predeterminados restaurados."; }
        private void Refresh()
        {
            var bindings = ControlBindings.Current;
            for (int i = 0; i < primaryButtons.Length; i++)
            {
                primaryButtons[i].GetComponentInChildren<Text>().text = ControlBindings.KeyName(bindings[i].primary);
                secondaryButtons[i].GetComponentInChildren<Text>().text = ControlBindings.KeyName(bindings[i].secondary);
            }
        }
        private void RestoreNavigation() { if (listening >= 0 && EventSystem.current != null) EventSystem.current.sendNavigationEvents = previousNavigation; }
        protected override void OnDisable() { base.OnDisable(); RestoreNavigation(); listening = -1; ControlBindings.Capturing = false; }
    }
}
