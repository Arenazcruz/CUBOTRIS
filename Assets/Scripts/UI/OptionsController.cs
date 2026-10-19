using UnityEngine;
using UnityEngine.UI;

namespace Cubotris.UI
{
    [DisallowMultipleComponent]
    public sealed class OptionsController : MonoBehaviour
    {
        public const string MusicVolumeKey = "Cubotris.Audio.MusicVolumePercent";
        public const string EffectsVolumeKey = "Cubotris.Audio.EffectsVolumePercent";
        public const int DefaultMusicVolume = 70;
        public const int DefaultEffectsVolume = 80;

        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider effectsSlider;
        [SerializeField] private Text musicValueText;
        [SerializeField] private Text effectsValueText;

        // Normalized values for a future audio system, also available while this panel is closed.
        public static float MusicVolume => ReadPercentage(MusicVolumeKey, DefaultMusicVolume) / 100f;
        public static float EffectsVolume => ReadPercentage(EffectsVolumeKey, DefaultEffectsVolume) / 100f;

        private void OnEnable()
        {
            // Loading must not invoke slider callbacks or replace existing preferences with defaults.
            UpdateControl(musicSlider, musicValueText, ReadPercentage(MusicVolumeKey, DefaultMusicVolume));
            UpdateControl(effectsSlider, effectsValueText, ReadPercentage(EffectsVolumeKey, DefaultEffectsVolume));
        }

        public void SetMusicVolume(float value)
        {
            int percentage = Mathf.RoundToInt(Mathf.Clamp(value, 0f, 100f));
            UpdateControl(musicSlider, musicValueText, percentage);
            SavePercentage(MusicVolumeKey, percentage);
        }

        public void SetEffectsVolume(float value)
        {
            int percentage = Mathf.RoundToInt(Mathf.Clamp(value, 0f, 100f));
            UpdateControl(effectsSlider, effectsValueText, percentage);
            SavePercentage(EffectsVolumeKey, percentage);
        }

        private static int ReadPercentage(string key, int defaultValue)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(key, defaultValue), 0, 100);
        }

        private static void UpdateControl(Slider slider, Text label, int percentage)
        {
            slider.SetValueWithoutNotify(percentage);
            label.text = percentage + " %";
        }

        private static void SavePercentage(string key, int percentage)
        {
            PlayerPrefs.SetInt(key, percentage);
            PlayerPrefs.Save();
        }
    }
}
