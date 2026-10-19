using UnityEngine;
using UnityEngine.UI;

namespace Cubotris.Gameplay
{
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private Text linesText;
        [SerializeField] private Text stateText;
        [SerializeField] private GameObject gameOverNotice;
        [SerializeField] private FallSettings fallSettings = new FallSettings();

        public TetrisGame Model { get; private set; }
        public bool AcceptsInput => isActiveAndEnabled && Model != null && !Model.IsGameOver;
        public bool SoftDropHeld { get; set; }
        private int lastRevision = -1;
        private bool skipGravityOnce;

        private void Start()
        {
            Model = new TetrisGame(settings: fallSettings);
            Refresh();
        }

        private void Update()
        {
            if (!skipGravityOnce) Model.Tick(Time.deltaTime, SoftDropHeld);
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

        private void Refresh()
        {
            if (lastRevision == Model.Revision) return;
            boardView.Render(Model);
            linesText.text = Model.LinesCleared.ToString("00");
            stateText.text = Model.IsGameOver ? "PARTIDA TERMINADA" : "EN JUEGO";
            gameOverNotice.SetActive(Model.IsGameOver);
            lastRevision = Model.Revision;
        }
    }
}
