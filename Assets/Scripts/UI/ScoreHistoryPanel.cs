using System;
using Cubotris.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Cubotris.UI
{
    public sealed class ScoreHistoryPanel : MonoBehaviour
    {
        [SerializeField] private MainMenuController menu;
        [SerializeField] private ScoreHistoryRow[] rows;
        [SerializeField] private Text emptyText, pageText, detailText, statusText, historicalText, confirmationText;
        [SerializeField] private Button previousButton, nextButton, deleteButton, clearButton, backButton, confirmButton, cancelButton;
        [SerializeField] private CanvasGroup content;
        [SerializeField] private GameObject confirmation;
        private int page, actionFrame = -1;
        private string selectedId, deletingId;
        private bool deletingAll;
        private ScoreHistoryStore Store => BestScoreStore.History;

        private void OnEnable()
        { page = 0; selectedId = null; statusText.text = Store.Notice ?? ""; SetConfirmation(false); Refresh(); FocusFirst(); }
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { if (confirmation.activeSelf) Cancel(); else Back(); }
        }
        public void Refresh()
        {
            var records = Store.Records;
            int pages = Math.Max(1, (records.Count + rows.Length - 1) / rows.Length);
            page = Mathf.Clamp(page, 0, pages - 1);
            pageText.text = $"PÁGINA {page + 1} / {pages} · {records.Count} REGISTROS";
            historicalText.text = Store.HistoricalUnattributedScore > 0
                ? $"Marca histórica sin nombre: {Store.HistoricalUnattributedScore} · No forma parte de esta clasificación" : "";
            emptyText.gameObject.SetActive(records.Count == 0);
            for (int i = 0; i < rows.Length; i++)
            {
                int index = page * rows.Length + i;
                rows[i].gameObject.SetActive(index < records.Count);
                if (index < records.Count) rows[i].Bind(this, records[index], index + 1);
            }
            previousButton.interactable = page > 0; nextButton.interactable = page + 1 < pages;
            clearButton.interactable = records.Count > 0;
            selectedId = null; deleteButton.interactable = false;
            detailText.text = "Selecciona un registro para ver su fecha o eliminarlo.";
            // Explicit navigation stays within the visible page and its actions.
            var navigation = new System.Collections.Generic.List<Selectable>();
            foreach (var row in rows) if (row.gameObject.activeSelf) navigation.Add(row.GetComponent<Button>());
            if (previousButton.interactable) navigation.Add(previousButton);
            if (nextButton.interactable) navigation.Add(nextButton);
            // Keep the delete action in the chain; it becomes interactable when a row gains focus.
            if (records.Count > 0) navigation.Add(deleteButton);
            if (clearButton.interactable) navigation.Add(clearButton);
            navigation.Add(backButton);
            for (int i = 0; i < navigation.Count; i++)
                navigation[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = navigation[(i + navigation.Count - 1) % navigation.Count], selectOnDown = navigation[(i + 1) % navigation.Count],
                    selectOnLeft = navigation[(i + navigation.Count - 1) % navigation.Count], selectOnRight = navigation[(i + 1) % navigation.Count] };
        }
        public void SelectRecord(ScoreRecord record)
        {
            if (confirmation.activeSelf) return;
            selectedId = record.id; deleteButton.interactable = true;
            string date = DateTimeOffset.Parse(record.finishedAtUtc).ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            detailText.text = $"{record.playerName} · {date} · {record.score} puntos · {GameHudView.FormatTime(record.survivalSeconds)}";
        }
        public void Previous() { if (!Allowed() || confirmation.activeSelf || page == 0) return; page--; Refresh(); FocusFirst(); }
        public void Next() { if (!Allowed() || confirmation.activeSelf || !nextButton.interactable) return; page++; Refresh(); FocusFirst(); }
        private void FocusFirst()
        {
            if (rows[0].gameObject.activeSelf) { rows[0].Choose(); Select(rows[0].GetComponent<Button>()); }
            else Select(backButton);
        }
        public void DeleteSelected()
        { if (selectedId == null || !Allowed() || confirmation.activeSelf) return; deletingId = selectedId; deletingAll = false; Ask("¿Seguro que quieres eliminar este récord?"); }
        public void ClearAll()
        { if (!Allowed() || confirmation.activeSelf || Store.Records.Count == 0) return; deletingAll = true; Ask("¿Seguro que quieres borrar TODOS los récords?\nEsta acción no se puede deshacer."); }
        private void Ask(string message)
        { confirmationText.text = message; SetConfirmation(true); Select(cancelButton); }
        public void Confirm()
        {
            if (!confirmation.activeSelf || !Allowed()) return;
            string error;
            bool success = deletingAll ? Store.TryClear(out error) : Store.TryDelete(deletingId, out error);
            if (!success) { confirmationText.text = error; return; }
            SetConfirmation(false); statusText.text = deletingAll ? "Historial eliminado." : "Récord eliminado.";
            Refresh(); menu.RefreshBest(); FocusFirst();
        }
        public void Cancel()
        { if (!confirmation.activeSelf || !Allowed()) return; SetConfirmation(false); Select(deleteButton.interactable ? deleteButton : backButton); }
        public void Back()
        { if (!Allowed() || confirmation.activeSelf) return; menu.CloseRecords(); }
        private bool Allowed()
        { if (!isActiveAndEnabled || actionFrame == Time.frameCount) return false; actionFrame = Time.frameCount; return true; }
        private void SetConfirmation(bool open)
        { confirmation.SetActive(open); content.interactable = content.blocksRaycasts = !open; }
        private static void Select(Selectable item)
        { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(item.gameObject); }
    }
}
