using UnityEngine;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    [RequireComponent(typeof(Text))]
    public sealed class ControlsHint : MonoBehaviour
    {
        private int version = -1;
        private void Update()
        {
            if (version == ControlBindings.Version) return;
            version = ControlBindings.Version; var keys = ControlBindings.Current;
            GetComponent<Text>().text = $"{ControlBindings.KeyName(keys[0].primary)} / {ControlBindings.KeyName(keys[1].primary)}  MOVER    "
                + $"{ControlBindings.KeyName(keys[2].primary)}  GIRAR    {ControlBindings.KeyName(keys[3].primary)}  BAJAR    "
                + $"{ControlBindings.KeyName(keys[4].primary)}  CAÍDA    ESC  PAUSA";
        }
    }
}
