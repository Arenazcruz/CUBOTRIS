using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class DisplayPanel : SettingsOverlay
    {
        public Text resolutionText,modeText,status;
        public Button previousButton,nextButton,modeButton,applyButton,keepButton,revertButton,backButton;
        private Vector2Int[] resolutions;
        private int selected;
        private bool fullscreen;
        private DisplayPreview preview;
        protected override void OnEnable()
        {
            base.OnEnable();preview=new DisplayPreview(DisplayPreferences.Apply,DisplayPreferences.Save);
            var current=DisplayPreferences.Load(DisplayPreferences.Current);fullscreen=current.fullscreen;
            resolutions=Screen.resolutions.Select(r=>new Vector2Int(r.width,r.height))
                .Concat(new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(current.width,current.height)})
                .Where(r=>r.x>=640&&r.y>=480).Distinct().OrderBy(r=>r.x).ThenBy(r=>r.y).ToArray();
            selected=System.Array.FindIndex(resolutions,r=>r.x==current.width&&r.y==current.height);if(selected<0)selected=0;
            Refresh();status.text=Application.isEditor?"La resolución se aplica al ejecutable. Confirma para guardar la preferencia.":"Prueba el cambio y confírmalo antes de 10 segundos.";
        }
        public void Previous() {if(preview.IsActive)return;selected=(selected+resolutions.Length-1)%resolutions.Length;Refresh();}
        public void Next() {if(preview.IsActive)return;selected=(selected+1)%resolutions.Length;Refresh();}
        public void ToggleMode() {if(preview.IsActive)return;fullscreen=!fullscreen;Refresh();}
        public void Apply()
        {
            if(preview.IsActive)return;
            var size=resolutions[selected];preview.Begin(DisplayPreferences.Current,new DisplayChoice(size.x,size.y,fullscreen));Refresh();Select(keepButton);
        }
        public void Keep() {preview.Confirm();Refresh();status.text="Configuración guardada.";Select(applyButton);}
        public void Revert() {preview.Cancel();Refresh();status.text="Cambio cancelado. Se restauró la pantalla anterior.";Select(applyButton);}
        protected override void Update()
        {
            base.Update();if(preview==null || !preview.IsActive)return;
            preview.Tick(Time.unscaledDeltaTime);
            if(!preview.IsActive){Refresh();status.text="Tiempo agotado: pantalla anterior restaurada.";Select(applyButton);}
            else status.text=$"¿Conservar estos cambios? Se restaurarán en {Mathf.CeilToInt(preview.Remaining)} s.";
        }
        protected override void OnDisable() {preview?.Cancel();base.OnDisable();}
        private void Refresh()
        {
            var size=resolutions[selected];resolutionText.text=$"{size.x} × {size.y}";modeText.text=fullscreen?"PANTALLA COMPLETA":"VENTANA";
            bool active=preview.IsActive;previousButton.interactable=nextButton.interactable=modeButton.interactable=applyButton.interactable=!active;
            keepButton.gameObject.SetActive(active);revertButton.gameObject.SetActive(active);
            Selectable[] items=active?new Selectable[]{keepButton,revertButton,backButton}:new Selectable[]{previousButton,nextButton,modeButton,applyButton,backButton};
            for(int i=0;i<items.Length;i++)items[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,
                selectOnUp=items[(i+items.Length-1)%items.Length],selectOnDown=items[(i+1)%items.Length],
                selectOnLeft=items[(i+items.Length-1)%items.Length],selectOnRight=items[(i+1)%items.Length]};
        }
    }
}
