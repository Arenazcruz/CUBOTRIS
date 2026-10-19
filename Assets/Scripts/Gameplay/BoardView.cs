using UnityEngine;

namespace Cubotris.Gameplay
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer blockPrefab;
        [SerializeField] private Transform activePieceRoot;
        [SerializeField] private Transform placedBlocksRoot;
        [SerializeField] private Sprite[] pieceSprites = new Sprite[7];
        [SerializeField, Range(0.5f, 1f)] private float blockFill = 0.96f;

        private readonly SpriteRenderer[] activeBlocks = new SpriteRenderer[4];
        private SpriteRenderer[,] placedBlocks;
        private int boardVersion = -1;

        public void Render(TetrisGame game)
        {
            if (placedBlocks == null) placedBlocks = new SpriteRenderer[game.Board.Width, game.Board.Height];
            if (boardVersion != game.Board.Version)
            {
                for (int y = 0; y < game.Board.Height; y++)
                    for (int x = 0; x < game.Board.Width; x++)
                    {
                        var type = game.Board[x, y];
                        if (type.HasValue)
                        {
                            if (placedBlocks[x, y] == null) placedBlocks[x, y] = Instantiate(blockPrefab, placedBlocksRoot);
                            SetBlock(placedBlocks[x, y], type.Value, new Vector2Int(x, y));
                        }
                        else if (placedBlocks[x, y] != null)
                        {
                            Destroy(placedBlocks[x, y].gameObject);
                            placedBlocks[x, y] = null;
                        }
                    }
                boardVersion = game.Board.Version;
            }
            var positions = game.Active.Cells;
            for (int i = 0; i < 4; i++)
            {
                if (activeBlocks[i] == null) activeBlocks[i] = Instantiate(blockPrefab, activePieceRoot);
                activeBlocks[i].enabled = game.HasActivePiece && !game.IsGameOver;
                if (activeBlocks[i].enabled) SetBlock(activeBlocks[i], game.Active.Type, positions[i]);
            }
        }

        private void SetBlock(SpriteRenderer renderer, TetrominoType type, Vector2Int cell)
        {
            renderer.sprite = pieceSprites[(int)type];
            renderer.color = Color.white;
            renderer.transform.localPosition = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0);
            var size = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(blockFill / size.x, blockFill / size.y, 1);
            renderer.name = $"{type} [{cell.x},{cell.y}]";
        }
    }
}
