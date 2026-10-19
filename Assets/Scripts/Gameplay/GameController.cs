using UnityEngine;
using UnityEngine.UI;

namespace Cubotris.Gameplay
{
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private FaceTransitionView faceTransition;
        [SerializeField] private Text linesText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text scoreText;
        [SerializeField] private GameObject gameOverNotice;
        [SerializeField] private FallSettings fallSettings = new FallSettings();
        [SerializeField] private GarbageSettings garbageSettings = new GarbageSettings();

        public CubotrisSession Session { get; private set; }
        public TetrisGame Model => Session?.Game;
        public bool IsPaused { get; private set; }
        public bool IsTransitioning => faceTransition.IsPlaying;
        public int InputVersion { get; private set; }
        public bool AcceptsInput => isActiveAndEnabled && Model != null && !Model.IsGameOver && !IsPaused && !IsTransitioning && Time.frameCount != transitionFrame;
        public bool SoftDropHeld { get; set; }
        private int lastRevision = -1;
        private bool skipGravityOnce;
        private int transitionFrame = -1;
        private int timedFrame = -1;

        private void Start()
        {
            Session = new CubotrisSession(settings: fallSettings, garbageSettings: garbageSettings);
            timedFrame = Time.frameCount;
            faceTransition.Show(Session);
            Refresh();
        }

        private void Update()
        {
            if (IsTransitioning)
            {
                if (!IsPaused)
                {
                    faceTransition.Advance(Time.deltaTime);
                    if (!IsTransitioning) { ResetInput(); lastRevision = -1; Refresh(); }
                }
                return;
            }
            int lockedBefore = Model.PiecesLocked;
            AdvanceActiveTime();
            if (AcceptsInput && !skipGravityOnce) Model.Tick(Time.deltaTime, SoftDropHeld);
            skipGravityOnce = false;
            HandlePlacement(lockedBefore);
            Refresh();
        }

        public void Move(int direction) { if (AcceptsInput) Model.Move(direction); }
        public void Rotate() { if (AcceptsInput) Model.RotateClockwise(); }
        public void HardDrop()
        {
            if (!AcceptsInput) return;
            AdvanceActiveTime();
            int lockedBefore = Model.PiecesLocked;
            Model.HardDrop();
            HandlePlacement(lockedBefore);
            skipGravityOnce = true;
        }

        private void AdvanceActiveTime()
        {
            if (timedFrame == Time.frameCount || !AcceptsInput || !Model.HasActivePiece || Time.timeScale <= 0) return;
            timedFrame = Time.frameCount;
            Session.AdvanceActiveTime(Time.unscaledDeltaTime);
        }

        private void HandlePlacement(int lockedBefore)
        {
            if (Model.PiecesLocked == lockedBefore) return;
            ResetInput();
            if (Model.IsGameOver) faceTransition.Show(Session);
            else faceTransition.Begin(Session, (Session.ActiveFace + CubotrisSession.FaceCount - 1) % CubotrisSession.FaceCount);
            Refresh();
        }

        public void SetPaused(bool paused)
        {
            if (Model == null || Model.IsGameOver || IsPaused == paused) return;
            if (paused) AdvanceActiveTime();
            SaveRecord();
            IsPaused = paused;
            Time.timeScale = paused ? 0 : 1;
            ResetInput();
            stateText.text = paused ? "EN PAUSA" : IsTransitioning ? "CAMBIANDO CARA" : "EN JUEGO";
        }

        public void Restart()
        {
            SaveRecord();
            Session = new CubotrisSession(settings: fallSettings, garbageSettings: garbageSettings);
            timedFrame = Time.frameCount;
            faceTransition.Show(Session);
            IsPaused = false;
            Time.timeScale = 1;
            skipGravityOnce = false;
            ResetInput();
            lastRevision = -1;
            Refresh();
        }

        private void ResetInput()
        {
            SoftDropHeld = false;
            InputVersion++;
            transitionFrame = Time.frameCount;
        }

        public void SaveRecord() { if (Model != null) BestScoreStore.Record(Model.Score); }
        private void OnApplicationPause(bool paused) { if (paused) SaveRecord(); }
        private void OnApplicationQuit() { SaveRecord(); }
        private void OnDestroy() { SaveRecord(); Time.timeScale = 1; }

        private void Refresh()
        {
            if (lastRevision == Model.Revision) return;
            if (!IsTransitioning) boardView.Render(Model);
            SaveRecord();
            linesText.text = Model.LinesCleared.ToString("00");
            scoreText.text = Model.Score.ToString();
            stateText.text = Model.IsGameOver ? "PARTIDA TERMINADA" : IsPaused ? "EN PAUSA" : IsTransitioning ? "CAMBIANDO CARA" : "EN JUEGO";
            gameOverNotice.SetActive(Model.IsGameOver);
            lastRevision = Model.Revision;
        }
    }
}
