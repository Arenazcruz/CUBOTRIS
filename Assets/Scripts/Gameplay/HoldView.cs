using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class HoldView : MonoBehaviour
    {
        public GameController game;
        public Sprite[] sprites;
        public Image[] blocks;
        public Text status;
        private void LateUpdate()
        {
            if (game.Model == null) return;
            var model = game.Model;
            status.text = model.IsGameOver ? "PARTIDA TERMINADA" : model.HoldUsed ? "DISPONIBLE AL FIJAR" : ControlBindings.KeyName(ControlBindings.Current.First(b => b.action == GameAction.Hold).primary) + " · RESERVAR";
            foreach (var block in blocks) block.enabled = model.HeldPiece.HasValue;
            if (!model.HeldPiece.HasValue) return;
            var cells = TetrominoData.Cells(model.HeldPiece.Value, 0, Vector2Int.zero);
            Vector2 min = cells[0], max = cells[0];
            foreach (var cell in cells) { min = Vector2.Min(min, cell); max = Vector2.Max(max, cell); }
            for (int i = 0; i < blocks.Length; i++)
            {
                blocks[i].sprite = sprites[(int)model.HeldPiece.Value];
                blocks[i].color = model.HeldPieceColor;
                blocks[i].rectTransform.anchoredPosition = ((Vector2)cells[i] - (min + max) * .5f) * 36;
            }
        }
    }
}
