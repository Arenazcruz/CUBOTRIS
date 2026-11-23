using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class SaveAndExitPanel : SettingsOverlay
    {
        public GameController game;
        public Text message;
        private bool saving;
        protected override void OnEnable()
        {
            base.OnEnable();saving=false;
            message.text=GameSave.Store.HasSave?"Se reemplazará la partida guardada anterior.\n\nLa actual se conservará completa y volverás al menú.":"Se guardarán tableros, piezas, tiempos y bonus.\n\nAl continuar, la partida comenzará en pausa.";
        }
        public void SaveAndExit()
        {
            if(saving || !game.IsPaused || game.Model.IsGameOver || Time.frameCount==openedFrame)return;
            saving=true;
            if(!GameSave.Store.TrySave(game.Session,out string error)){message.text=error;saving=false;return;}
            game.MarkSaved();Time.timeScale=1;SceneManager.LoadScene("MainMenu");
        }
    }
}
