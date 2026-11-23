using UnityEngine;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class RotationBonusView : MonoBehaviour
    {
        public GameController game;
        public Text progress,notice;
        private void LateUpdate()
        {
            var session=game.Session;if(session==null)return;
            if(session.IsPractice){progress.text="PRÁCTICA\n\nObjetivo de vuelta desactivado.";notice.text="";return;}
            progress.text="";
            for(int i=0;i<4;i++)progress.text+="CARA "+(i+1)+" · "+((session.CycleClearMask&(1<<i))!=0?"LISTA":"PENDIENTE")+(i==3?"":"\n");
            notice.text=(session.LastRotationBonus>0?"¡VUELTA COMPLETA!\n+400 puntos\n":"")+"TOTAL BONUS: "+session.CycleBonusScore;
        }
    }
}
