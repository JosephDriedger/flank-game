using UnityEngine;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioSource sfxSource;

    // Dedicated source for pitch-varied one-shots.  Using a separate AudioSource
    // means we can set an arbitrary pitch per call without resetting it afterwards
    // (resetting in the same frame would snap back before the audio engine renders).
    [SerializeField] private AudioSource sfxOneShotSource;

    private float musicVolume = 1.0f;
    private float sfxVolume   = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSavedVolumes();
        ApplyVolumes();
    }

    /// <summary>
    /// Reads persisted volume values written by SettingsPanelController.
    /// SaveSystem.Instance may not be ready yet in Awake, so we read via
    /// PlayerPrefs.GetString directly — the same underlying storage SaveSystem
    /// uses — and fall back to 1.0 if no value has been saved yet.
    /// </summary>
    private void LoadSavedVolumes()
    {
        musicVolume = ParseFloat(PlayerPrefs.GetString(SettingsKeys.MusicVolume, string.Empty), 1f);
        sfxVolume   = ParseFloat(PlayerPrefs.GetString(SettingsKeys.SFXVolume,   string.Empty), 1f);
    }

    private static float ParseFloat(string raw, float fallback)
    {
        return !string.IsNullOrEmpty(raw) && float.TryParse(raw, out float v) ? v : fallback;
    }

    private void ApplyVolumes()
    {
        if (musicSource != null)
            musicSource.volume = musicVolume;

        if (sfxSource != null)
            sfxSource.volume = sfxVolume;

        if (sfxOneShotSource != null)
            sfxOneShotSource.volume = sfxVolume;
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);

        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);

        if (sfxSource != null)
            sfxSource.volume = sfxVolume;

        if (sfxOneShotSource != null)
            sfxOneShotSource.volume = sfxVolume;
    }

    /// <summary>
    /// Plays a one-shot SFX at normal (pitch = 1) through sfxSource.
    /// Volume is controlled by sfxSource.volume — do NOT pass sfxVolume as
    /// the volumeScale or the volume will be squared at non-max settings.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip, 1f);
    }

    /// <summary>
    /// Plays a one-shot SFX with a randomised pitch through the dedicated
    /// sfxOneShotSource.  A separate AudioSource is used so the pitch can be
    /// set per-call without being reset in the same frame (which would cancel
    /// the pitch change before the audio engine renders the first sample).
    /// Falls back to sfxSource if sfxOneShotSource is not assigned.
    /// </summary>
    public void PlaySFXRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        AudioSource source = sfxOneShotSource != null ? sfxOneShotSource : sfxSource;

        if (source == null || clip == null)
            return;

        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clip, 1f);
    }
}
