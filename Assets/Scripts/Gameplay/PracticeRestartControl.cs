using UnityEngine;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class PracticeRestartControl : MonoBehaviour
    {
        public GameController game;
        public Button button;
        private void Update(){if(game.Session==null)return;button.gameObject.SetActive(game.Session.IsPractice);button.interactable=game.AcceptsInput;}
        public void Restart(){if(game.Session.IsPractice && game.AcceptsInput)game.Restart();}
    }
}
