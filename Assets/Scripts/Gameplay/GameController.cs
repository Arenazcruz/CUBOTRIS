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
        public bool LeavingWithSave { get; set; }
        public string SaveNotice { get; private set; }
        private bool runClosed;
        private int lastRevision = -1;
        private bool skipGravityOnce;
        private int transitionFrame = -1;
        private int timedFrame = -1;
        private ScoreRecord finishedRecord;
        public ScoreRecord FinishedRecord => Model == null || !Model.IsGameOver ? null : (finishedRecord ??= new ScoreRecord
        {
            id = Session.RunId, score = Model.Score, mode = Session.Mode, practice = Session.IsPractice,
            survivalSeconds = Session.SurvivalSeconds, finishedAtUtc = System.DateTimeOffset.UtcNow.ToString("O")
        }).Copy();

        private void Start()
        {
            var resumed=GameSave.TakeResume();
            Session = resumed ?? new CubotrisSession(settings: fallSettings, garbageSettings: garbageSettings, mode: GameSelection.Mode, practiceFace: GameSelection.PracticeFace);
            IsPaused=resumed!=null;Time.timeScale=IsPaused?0:1;
            if(IsPaused)ResetInput();
            RecordStatistics(RunOutcome.Started);
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
        public void Hold()
        {
            if (!AcceptsInput || !Model.CanHold) return;
            AdvanceActiveTime();
            Model.Hold();
            ResetInput();
            skipGravityOnce = true;
            Refresh();
        }
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
            if (Model.IsGameOver || Session.IsPractice) faceTransition.Show(Session);
            else faceTransition.Begin(Session, (Session.ActiveFace + CubotrisSession.FaceCount - 1) % CubotrisSession.FaceCount);
            Refresh();
        }

        public void SetPaused(bool paused)
        {
            if (Model == null || Model.IsGameOver || IsPaused == paused) return;
            if (paused) AdvanceActiveTime();
            IsPaused = paused;
            Time.timeScale = paused ? 0 : 1;
            ResetInput();
            stateText.text = paused ? "EN PAUSA" : IsTransitioning ? "CAMBIANDO CARA" : "EN JUEGO";
        }

        public void Restart()
        {
            Abandon();
            runClosed=false;SaveNotice=null;LeavingWithSave=false;
            finishedRecord = null;
            Session = new CubotrisSession(settings: fallSettings, garbageSettings: garbageSettings, mode: Session.Mode, practiceFace: Session.IsPractice?Session.ActiveFace:-1);
            RecordStatistics(RunOutcome.Started);
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

        public void Abandon()=>CloseRun();
        public void MarkSaved(){LeavingWithSave=true;RecordStatistics(RunOutcome.Suspended);}
        private void RecordStatistics(RunOutcome outcome)
        {if(!PersonalStats.Store.TryUpdate(Session,outcome,out string error))SaveNotice=error;}
        private void CloseRun()
        {
            if(runClosed || Session==null)return;
            runClosed=true;
            RecordStatistics(Model.IsGameOver?RunOutcome.Completed:RunOutcome.Abandoned);
            if(!GameSave.Store.TryClose(Session.RunId,out string error))SaveNotice=error;
        }

        private void PreserveOrAbandon()
        {
            if(runClosed || LeavingWithSave || Session==null)return;
            if(!Model.IsGameOver && GameSave.Store.TryLoad(out var checkpoint,out _) && checkpoint.RunId==Session.RunId)
            {LeavingWithSave=true;if(!PersonalStats.Store.TryUpdate(checkpoint,RunOutcome.Suspended,out string error))SaveNotice=error;}
            else CloseRun();
        }
        private void OnApplicationQuit(){PreserveOrAbandon();}
        private void OnDestroy() { PreserveOrAbandon();Time.timeScale = 1; }

        private void Refresh()
        {
            if (lastRevision == Model.Revision) return;
            if (!IsTransitioning) boardView.Render(Model);
            // Freeze the timestamp and values when the run ends, before showing its form.
            if (Model.IsGameOver){ _ = FinishedRecord;CloseRun(); }
            linesText.text = Model.LinesCleared.ToString("00");
            scoreText.text = Model.Score.ToString();
            stateText.text = Model.IsGameOver ? "PARTIDA TERMINADA" : IsPaused ? "EN PAUSA" : IsTransitioning ? "CAMBIANDO CARA" : "EN JUEGO";
            gameOverNotice.SetActive(Model.IsGameOver);
            lastRevision = Model.Revision;
        }
    }
}
