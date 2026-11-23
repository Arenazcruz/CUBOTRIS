using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class PracticePanel : SettingsOverlay
    {
        public SettingsOverlay difficultyPanel;
        public Text faceText,modeText,description;
        private int face;
        private DifficultyMode mode;
        private bool starting;
        protected override void OnEnable(){base.OnEnable();mode=GameSelection.Mode;starting=false;Refresh();}
        public void OpenPractice(){difficultyPanel.Close();Open();}
        public void NextFace(){face=(face+1)%4;Refresh();}
        public void NextMode(){mode=(DifficultyMode)(((int)mode+1)%3);Refresh();}
        private void Refresh(){faceText.text="CARA "+(face+1)+"  >";modeText.text=DifficultyRules.Label(mode)+"  >";description.text=DifficultyRules.Description(mode)+"\nEsta cara se mantiene fija. Sin récords competitivos.";}
        public void Begin()
        {
            if(starting || !isActiveAndEnabled || Time.frameCount==openedFrame)return;
            starting=true;GameSelection.Mode=mode;GameSelection.PracticeFace=face;Time.timeScale=1;SceneManager.LoadScene("Game");
        }
    }
}
