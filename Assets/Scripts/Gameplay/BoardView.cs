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
        private BoardState renderedBoard;

        public void Render(TetrisGame game)
        {
            Render(game.Board, game.HasActivePiece && !game.IsGameOver ? game.Active : (ActivePiece?)null);
        }

        public void Render(BoardState board, ActivePiece? active = null)
        {
            if (renderedBoard != board)
            {
                renderedBoard = board;
                boardVersion = -1;
            }
            if (placedBlocks == null) placedBlocks = new SpriteRenderer[board.Width, board.Height];
            if (boardVersion != board.Version)
            {
                for (int y = 0; y < board.Height; y++)
                    for (int x = 0; x < board.Width; x++)
                    {
                        var type = board[x, y];
                        if (type.HasValue)
                        {
                            if (placedBlocks[x, y] == null) placedBlocks[x, y] = Instantiate(blockPrefab, placedBlocksRoot);
                            SetBlock(placedBlocks[x, y], type.Value, new Vector2Int(x, y));
                        }
                        else if (placedBlocks[x, y] != null)
                        {
                            placedBlocks[x, y].enabled = false;
                            if (Application.isPlaying) Destroy(placedBlocks[x, y].gameObject);
                            else DestroyImmediate(placedBlocks[x, y].gameObject);
                            placedBlocks[x, y] = null;
                        }
                    }
                boardVersion = board.Version;
            }
            var positions = active?.Cells;
            for (int i = 0; i < 4; i++)
            {
                if (activeBlocks[i] == null) activeBlocks[i] = Instantiate(blockPrefab, activePieceRoot);
                activeBlocks[i].enabled = active.HasValue;
                if (activeBlocks[i].enabled) SetBlock(activeBlocks[i], active.Value.Type, positions[i]);
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
