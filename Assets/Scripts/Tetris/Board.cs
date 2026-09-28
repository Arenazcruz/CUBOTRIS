using UnityEngine;

[RequireComponent(typeof(Spawner))]
public class Board : MonoBehaviour
{
    public const int Width = 10;
    public const int Height = 20;

    [SerializeField] private SpriteRenderer blockPrefab;

    // Solo contiene bloques fijados. La pieza activa se comprueba por separado.
    private readonly Transform[,] grid = new Transform[Width, Height];
    private Spawner spawner;

    public bool IsGameOver { get; private set; }
    public Piece ActivePiece { get; private set; }

    private void Start()
    {
        spawner = GetComponent<Spawner>();
        if (blockPrefab == null || blockPrefab.sprite == null)
        {
            Debug.LogError("Board: asigna un prefab de bloque con SpriteRenderer y Sprite.", this);
            enabled = false;
            return;
        }

        SpawnNextPiece();
    }

    // Cada celda mide una unidad; el origen del tablero es su esquina inferior izquierda.
    public Vector3 CellToLocalPosition(Vector2Int cell)
    {
        return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
    }

    public bool IsValidPosition(Vector2Int[] cells, Vector2Int position)
    {
        foreach (Vector2Int offset in cells)
        {
            Vector2Int cell = position + offset;
            if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
                return false;

            if (grid[cell.x, cell.y] != null)
                return false;
        }

        return true;
    }

    public void LockPiece(Piece piece)
    {
        if (IsGameOver || piece != ActivePiece)
            return;

        // Los bloques pasan a pertenecer al tablero antes de destruir la pieza vacia.
        for (int i = 0; i < piece.Cells.Length; i++)
        {
            Vector2Int cell = piece.Position + piece.Cells[i];
            Transform block = piece.Blocks[i];
            block.SetParent(transform, false);
            block.localPosition = CellToLocalPosition(cell);
            grid[cell.x, cell.y] = block;
        }

        ActivePiece = null;
        Destroy(piece.gameObject);
        ClearLines();
        SpawnNextPiece();
    }

    private void ClearLines()
    {
        int row = 0;
        while (row < Height)
        {
            if (!IsFullRow(row))
            {
                row++;
                continue;
            }

            for (int x = 0; x < Width; x++)
            {
                Destroy(grid[x, row].gameObject);
                grid[x, row] = null;
            }

            // Todas las filas superiores bajan una celda.
            for (int y = row; y < Height - 1; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    grid[x, y] = grid[x, y + 1];
                    if (grid[x, y] != null)
                        grid[x, y].localPosition = CellToLocalPosition(new Vector2Int(x, y));
                }
            }

            for (int x = 0; x < Width; x++)
                grid[x, Height - 1] = null;

            // Revisar otra vez la misma fila permite eliminar varias lineas juntas.
        }
    }

    private bool IsFullRow(int row)
    {
        for (int x = 0; x < Width; x++)
            if (grid[x, row] == null)
                return false;

        return true;
    }

    private void SpawnNextPiece()
    {
        if (!IsGameOver)
            ActivePiece = spawner.Spawn(this, blockPrefab);
    }

    public void EndGame()
    {
        if (IsGameOver)
            return;

        IsGameOver = true;
        Debug.Log("GAME OVER");
    }

    // Guia de montaje en Scene; no crea objetos ni afecta a las colisiones.
    private void OnDrawGizmos()
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.4f);
        for (int x = 0; x <= Width; x++)
            Gizmos.DrawLine(new Vector3(x, 0, 0), new Vector3(x, Height, 0));
        for (int y = 0; y <= Height; y++)
            Gizmos.DrawLine(new Vector3(0, y, 0), new Vector3(Width, y, 0));
        Gizmos.matrix = previousMatrix;
    }
}
