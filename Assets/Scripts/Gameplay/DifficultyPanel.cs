using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class DifficultyPanel : SettingsOverlay
    {
        public Text current;
        public UnityEvent changed=new UnityEvent();
        protected override void OnEnable(){base.OnEnable();Refresh();}
        public void Normal()=>Choose(DifficultyMode.Normal);
        public void Relaxed()=>Choose(DifficultyMode.Relaxed);
        public void Challenge()=>Choose(DifficultyMode.Challenge);
        private void Choose(DifficultyMode mode){GameSelection.Mode=mode;Refresh();changed.Invoke();}
        private void Refresh(){current.text="SELECCIONADO: "+DifficultyRules.Label(GameSelection.Mode)+"\nCada modo tiene su propia clasificación.";}
    }
}
