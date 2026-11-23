using UnityEngine;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class SessionModeLabel : MonoBehaviour
    {
        public GameController game;
        private void LateUpdate(){if(game.Session!=null)GetComponent<Text>().text=DifficultyRules.Label(game.Session.Mode);}
    }
}
