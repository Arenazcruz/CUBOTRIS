using UnityEngine;
using UnityEngine.UI;

namespace Cubotris.Gameplay
{
    /// <summary>
    /// Cube-style face change: the current board turns away to the left while the next board
    /// enters from the right, as if the four boards were the sides of a cube rotating left.
    /// The outgoing board is a frozen visual copy; the real BoardView is always the incoming one.
    /// </summary>
    public sealed class FaceTransitionView : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private SpriteRenderer[] frame;
        [SerializeField] private SpriteRenderer grid;
        [SerializeField] private Text faceText;
        [SerializeField, Range(0.3f, 0.6f)] private float duration = 0.45f;
        [Tooltip("Opacity of a face when it is completely sideways (fakes shading).")]
        [SerializeField, Range(0f, 1f)] private float edgeOpacity = 0.35f;
        [Tooltip("Seconds the board takes to flip upside down (or back).")]
        [SerializeField, Range(0.2f, 1.5f)] private float flipDuration = 0.6f;
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
        private Quaternion originRotation;
        private Vector3 originScale;
        private Vector3 pivotLocal;
        private float halfWidth;
        private bool hasOrigin;
        private bool labelSwapped;
        private float elapsed;
        private CubotrisSession session;
        private GameObject outgoing;
        private float flipFrom, flipTo, flipAngle, flipElapsed;
        public bool IsUpsideDown => flipTo >= 180f;
        public bool IsFlipping => flipAngle != flipTo;

        public void Show(CubotrisSession value)
        {
            RememberOrigin();
            IsPlaying = false;
            elapsed = 0;
            session = value;
            DestroyOutgoing();
            ApplyPose(boardView.transform, 0);
            SetOpacity(boardView.transform, 1);
            boardView.Render(value.Game);
            ApplyFace(value.ActiveFace);
            ApplyLabel(value.ActiveFace);
        }

        public void Begin(CubotrisSession value, int previousFace)
        {
            if (IsPlaying) return;
            RememberOrigin();
            session = value;
            elapsed = 0;
            labelSwapped = false;
            IsPlaying = true;

            // Freeze a copy of the outgoing face (it already includes the locked piece and cleared rows).
            ApplyPose(boardView.transform, 0);
            SetOpacity(boardView.transform, 1);
            boardView.Render(value.Faces[previousFace]);
            ApplyFace(previousFace);
            ApplyLabel(previousFace);
            DestroyOutgoing();
            outgoing = Instantiate(boardView.gameObject, boardView.transform.parent);
            outgoing.name = "OutgoingFace";
            DestroyImmediate(outgoing.GetComponent<BoardView>());

            // The real board becomes the incoming face, waiting on the right side of the cube.
            boardView.Render(value.Game, showGhost: false);
            ApplyFace(value.ActiveFace);
            UpdatePoses(0);
        }

        public void Advance(float deltaTime)
        {
            if (!IsPlaying || deltaTime <= 0) return;
            elapsed = Mathf.Min(Duration, elapsed + deltaTime);
            float progress = elapsed / Duration;
            UpdatePoses(Mathf.SmoothStep(0, 1, progress));
            if (!labelSwapped && progress >= 0.5f)
            {
                labelSwapped = true;
                ApplyLabel(session.ActiveFace);
            }
            if (progress >= 1) Show(session);
        }

        /// <summary>Starts flipping the board over its horizontal axis (normal &lt;-&gt; upside down).</summary>
        public void Flip()
        {
            RememberOrigin();
            flipFrom = flipAngle;
            flipTo = IsUpsideDown ? 0f : 180f;
            flipElapsed = 0;
        }

        /// <summary>Advances the flip animation; works both during and outside a face change.</summary>
        public void TickFlip(float deltaTime)
        {
            if (!IsFlipping || deltaTime <= 0) return;
            flipElapsed = Mathf.Min(flipDuration, flipElapsed + deltaTime);
            float t = Mathf.SmoothStep(0, 1, flipElapsed / flipDuration);
            flipAngle = flipElapsed >= flipDuration ? flipTo : Mathf.Lerp(flipFrom, flipTo, t);
            if (!IsPlaying) ApplyPose(boardView.transform, 0);
        }

        /// <summary>Puts the board back to its normal orientation immediately.</summary>
        public void ResetFlip()
        {
            flipFrom = flipTo = flipAngle = flipElapsed = 0;
            if (hasOrigin && !IsPlaying) ApplyPose(boardView.transform, 0);
        }

        /// <param name="t">0 = old face in front, 1 = new face in front.</param>
        private void UpdatePoses(float t)
        {
            float turn = 90f * t;
            float outAngle = turn;          // 0° -> 90°: turns away to the left
            float inAngle = turn - 90f;     // -90° -> 0°: comes in from the right
            if (outgoing != null)
            {
                ApplyPose(outgoing.transform, outAngle);
                SetOpacity(outgoing.transform, Mathf.Lerp(edgeOpacity, 1, Mathf.Cos(outAngle * Mathf.Deg2Rad)));
            }
            ApplyPose(boardView.transform, inAngle);
            SetOpacity(boardView.transform, Mathf.Lerp(edgeOpacity, 1, Mathf.Cos(inAngle * Mathf.Deg2Rad)));
        }

        private void RememberOrigin()
        {
            if (hasOrigin) return;
            var root = boardView.transform;
            origin = root.localPosition;
            originRotation = root.localRotation;
            originScale = root.localScale;
            // Rotate around the visual centre of the board (the grid), not the bottom-left pivot.
            pivotLocal = grid != null ? root.InverseTransformPoint(grid.bounds.center) : Vector3.zero;
            pivotLocal.z = 0;
            halfWidth = Mathf.Abs(pivotLocal.x * originScale.x);
            hasOrigin = true;
        }

        /// <summary>Places a face of a cube (whose centre is halfWidth behind the board) at the given yaw.</summary>
        private void ApplyPose(Transform target, float angleY)
        {
            float rad = angleY * Mathf.Deg2Rad;
            var rotation = originRotation * Quaternion.Euler(0, angleY, 0);
            Vector3 frontCenter = origin + originRotation * Vector3.Scale(originScale, pivotLocal);
            Vector3 cubeOffset = originRotation * new Vector3(-halfWidth * Mathf.Sin(rad), 0, halfWidth * (1 - Mathf.Cos(rad)));
            Vector3 position = frontCenter + cubeOffset - rotation * Vector3.Scale(originScale, pivotLocal);
            // Upside-down flip: rotate the whole cube pose around the board centre on the horizontal axis.
            var flip = originRotation * Quaternion.Euler(flipAngle, 0, 0) * Quaternion.Inverse(originRotation);
            target.localRotation = flip * rotation;
            target.localScale = originScale;
            target.localPosition = frontCenter + flip * (position - frontCenter);
        }

        private void DestroyOutgoing()
        {
            if (outgoing == null) return;
            outgoing.SetActive(false);
            Destroy(outgoing);
            outgoing = null;
        }

        private void ApplyFace(int index)
        {
            Color color = faceColors[index];
            foreach (var renderer in frame) renderer.color = color;
            grid.color = Color.Lerp(Color.white, color, 0.2f);
        }

        private void ApplyLabel(int index)
        {
            DisplayedFace = index;
            faceText.color = faceColors[index];
            faceText.text = $"CARA {index + 1} / {CubotrisSession.FaceCount}";
        }

        private static void SetOpacity(Transform root, float alpha)
        {
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>())
            {
                var color = renderer.color; color.a = alpha; renderer.color = color;
            }
        }

        private void OnDestroy() => DestroyOutgoing();
    }
}
