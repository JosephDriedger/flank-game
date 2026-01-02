using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneLoader : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string navigationSceneName = "NavigationScene";

    private bool isLoading;

    public void LoadGameScene()
    {
        LoadSingle(gameSceneName);
    }

    public void LoadNavigationScene()
    {
        LoadSingle(navigationSceneName);
    }

    public void LoadSingle(string sceneName, Action onComplete = null)
    {
        if (isLoading)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("SceneLoader.LoadSingle: sceneName is null/empty.");
            return;
        }

        StartCoroutine(LoadRoutine(sceneName, onComplete));
    }

    private IEnumerator LoadRoutine(string sceneName, Action onComplete)
    {
        isLoading = true;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone)
        {
            yield return null;
        }

        isLoading = false;
        onComplete?.Invoke();
    }
}
