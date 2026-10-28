using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies local audio/display preferences to scene-authored controls.</summary>
public sealed class SettingsController : MonoBehaviour
{
    const string VolumeKey = "Battleship.Volume";
    const string SoundKey = "Battleship.SoundEnabled";
    const string FullscreenKey = "Battleship.Fullscreen";
    const float DefaultVolume = 0.8f;

    [SerializeField] Slider volumeSlider;
    [SerializeField] TMP_Text volumeLabel;
    [SerializeField] Toggle soundToggle;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] Button resetButton;

    void OnEnable()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(SetVolume);
        if (soundToggle != null) soundToggle.onValueChanged.AddListener(SetSoundEnabled);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        if (resetButton != null) resetButton.onClick.AddListener(ResetSettings);
    }

    void Start()
    {
        float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, DefaultVolume));
        bool soundEnabled = PlayerPrefs.GetInt(SoundKey, 1) != 0;
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;

        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(volume);
        if (soundToggle != null) soundToggle.SetIsOnWithoutNotify(soundEnabled);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        AudioListener.volume = soundEnabled ? volume : 0f;
        Screen.fullScreen = fullscreen;
        UpdateVolumeLabel(volume);
    }

    void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, value);
        AudioListener.volume = soundToggle == null || soundToggle.isOn ? value : 0f;
        UpdateVolumeLabel(value);
        PlayerPrefs.Save();
    }

    void SetSoundEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(SoundKey, enabled ? 1 : 0);
        AudioListener.volume = enabled && volumeSlider != null ? volumeSlider.value : enabled ? DefaultVolume : 0f;
        PlayerPrefs.Save();
    }

    void SetFullscreen(bool enabled)
    {
        PlayerPrefs.SetInt(FullscreenKey, enabled ? 1 : 0);
        Screen.fullScreen = enabled;
        PlayerPrefs.Save();
    }

    void ResetSettings()
    {
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(DefaultVolume);
        if (soundToggle != null) soundToggle.SetIsOnWithoutNotify(true);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(false);
        PlayerPrefs.SetFloat(VolumeKey, DefaultVolume);
        PlayerPrefs.SetInt(SoundKey, 1);
        PlayerPrefs.SetInt(FullscreenKey, 0);
        AudioListener.volume = DefaultVolume;
        Screen.fullScreen = false;
        UpdateVolumeLabel(DefaultVolume);
        PlayerPrefs.Save();
    }

    void UpdateVolumeLabel(float value)
    {
        if (volumeLabel != null) volumeLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    void OnDisable()
    {
        if (volumeSlider != null) volumeSlider.onValueChanged.RemoveListener(SetVolume);
        if (soundToggle != null) soundToggle.onValueChanged.RemoveListener(SetSoundEnabled);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(SetFullscreen);
        if (resetButton != null) resetButton.onClick.RemoveListener(ResetSettings);
    }
}
