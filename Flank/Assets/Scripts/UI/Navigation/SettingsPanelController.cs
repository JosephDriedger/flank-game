using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Settings panel: music volume, SFX volume, and fullscreen toggle.
/// All values are persisted immediately via SaveSystem (PlayerPrefs) and applied
/// live so the player hears / sees the change without closing the panel.
/// </summary>
public sealed class SettingsPanelController : MonoBehaviour
{
    private const float DefaultMusicVolume = 1f;
    private const float DefaultSFXVolume   = 1f;
    private const bool  DefaultFullscreen  = true;

    // Target windowed resolution — 16:9, matches ProjectSettings defaults.
    private const int WindowedWidth  = 1920;
    private const int WindowedHeight = 1080;

    [Header("Controls")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button resetDefaultsButton;

    // Guard flag: prevents listener callbacks from firing while we set
    // slider/toggle values from code, which would cause a double-save.
    private bool isApplying;

    // ----------------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------------

    private void OnEnable()
    {
        LoadAndApply();

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(HandleSFXVolumeChanged);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(HandleFullscreenChanged);
        if (resetDefaultsButton != null)
            resetDefaultsButton.onClick.AddListener(HandleResetDefaults);
    }

    private void OnDisable()
    {
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(HandleSFXVolumeChanged);
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(HandleFullscreenChanged);
        if (resetDefaultsButton != null)
            resetDefaultsButton.onClick.RemoveListener(HandleResetDefaults);
    }

    // ----------------------------------------------------------------
    // Load & apply all settings
    // ----------------------------------------------------------------

    private void LoadAndApply()
    {
        isApplying = true;

        float music     = LoadFloat(SettingsKeys.MusicVolume, DefaultMusicVolume);
        float sfx       = LoadFloat(SettingsKeys.SFXVolume,   DefaultSFXVolume);
        bool  fullscreen = LoadBool(SettingsKeys.Fullscreen,  DefaultFullscreen);

        if (musicVolumeSlider != null)  musicVolumeSlider.value = music;
        if (sfxVolumeSlider != null)    sfxVolumeSlider.value   = sfx;
        if (fullscreenToggle != null)   fullscreenToggle.isOn   = fullscreen;

        ApplyAudio(music, sfx);
        ApplyFullscreen(fullscreen);

        isApplying = false;
    }

    // ----------------------------------------------------------------
    // Listeners
    // ----------------------------------------------------------------

    private void HandleMusicVolumeChanged(float value)
    {
        if (isApplying) return;

        SaveFloat(SettingsKeys.MusicVolume, value);

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    private void HandleSFXVolumeChanged(float value)
    {
        if (isApplying) return;

        SaveFloat(SettingsKeys.SFXVolume, value);

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
    }

    private void HandleFullscreenChanged(bool value)
    {
        if (isApplying) return;

        SaveBool(SettingsKeys.Fullscreen, value);
        ApplyFullscreen(value);
    }

    private void HandleResetDefaults()
    {
        SaveFloat(SettingsKeys.MusicVolume, DefaultMusicVolume);
        SaveFloat(SettingsKeys.SFXVolume,   DefaultSFXVolume);
        SaveBool(SettingsKeys.Fullscreen,   DefaultFullscreen);
        LoadAndApply();
    }

    // ----------------------------------------------------------------
    // Apply helpers
    // ----------------------------------------------------------------

    private static void ApplyAudio(float music, float sfx)
    {
        if (AudioManager.Instance == null) return;
        AudioManager.Instance.SetMusicVolume(music);
        AudioManager.Instance.SetSFXVolume(sfx);
    }

    private static void ApplyFullscreen(bool value)
    {
        if (value)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        else
        {
            // Always return to an explicit 16:9 windowed resolution so the
            // window doesn't inherit the native ultrawide (or any other) size.
            Screen.SetResolution(WindowedWidth, WindowedHeight, FullScreenMode.Windowed);
        }
    }

    // ----------------------------------------------------------------
    // Persistence helpers
    //
    // Floats and bools are stored as strings via SaveSystem so they share
    // the same PlayerPrefs.SetString / GetString channel as the rest of
    // the app.  AudioManager.Awake() also reads via GetString so the
    // format must be consistent.
    // ----------------------------------------------------------------

    private static float LoadFloat(string key, float fallback)
    {
        if (SaveSystem.Instance != null)
        {
            string raw = SaveSystem.Instance.LoadString(key, fallback.ToString("R"));
            return float.TryParse(raw, out float v) ? v : fallback;
        }

        // SaveSystem stores strings, so read the string channel even without it.
        string stored = PlayerPrefs.GetString(key, string.Empty);
        return !string.IsNullOrEmpty(stored) && float.TryParse(stored, out float pv)
            ? pv
            : fallback;
    }

    private static bool LoadBool(string key, bool fallback)
    {
        if (SaveSystem.Instance != null)
        {
            string raw = SaveSystem.Instance.LoadString(key, fallback ? "1" : "0");
            return raw == "1";
        }

        string stored = PlayerPrefs.GetString(key, string.Empty);
        if (string.IsNullOrEmpty(stored)) return fallback;
        return stored == "1";
    }

    private static void SaveFloat(string key, float value)
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveString(key, value.ToString("R"));
            return;
        }

        PlayerPrefs.SetString(key, value.ToString("R"));
        PlayerPrefs.Save();
    }

    private static void SaveBool(string key, bool value)
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveString(key, value ? "1" : "0");
            return;
        }

        PlayerPrefs.SetString(key, value ? "1" : "0");
        PlayerPrefs.Save();
    }
}
