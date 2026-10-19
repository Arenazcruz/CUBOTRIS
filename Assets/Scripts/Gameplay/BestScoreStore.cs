using UnityEngine;

namespace Cubotris.Gameplay
{
    public static class BestScoreStore
    {
        public const string Key = "Cubotris.BestScore";
        private static ScoreHistoryStore history;
        public static ScoreHistoryStore History => history ??= new ScoreHistoryStore(
            System.IO.Path.Combine(Application.persistentDataPath, "records.json"), PlayerPrefs.GetInt(Key, 0));
        public static int Best => History.Best?.score ?? 0;
        public static string PlayerName => History.Best?.playerName ?? "Sin récords";
    }
}
