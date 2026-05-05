using System;
using UnityEngine;
using System.Collections;
public enum GameState
{
    Playing,
    Winner,
    Paused,
    Menu,
    Death,
    Dying
}
public class GameManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }
    // ─── Inspector ────────────────────────────────────────────────────────────
    [SerializeField] private GameObject _pauseMenuPanel;
    [SerializeField] private GameObject _DeathUI;
    [SerializeField] private GameObject _WinningUI;
    [SerializeField] public GameObject[] playerPrefabs;
    [SerializeField] public int _selectedPlayerIndex = 0; // 0 = Male, 1 = Female

    [Header("Death Prompt")]
    [Tooltip("'Press Enter to Continue' text — shown after the delay, then hidden when DeathUI opens.")]
    [SerializeField] private GameObject _restartPromptText;
    [Tooltip("Seconds after death before the prompt appears.")]
    [SerializeField] private float _deathDelay = 3f;

    // ─── Private State ────────────────────────────────────────────────────────
    [SerializeField] private GameState _currentState = GameState.Playing;
    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // ── Singleton enforcement ─────────────────────────────────────────────
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    public void PauseGame()
    {
        SetState(GameState.Paused);
        Time.timeScale = 0f;
    }
    public void ResumeGame()
    {
        SetState(GameState.Playing);
        Time.timeScale = 1f;
    }
    /// <summary>
    /// Unified death entry point — called for every cause of death (spike, predator, etc.).
    /// Waits _deathDelay seconds, then shows the restart prompt.
    /// Pressing Jump from that prompt opens the Death UI.
    /// </summary>
    public void TriggerDeath()
    {
        StopAllCoroutines();
        SetState(GameState.Dying);
        StartCoroutine(DyingSequence());
    }

    private IEnumerator DyingSequence()
    {
        yield return new WaitForSeconds(_deathDelay);
        SetState(GameState.Death);
        if (_restartPromptText != null)
            _restartPromptText.SetActive(true);
    }

    public void OpenDeathUI()
    {
        if (_restartPromptText != null)
            _restartPromptText.SetActive(false);
        if (_DeathUI != null)
            _DeathUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void TriggerWinner()
    {
        HandlePlayerWinner();
    }
    private void HandlePlayerWinner()
    {
        SetState(GameState.Winner);
        _WinningUI.SetActive(true);
        Time.timeScale = 0f;
    }
    private void SetState(GameState newState)
    {
        _currentState = newState;
        Debug.Log($"[GameManager] State → {newState}");
    }
    /// <summary>
    /// Immediately ends the game (skips any remaining dying timer).
    /// Called by ContinuePrompt when the player presses Interact during the Dying state.
    /// </summary>
    public void SetGameOver()
    {
        StopAllCoroutines();
        SetState(GameState.Death);
        if (_DeathUI != null) _DeathUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SetState(GameState.Playing);
        ArtSceneManager.Instance.RestartLevel();
    }

    public void PlayClickSound()
    {
        AudioManager.Instance.PlayEffect("ButtonKey");
    }
    public void LoadLevel(int levelIndex)
    {
        Time.timeScale = 1f;
        ArtSceneManager.Instance.LoadSceneByNumber(levelIndex);
    }
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        ArtSceneManager.Instance.LoadMainMenu();
    }
    public void TogglePlayerPrefab(int index)
    {
        _selectedPlayerIndex = index;
    }
    public GameState CurrentState => _currentState;
}
