using UnityEngine.UI;
namespace Cubotris.Gameplay
{
    public sealed class StatisticsPanel : SettingsOverlay
    {
        public Text filter,totals,hardest,notice;
        public Text[] faces;
        private bool practice;
        protected override void OnEnable(){base.OnEnable();practice=false;Refresh();}
        public void TogglePractice(){practice=!practice;Refresh();}
        private void Refresh()
        {
            var s=PersonalStats.Store.Summarize(practice);
            filter.text=(practice?"PRÁCTICA":"COMPETICIÓN")+"  >";
            totals.text=$"PARTIDAS JUGADAS: {s.Played}     TERMINADAS: {s.Completed}\nABANDONADAS: {s.Abandoned}     EN CURSO / GUARDADAS: {s.Suspended}\n\nLÍNEAS: {s.Lines}     TIEMPO TOTAL: {GameHudView.FormatTime(s.Seconds)}\nDURACIÓN MEDIA AL PERDER: {GameHudView.FormatTime(s.AverageCompletedSeconds)}\nMEJOR RACHA: {s.MaxCombo}";
            hardest.text="MÁS GAME OVER: "+s.HardestFaces;
            for(int i=0;i<4;i++)faces[i].text=$"CARA {i+1}     {s.FaceLines[i]} LÍNEAS     {GameHudView.FormatTime(s.FaceSeconds[i])}     {s.FaceLosses[i]} GAME OVER";
            notice.text=PersonalStats.Store.Notice??"Todos los modos de dificultad. Borrar récords no borra estas estadísticas.";
        }
    }
}
