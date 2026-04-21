using UnityEngine;

public sealed class SaveSystem : MonoBehaviour
{
    public static SaveSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public string LoadString(string key, string fallback = "")
    {
        return PlayerPrefs.GetString(key, fallback);
    }

    public void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }

    public void ClearAll()
    {
        PlayerPrefs.DeleteAll();
    }
}
