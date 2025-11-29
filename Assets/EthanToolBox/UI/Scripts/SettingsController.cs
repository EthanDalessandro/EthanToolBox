using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EthanToolBox.UI.Scripts
{
    public class SettingsController : MonoBehaviour
    {
        [Header("UI References")]
        public Slider musicSlider;
        public Slider sfxSlider;
        public TMP_Dropdown qualityDropdown;

        private const string MUSIC_KEY = "MusicVolume";
        private const string SFX_KEY = "SfxVolume";
        private const string QUALITY_KEY = "QualitySetting";

        private void Start()
        {
            LoadSettings();
        }

        public void SetMusicVolume(float volume)
        {
            // In a real project, link this to AudioManager
            PlayerPrefs.SetFloat(MUSIC_KEY, volume);
            Debug.Log($"Music Volume: {volume}");
        }

        public void SetSfxVolume(float volume)
        {
            // In a real project, link this to AudioManager
            PlayerPrefs.SetFloat(SFX_KEY, volume);
            Debug.Log($"SFX Volume: {volume}");
        }

        public void SetQuality(int qualityIndex)
        {
            QualitySettings.SetQualityLevel(qualityIndex);
            PlayerPrefs.SetInt(QUALITY_KEY, qualityIndex);
            Debug.Log($"Quality Level: {qualityIndex}");
        }

        private void LoadSettings()
        {
            if (musicSlider != null)
                musicSlider.value = PlayerPrefs.GetFloat(MUSIC_KEY, 0.75f);

            if (sfxSlider != null)
                sfxSlider.value = PlayerPrefs.GetFloat(SFX_KEY, 0.75f);

            if (qualityDropdown != null)
            {
                int quality = PlayerPrefs.GetInt(QUALITY_KEY, 2); // Default to Medium/High usually
                qualityDropdown.value = quality;
                QualitySettings.SetQualityLevel(quality);
            }
        }
    }
}
