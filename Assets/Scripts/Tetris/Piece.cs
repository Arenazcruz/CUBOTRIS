using UnityEngine;
using UnityEngine.InputSystem;

public class Piece : MonoBehaviour
{
    // Tiempos en segundos. No hay niveles ni aumento de dificultad por ahora.
    private const float FallInterval = 0.8f;
    private const float SoftDropInterval = 0.05f;
    private const float MoveDelay = 0.18f;
    private const float MoveRepeatInterval = 0.1f;

    private Board board;
    private TetrominoType type;
    private float fallTimer;
    private float currentFallInterval = FallInterval;
    private float moveTimer;
    private int heldDirection;
    private int spawnFrame;
    private bool locked;

    public Vector2Int Position { get; private set; }
    public Vector2Int[] Cells { get; private set; }
    public Transform[] Blocks { get; private set; }

    public void Initialize(Board owner, TetrominoType tetromino, Vector2Int[] cells,
        Vector2Int position, SpriteRenderer blockPrefab)
    {
        board = owner;
        type = tetromino;
        Cells = cells;
        Position = position;
        spawnFrame = Time.frameCount;
        Blocks = new Transform[cells.Length];

        for (int i = 0; i < Blocks.Length; i++)
        {
            SpriteRenderer block = Instantiate(blockPrefab, transform);
            block.color = TetrominoData.GetColor(type);
            block.gameObject.SetActive(true);
            Blocks[i] = block.transform;
        }

        UpdateBlockPositions();
    }

    private void Update()
    {
        if (board == null || board.IsGameOver || locked || Time.frameCount == spawnFrame)
            return;

        Keyboard keyboard = Keyboard.current;
        bool softDrop = false;
        if (keyboard != null)
        {
            HandleHorizontalInput(keyboard);

            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                Rotate();

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                HardDrop();
                return; // La pieza nueva no recibe la caida de esta pieza.
            }

            softDrop = keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed;
        }

        float interval = softDrop ? SoftDropInterval : FallInterval;
        // Al acelerar, no convertir el tiempo de caida normal en muchos pasos de golpe.
        if (interval != currentFallInterval)
        {
            fallTimer = Mathf.Min(fallTimer, interval);
            currentFallInterval = interval;
        }

        fallTimer += Time.deltaTime;
        while (fallTimer >= interval && !locked)
        {
            fallTimer -= interval;
            if (!Move(Vector2Int.down))
                Lock();
        }
    }

    private void HandleHorizontalInput(Keyboard keyboard)
    {
        bool left = keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed;
        bool right = keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed;
        int direction = (right ? 1 : 0) - (left ? 1 : 0);

        if (direction == 0)
        {
            heldDirection = 0;
            moveTimer = 0f;
            return;
        }

        if (direction != heldDirection)
        {
            heldDirection = direction;
            Move(new Vector2Int(direction, 0));
            moveTimer = MoveDelay;
            return;
        }

        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0f)
        {
            Move(new Vector2Int(direction, 0));
            moveTimer = MoveRepeatInterval;
        }
    }

    public bool Move(Vector2Int direction)
    {
        if (board == null || board.IsGameOver || locked)
            return false;

        Vector2Int nextPosition = Position + direction;
        if (!board.IsValidPosition(Cells, nextPosition))
            return false;

        Position = nextPosition;
        UpdateBlockPositions();
        return true;
    }

    public void Rotate()
    {
        if (board == null || board.IsGameOver || locked)
            return;

        Vector2Int[] rotated = TetrominoData.RotateClockwise(type, Cells);
        // Pequenos ajustes horizontales permiten girar junto a una pared.
        // Si ninguna posicion cabe, se conserva la orientacion original.
        int[] horizontalOffsets = { 0, -1, 1, -2, 2 };
        foreach (int offset in horizontalOffsets)
        {
            Vector2Int candidate = Position + new Vector2Int(offset, 0);
            if (!board.IsValidPosition(rotated, candidate))
                continue;

            Cells = rotated;
            Position = candidate;
            UpdateBlockPositions();
            return;
        }
    }

    public void HardDrop()
    {
        if (board == null || board.IsGameOver || locked)
            return;

        while (Move(Vector2Int.down)) { }
        Lock();
    }

    private void Lock()
    {
        locked = true;
        enabled = false;
        board.LockPiece(this);
    }

    private void UpdateBlockPositions()
    {
        for (int i = 0; i < Blocks.Length; i++)
            Blocks[i].localPosition = board.CellToLocalPosition(Position + Cells[i]);
    }
}
