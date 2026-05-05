using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel that contains the pause UI.")]
    public GameObject pausePanel;

    [Tooltip("Save button. Shown only when a checkpoint has been reached.")]
    public GameObject saveButton;

    [Tooltip("Save slots panel (child of pausePanel).")]
    public GameObject saveGamePanel;

    [Header("Scenes")]
    [Tooltip("Exact name of the main menu scene.")]
    public string mainMenuSceneName = "MainMenu";

    private bool _isPaused;
    private bool _subscribedToCheckpoint;

    private void Start()
    {
        Time.timeScale = 1f;

        if (pausePanel    != null) pausePanel.SetActive(false);
        if (saveGamePanel != null) saveGamePanel.SetActive(false);
        if (saveButton    != null) saveButton.SetActive(false);

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.OnCheckpointReached += RefreshSaveButton;
            _subscribedToCheckpoint = true;
        }

        RefreshSaveButton();
    }

    private void OnDestroy()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.OnCheckpointReached -= RefreshSaveButton;
    }

    private void Update()
    {
        // Deferred subscription in case CheckpointManager initialises after this component.
        if (!_subscribedToCheckpoint && CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.OnCheckpointReached += RefreshSaveButton;
            _subscribedToCheckpoint = true;
            RefreshSaveButton();
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isPaused) ContinueGame();
            else           PauseGame();
        }
    }

    private void RefreshSaveButton()
    {
        if (saveButton == null) return;
        saveButton.SetActive(CheckpointManager.Instance != null && CheckpointManager.Instance.CanSave);
    }

    public void PauseGame()
    {
        if (pausePanel == null) return;

        _isPaused = true;
        pausePanel.SetActive(true);
        RefreshSaveButton();
        Time.timeScale = 0f;
    }

    public void ContinueGame()
    {
        _isPaused = false;
        if (saveGamePanel != null) saveGamePanel.SetActive(false);
        if (pausePanel    != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void OpenSavePanel()
    {
        if (saveGamePanel != null) saveGamePanel.SetActive(true);
    }

    public void CloseSavePanel()
    {
        if (saveGamePanel != null) saveGamePanel.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        // Must reset timescale before switching scenes or the menu loads frozen.
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
