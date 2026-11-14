using UnityEngine;
using UnityEngine.UI;

namespace Cubotris.Gameplay
{
    /// <summary>One UI mesh per board. Only locked cells are drawn; no runtime cell GameObjects.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MiniBoardView : MaskableGraphic
    {
        [SerializeField, Range(0.5f, 1f)] private float cellFill = 0.86f;
        private BoardState board;
        private int boardVersion = -1;

        public void Display(BoardState value)
        {
            if (board == value && boardVersion == value.Version) return;
            board = value;
            boardVersion = value.Version;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (board == null) return;
            Rect rect = rectTransform.rect;
            float width = rect.width / board.Width, height = rect.height / board.Height;
            float inset = (1 - cellFill) * 0.5f;
            for (int y = 0; y < board.Height; y++)
                for (int x = 0; x < board.Width; x++)
                {
                    if (!board[x, y].HasValue) continue;
                    float left = rect.xMin + (x + inset) * width;
                    float bottom = rect.yMin + (y + inset) * height;
                    float right = left + width * cellFill, top = bottom + height * cellFill;
                    int start = mesh.currentVertCount;
                    mesh.AddVert(new Vector3(left, bottom), color, Vector2.zero);
                    mesh.AddVert(new Vector3(left, top), color, Vector2.zero);
                    mesh.AddVert(new Vector3(right, top), color, Vector2.zero);
                    mesh.AddVert(new Vector3(right, bottom), color, Vector2.zero);
                    mesh.AddTriangle(start, start + 1, start + 2);
                    mesh.AddTriangle(start + 2, start + 3, start);
                }
        }
    }
}
