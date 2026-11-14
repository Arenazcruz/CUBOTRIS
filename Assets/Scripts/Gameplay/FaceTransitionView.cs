using UnityEngine;
using UnityEngine.UI;

namespace Cubotris.Gameplay
{
    /// <summary>Slides and fades one saved board view; the old and new faces never overlap.</summary>
    public sealed class FaceTransitionView : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private SpriteRenderer[] frame;
        [SerializeField] private SpriteRenderer grid;
        [SerializeField] private Text faceText;
        [SerializeField, Range(0.3f, 0.6f)] private float duration = 0.45f;
        [SerializeField, Min(0.1f)] private float slideDistance = 1.6f;
        [SerializeField] private Color[] faceColors =
        {
            new Color(0.47f, 0.90f, 0.75f), new Color(0.42f, 0.75f, 1f),
            new Color(0.76f, 0.64f, 1f), new Color(1f, 0.74f, 0.43f)
        };
        public bool IsPlaying { get; private set; }
        public int DisplayedFace { get; private set; }
        public float Progress => IsPlaying ? elapsed / Duration : 0;
        public float Duration => Mathf.Clamp(duration, 0.3f, 0.6f);
        public Color FaceColor(int index) => faceColors[index];
        private Vector3 origin;
        private bool hasOrigin;
        private bool swapped;
        private float elapsed;
        private CubotrisSession session;

        public void Show(CubotrisSession value)
        {
            RememberOrigin();
            IsPlaying = false;
            elapsed = 0;
            session = value;
            boardView.transform.localPosition = origin;
            SetOpacity(1);
            boardView.Render(value.Game);
            ApplyFace(value.ActiveFace);
        }

        public void Begin(CubotrisSession value, int previousFace)
        {
            if (IsPlaying) return;
            RememberOrigin();
            session = value;
            elapsed = 0;
            swapped = false;
            IsPlaying = true;
            // The outgoing board already includes the locked piece and cleared rows.
            boardView.Render(value.Faces[previousFace]);
            ApplyFace(previousFace);
            boardView.transform.localPosition = origin;
            SetOpacity(1);
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying || deltaTime <= 0) return;
            elapsed = Mathf.Min(Duration, elapsed + deltaTime);
            float progress = elapsed / Duration;
            if (!swapped && progress >= 0.5f)
            {
                swapped = true;
                boardView.Render(session.Game, showGhost: false);
                ApplyFace(session.ActiveFace);
            }
            float phase = progress < 0.5f ? progress * 2 : (progress - 0.5f) * 2;
            float eased = Mathf.SmoothStep(0, 1, phase);
            float offset = progress < 0.5f ? -slideDistance * eased : slideDistance * (1 - eased);
            boardView.transform.localPosition = origin + Vector3.right * offset;
            SetOpacity(progress < 0.5f ? 1 - eased : eased);
            if (progress >= 1) Show(session);
        }

        private void RememberOrigin()
        {
            if (hasOrigin) return;
            origin = boardView.transform.localPosition;
            hasOrigin = true;
        }

        private void ApplyFace(int index)
        {
            DisplayedFace = index;
            Color color = faceColors[index];
            foreach (var renderer in frame) renderer.color = color;
            grid.color = Color.Lerp(Color.white, color, 0.2f);
            faceText.color = color;
            faceText.text = $"CARA {index + 1} / {CubotrisSession.FaceCount}";
        }

        private void SetOpacity(float alpha)
        {
            foreach (var renderer in boardView.GetComponentsInChildren<SpriteRenderer>())
            {
                var color = renderer.color; color.a = alpha; renderer.color = color;
            }
        }
    }
}
