using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadModeSelect()
    {
        SceneManager.LoadScene("ModeSelect");
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void LoadPostGame()
    {
        SceneManager.LoadScene("PostGame");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
