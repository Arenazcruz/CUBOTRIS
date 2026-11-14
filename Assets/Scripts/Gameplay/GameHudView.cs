using UnityEngine;
using UnityEngine.UI;

namespace Cubotris.Gameplay
{
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField] private GameController game;
        [SerializeField] private FaceTransitionView transition;
        [SerializeField] private Sprite[] pieceSprites = new Sprite[7];
        [SerializeField] private Image[] nextBlocks = new Image[4];
        [SerializeField, Min(1)] private float previewCellSize = 42;
        [SerializeField] private MiniBoardView[] miniBoards = new MiniBoardView[4];
        [SerializeField] private Image[] faceBorders = new Image[4];
        [SerializeField] private Text[] faceMarkers = new Text[4];
        [SerializeField] private Text nextFaceText;
        [SerializeField] private Text survivalTimeText;
        [SerializeField] private Text[] difficultyTexts = new Text[4];
        private long shownSeconds = -1;
        private readonly int[] shownRemaining = { -1, -1, -1, -1 };
        private readonly int[] shownPending = { -1, -1, -1, -1 };
        private TetrominoType? shownPiece;
        private bool hasPreview;
        private int shownFace = -1;

        private void LateUpdate()
        {
            if (game.Session != null) Refresh(game.Session, transition.DisplayedFace);
        }

        public void Refresh(CubotrisSession session, int displayedFace)
        {
            long seconds = (long)System.Math.Floor(session.SurvivalSeconds);
            if (shownSeconds != seconds)
            {
                shownSeconds = seconds;
                survivalTimeText.text = FormatTime(session.SurvivalSeconds);
            }
            for (int i = 0; i < difficultyTexts.Length; i++)
            {
                int remaining = (int)System.Math.Ceiling(session.SecondsUntilGarbage(i));
                int pending = session.PendingGarbageRows(i);
                if (shownRemaining[i] == remaining && shownPending[i] == pending) continue;
                shownRemaining[i] = remaining; shownPending[i] = pending;
                difficultyTexts[i].text = i == 0 ? "SIN\nPENALIZACIÓN" : pending > 0
                    ? $"+{pending} AL FIJAR\n+{i} EN {remaining:00} s"
                    : $"+{i} {(i == 1 ? "FILA" : "FILAS")}\nEN {remaining:00} s";
            }
            for (int i = 0; i < miniBoards.Length; i++) miniBoards[i].Display(session.Faces[i]);
            if (shownFace != displayedFace)
            {
                shownFace = displayedFace;
                int next = (displayedFace + 1) % CubotrisSession.FaceCount;
                nextFaceText.text = $"SIGUIENTE: {next + 1} / {CubotrisSession.FaceCount}";
                nextFaceText.color = transition.FaceColor(next);
                for (int i = 0; i < faceMarkers.Length; i++)
                {
                    bool current = i == displayedFace, upcoming = i == next;
                    faceMarkers[i].text = current ? "ACTUAL" : upcoming ? "SIGUIENTE" : "";
                    faceMarkers[i].color = transition.FaceColor(i);
                    var tint = transition.FaceColor(i);
                    tint.a = current ? 1 : upcoming ? 0.65f : 0.2f;
                    faceBorders[i].color = tint;
                }
            }
            TetrominoType? nextPiece = session.Game.IsGameOver ? null : session.Game.NextPiece;
            if (hasPreview && shownPiece == nextPiece) return;
            hasPreview = true;
            shownPiece = nextPiece;
            foreach (var block in nextBlocks) block.enabled = nextPiece.HasValue;
            if (!nextPiece.HasValue) return;
            var cells = TetrominoData.Cells(nextPiece.Value, 0, Vector2Int.zero);
            Vector2 min = cells[0], max = cells[0];
            foreach (var cell in cells) { min = Vector2.Min(min, cell); max = Vector2.Max(max, cell); }
            var center = (min + max) * 0.5f;
            for (int i = 0; i < nextBlocks.Length; i++)
            {
                nextBlocks[i].sprite = pieceSprites[(int)nextPiece.Value];
                nextBlocks[i].rectTransform.anchoredPosition = ((Vector2)cells[i] - center) * previewCellSize;
                nextBlocks[i].rectTransform.sizeDelta = Vector2.one * previewCellSize * 0.94f;
            }
        }

        public static string FormatTime(double seconds)
        {
            long whole = (long)System.Math.Floor(System.Math.Max(0, seconds));
            return $"{whole / 60:00}:{whole % 60:00}";
        }
    }
}
