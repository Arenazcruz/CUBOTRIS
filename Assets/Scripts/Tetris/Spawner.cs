using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Piece Spawn(Board board, SpriteRenderer blockPrefab)
    {
        if (board.IsGameOver)
            return null;

        // Random.Range con enteros excluye el limite superior: indices de 0 a 6.
        TetrominoType type = (TetrominoType)Random.Range(0, 7);
        Vector2Int[] cells = TetrominoData.GetCells(type);
        Vector2Int position = new Vector2Int(Board.Width / 2 - 1, Board.Height - 2);

        if (!board.IsValidPosition(cells, position))
        {
            board.EndGame();
            return null;
        }

        GameObject pieceObject = new GameObject("Pieza " + type);
        pieceObject.transform.SetParent(board.transform, false);
        Piece piece = pieceObject.AddComponent<Piece>();
        piece.Initialize(board, type, cells, position, blockPrefab);
        return piece;
    }
}
