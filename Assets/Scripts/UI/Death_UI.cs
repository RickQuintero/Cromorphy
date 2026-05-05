using UnityEngine;

public class Death_UI : MonoBehaviour
{
    public void RetryLevel()
    {
        Time.timeScale = 1f;

        int slot = SaveSystem.GetActiveSlot();
        if (slot >= 0 && SaveSystem.SlotExists(slot))
            PlayerPrefs.SetInt(SaveSystem.PENDING_LOAD_KEY, slot);

        ArtSceneManager.Instance.RestartLevel();
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        ArtSceneManager.Instance.LoadMainMenu();
    }
}
