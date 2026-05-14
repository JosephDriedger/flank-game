using UnityEngine;

public sealed class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioSource sfxSource;

    private float musicVolume = 1.0f;
    private float sfxVolume = 1.0f;

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
        {
            musicSource.volume = musicVolume;
        }

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);

        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void PlaySFXRandomPitch(AudioClip clip, float minPitch = 0.9f, float maxPitch = 1.1f)
    {
        if (sfxSource == null || clip == null)
        {
            return;
        }

        float originalPitch = sfxSource.pitch;
        sfxSource.pitch = Random.Range(minPitch, maxPitch);

        sfxSource.PlayOneShot(clip, sfxVolume);

        sfxSource.pitch = originalPitch;
    }
}
