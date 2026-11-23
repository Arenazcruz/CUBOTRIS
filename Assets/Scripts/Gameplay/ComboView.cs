using UnityEngine;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class ComboView : MonoBehaviour
    {
        public GameController game;
        public Text value,detail;
        private void LateUpdate()
        {
            var model=game.Model;if(model==null)return;
            value.text=model.ComboChain>0?"RACHA "+model.ComboChain:"SIN RACHA";
            detail.text=(model.LastComboBonus>0?"+"+model.LastComboBonus+" PUNTOS":"Limpia con la siguiente pieza")+"\nMEJOR RACHA: "+model.MaxCombo+"\nTOTAL BONUS: "+model.ComboBonusScore;
        }
    }
}
