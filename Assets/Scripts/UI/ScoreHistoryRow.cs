using Cubotris.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Cubotris.UI
{
    public sealed class ScoreHistoryRow : MonoBehaviour, ISelectHandler
    {
        [SerializeField] private Text positionText, nameText, scoreText, timeText;
        private ScoreHistoryPanel owner;
        private ScoreRecord record;
        public void Bind(ScoreHistoryPanel panel, ScoreRecord value, int position)
        {
            owner = panel; record = value;
            positionText.text = position.ToString("00"); nameText.text = value.playerName;
            scoreText.text = value.score.ToString(); timeText.text = GameHudView.FormatTime(value.survivalSeconds);
        }
        public void OnSelect(BaseEventData data) => Choose();
        public void Choose() { if (record != null) owner.SelectRecord(record); }
    }
}
