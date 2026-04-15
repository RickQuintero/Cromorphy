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

    [Header("Predator Death")]
    [Tooltip("UI element (TextMesh/TMP) showing 'Press Jump to Restart'. Shown after predator kill.")]
    [SerializeField] private GameObject _restartPromptText;

    // ─── Private State ────────────────────────────────────────────────────────
    [SerializeField] private GameState _currentState = GameState.Playing;
    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Update()
    {
        // Listen for restart input only when the predator-death prompt is visible.
        // Using Input.GetButtonDown("Jump") keeps this independent of InputReader.
        if (_currentState != GameState.Death) return;
        if (_restartPromptText == null || !_restartPromptText.activeSelf) return;

        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
            RestartGame();
    }

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
    public void TriggerDeath()
    {
        SetState(GameState.Dying);
        StartCoroutine(DyingSequence());
    }

    /// <summary>
    /// Called by PlayerTriggerStates when a predator kills the player.
    ///
    /// Sequence:
    ///   0 s  — disable player control (already done by PlayerTriggerStates)
    ///   6 s  — show "Press Jump to Restart" prompt
    ///   Jump — RestartGame()
    ///
    /// The prompt is hidden again by RestartGame() via scene reload.
    /// </summary>
    public void TriggerDyingByPredator()
    {
        StopAllCoroutines();    // cancel any prior dying sequence
        SetState(GameState.Dying);
        StartCoroutine(PredatorDyingSequence());
    }

    private IEnumerator PredatorDyingSequence()
    {
        // Give the snake time to carry the player back to the nest
        yield return new WaitForSeconds(6f);

        SetState(GameState.Death);

        if (_restartPromptText != null)
            _restartPromptText.SetActive(true);
        // Update() will now listen for Jump input to restart
    }

    public void TriggerWinner()
    {
        HandlePlayerWinner();
    }
    public IEnumerator DyingSequence()
    {
        //AudioManager.Instance.ChangeSong("DeathSong");
        yield return new WaitForSeconds(3f);
        SetState(GameState.Death);
        HandlePlayerDeath();

    }

    private void HandlePlayerDeath()
    {
        StopAllCoroutines();
        SetState(GameState.Death);
        _DeathUI.SetActive(true);
        Time.timeScale = 0f;
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
