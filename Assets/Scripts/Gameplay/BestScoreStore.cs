using UnityEngine;

namespace Cubotris.Gameplay
{
    public static class BestScoreStore
    {
        public const string Key = "Cubotris.BestScore";
        public static int Best => Mathf.Max(0, PlayerPrefs.GetInt(Key, 0));

        public static void Record(int score)
        {
            if (score <= Best) return;
            PlayerPrefs.SetInt(Key, score);
            // Persist each improvement, including if the application closes mid-game.
            PlayerPrefs.Save();
        }
    }
}
