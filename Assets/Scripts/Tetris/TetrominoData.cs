using UnityEngine;

public enum TetrominoType { I, J, L, O, S, T, Z }

// Solo datos: cada pieza ocupa cuatro celdas relativas a su origen.
public static class TetrominoData
{
    public static Vector2Int[] GetCells(TetrominoType type)
    {
        // Se devuelve un arreglo nuevo para no modificar otras piezas al rotar.
        switch (type)
        {
            case TetrominoType.I:
                return new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
            case TetrominoType.J:
                return new[] { new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) };
            case TetrominoType.L:
                return new[] { new Vector2Int(1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) };
            case TetrominoType.O:
                return new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 0), new Vector2Int(1, 0) };
            case TetrominoType.S:
                return new[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0) };
            case TetrominoType.T:
                return new[] { new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) };
            case TetrominoType.Z:
                return new[] { new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0) };
            default:
                throw new System.ArgumentOutOfRangeException(nameof(type));
        }
    }

    public static Color GetColor(TetrominoType type)
    {
        switch (type)
        {
            case TetrominoType.I: return Color.cyan;
            case TetrominoType.J: return new Color(0.25f, 0.4f, 1f);
            case TetrominoType.L: return new Color(1f, 0.55f, 0.1f);
            case TetrominoType.O: return Color.yellow;
            case TetrominoType.S: return Color.green;
            case TetrominoType.T: return new Color(0.75f, 0.3f, 1f);
            case TetrominoType.Z: return Color.red;
            default: return Color.white;
        }
    }

    public static Vector2Int[] RotateClockwise(TetrominoType type, Vector2Int[] cells)
    {
        Vector2Int[] rotated = new Vector2Int[cells.Length];
        // I gira alrededor de medio bloque; las demas, alrededor de (0, 0).
        Vector2 pivot = type == TetrominoType.I ? new Vector2(0.5f, -0.5f) : Vector2.zero;

        for (int i = 0; i < cells.Length; i++)
        {
            if (type == TetrominoType.O)
            {
                rotated[i] = cells[i]; // El cuadrado no cambia al rotar.
                continue;
            }

            Vector2 relative = (Vector2)cells[i] - pivot;
            rotated[i] = new Vector2Int(
                Mathf.RoundToInt(relative.y + pivot.x),
                Mathf.RoundToInt(-relative.x + pivot.y));
        }

        return rotated;
    }
}
