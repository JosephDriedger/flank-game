using UnityEngine;

public sealed class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }

    private const string SettingsPrefsKey = "GameSettingsManager.CurrentJson";

    public GameSettings Current { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadOrCreateDefault();
    }

    public void Set(GameSettings settings)
    {
        if (settings == null)
        {
            settings = GameSettings.CreateDefault();
        }

        Current = settings;
        Save();
    }

    public void Clear()
    {
        Current = GameSettings.CreateDefault();
        PlayerPrefs.DeleteKey(SettingsPrefsKey);
        PlayerPrefs.Save();
    }

    private void LoadOrCreateDefault()
    {
        if (!PlayerPrefs.HasKey(SettingsPrefsKey))
        {
            Current = GameSettings.CreateDefault();
            return;
        }

        string json = PlayerPrefs.GetString(SettingsPrefsKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            Current = GameSettings.CreateDefault();
            return;
        }

        try
        {
            Current = JsonUtility.FromJson<GameSettings>(json);
        }
        catch
        {
            Current = GameSettings.CreateDefault();
        }

        if (Current == null)
        {
            Current = GameSettings.CreateDefault();
        }
    }

    private void Save()
    {
        if (Current == null)
        {
            return;
        }

        string json = JsonUtility.ToJson(Current);
        PlayerPrefs.SetString(SettingsPrefsKey, json);
        PlayerPrefs.Save();
    }

    public string ExportJson()
    {
        if (Current == null)
        {
            return string.Empty;
        }

        return UnityEngine.JsonUtility.ToJson(Current);
    }

    public void ImportJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            GameSettings loaded = UnityEngine.JsonUtility.FromJson<GameSettings>(json);
            if (loaded == null)
            {
                return;
            }

            Current = loaded;
            Save();
        }
        catch
        {
            // Keep current if invalid.
        }
    }

}
