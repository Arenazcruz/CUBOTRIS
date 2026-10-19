using UnityEngine;
using UnityEngine.UI;

namespace Cubotris.Gameplay
{
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private Text linesText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text scoreText;
        [SerializeField] private GameObject gameOverNotice;
        [SerializeField] private FallSettings fallSettings = new FallSettings();

        public TetrisGame Model { get; private set; }
        public bool IsPaused { get; private set; }
        public int InputVersion { get; private set; }
        public bool AcceptsInput => isActiveAndEnabled && Model != null && !Model.IsGameOver && !IsPaused && Time.frameCount != transitionFrame;
        public bool SoftDropHeld { get; set; }
        private int lastRevision = -1;
        private bool skipGravityOnce;
        private int transitionFrame = -1;

        private void Start()
        {
            Model = new TetrisGame(settings: fallSettings);
            Refresh();
        }

        private void Update()
        {
            if (AcceptsInput && !skipGravityOnce) Model.Tick(Time.deltaTime, SoftDropHeld);
            skipGravityOnce = false;
            Refresh();
        }

        public void Move(int direction) { if (AcceptsInput) Model.Move(direction); }
        public void Rotate() { if (AcceptsInput) Model.RotateClockwise(); }
        public void HardDrop()
        {
            if (!AcceptsInput) return;
            Model.HardDrop();
            skipGravityOnce = true;
        }

        public void SetPaused(bool paused)
        {
            if (Model == null || Model.IsGameOver || IsPaused == paused) return;
            SaveRecord();
            IsPaused = paused;
            Time.timeScale = paused ? 0 : 1;
            ResetInput();
            stateText.text = paused ? "EN PAUSA" : "EN JUEGO";
        }

        public void Restart()
        {
            SaveRecord();
            Model = new TetrisGame(settings: fallSettings);
            IsPaused = false;
            Time.timeScale = 1;
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
            boardView.Render(Model);
            SaveRecord();
            linesText.text = Model.LinesCleared.ToString("00");
            scoreText.text = Model.Score.ToString();
            stateText.text = Model.IsGameOver ? "PARTIDA TERMINADA" : IsPaused ? "EN PAUSA" : "EN JUEGO";
            gameOverNotice.SetActive(Model.IsGameOver);
            lastRevision = Model.Revision;
        }
    }
}
