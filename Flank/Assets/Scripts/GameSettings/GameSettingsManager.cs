using UnityEngine;

public sealed class GameSettingsManager : MonoBehaviour
{
    private static GameSettingsManager _instance;

    public static GameSettingsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameSettingsManager");
                _instance = go.AddComponent<GameSettingsManager>();
            }

            return _instance;
        }
    }

    public GameSettings Current { get; private set; }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        if (Current == null)
        {
            Current = GameSettings.CreateDefault();
        }
    }

    public void Set(GameSettings settings)
    {
        Current = settings ?? GameSettings.CreateDefault();
    }
}
