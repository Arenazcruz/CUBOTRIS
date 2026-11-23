using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class ContinuePanel : SettingsOverlay
    {
        public Text message;
        public Button resumeButton;
        private CubotrisSession pending;
        private bool starting;
        protected override void OnEnable()
        {
            base.OnEnable();starting=false;
            bool loaded=GameSave.Store.TryLoad(out pending,out string error);resumeButton.interactable=loaded;
            if(loaded && (PersonalStats.Store.IsFinal(pending.RunId) || BestScoreStore.History.Contains(pending.RunId)))
            {loaded=false;pending=null;resumeButton.interactable=false;error="Esta partida ya terminó. Inicia una nueva desde JUGAR.";}
            message.text=loaded?(pending.IsPractice?"PRÁCTICA · ":"")+DifficultyRules.Label(pending.Mode)+"\nCARA "+(pending.ActiveFace+1)+" · "+pending.Game.Score+" PUNTOS\nTIEMPO "+GameHudView.FormatTime(pending.SurvivalSeconds)+"\n\nContinuarás en pausa.":error;
            Select(loaded?resumeButton:firstControl);
        }
        public void Resume()
        {
            if(starting || pending==null || Time.frameCount==openedFrame)return;
            starting=true;GameSave.ResumeSession=pending;GameSelection.Mode=pending.Mode;GameSelection.PracticeFace=pending.IsPractice?pending.ActiveFace:-1;
            Time.timeScale=1;SceneManager.LoadScene("Game");
        }
    }
}
