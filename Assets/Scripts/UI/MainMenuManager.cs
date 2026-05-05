using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scenes")]
    public int gameSceneIndex = 1;

    public void Start()
    {
        AudioManager.Instance.PlaySong("Song_MainMenu");
    }
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
    public void PlaySoundClick()
    {
        AudioManager.Instance.PlayEffect("FX_UIClick");
    }
}
