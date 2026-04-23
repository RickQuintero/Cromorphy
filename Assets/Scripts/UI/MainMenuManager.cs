using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scenes")]
    public int gameSceneIndex = 1;

    [Header("Panels")]
    public GameObject loadGamePanel;

    public void PlayGame()
    {
        ArtSceneManager.Instance.LoadSceneByNumber(gameSceneIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    public void OpenLoadGamePanel()
    {
        if (loadGamePanel != null)
            loadGamePanel.SetActive(true);
    }

    public void CloseLoadGamePanel()
    {
        if (loadGamePanel != null)
            loadGamePanel.SetActive(false);
    }
}
