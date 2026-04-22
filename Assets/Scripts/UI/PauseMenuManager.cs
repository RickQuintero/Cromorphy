using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Utilizado para la consulta nativa (directa) del hardware.

public class PauseMenuManager : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("El objeto Panel (hijo del Canvas) que contiene la interfaz de tu pausa.")]
    public GameObject pausePanel;

    [Tooltip("Botón de guardar partida. Se activa solo cuando hay un checkpoint alcanzado.")]
    public GameObject saveButton;

    [Tooltip("Panel de slots de guardado (hijo del pausePanel).")]
    public GameObject saveGamePanel;

    [Header("Escenas")]
    [Tooltip("El nombre exacto de la escena de menú principal.")]
    public string mainMenuSceneName = "MainMenu";

    private bool _isPaused = false;

    private void Start()
    {
        // Al arrancar o cargar la zona, aseguramos que el juego no inicie congelado.
        Time.timeScale = 1f;
        
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (saveGamePanel != null)
            saveGamePanel.SetActive(false);

        // El botón de guardar empieza oculto; se muestra al llegar a un checkpoint
        if (saveButton != null)
            saveButton.SetActive(false);

        // Suscribirse al evento de checkpoint
        // Usamos Update como fallback por si CheckpointManager aún no existe en Start()
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.OnCheckpointReached += OnCheckpointReached;

        // Si ya había un checkpoint activo al cargar (partida cargada), mostrar el botón
        RefreshSaveButton();
    }

    private void OnDestroy()
    {
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.OnCheckpointReached -= OnCheckpointReached;
    }

    private void OnCheckpointReached()
    {
        RefreshSaveButton();
    }

    private void RefreshSaveButton()
    {
        if (saveButton == null) return;
        bool canSave = CheckpointManager.Instance != null && CheckpointManager.Instance.CanSave;
        saveButton.SetActive(canSave);
    }

    private bool _subscribedToCheckpoint = false;

    private void Update()
    {
        // Suscripción diferida por si CheckpointManager se inicializa después del PauseMenuManager
        if (!_subscribedToCheckpoint && CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.OnCheckpointReached += OnCheckpointReached;
            _subscribedToCheckpoint = true;
            RefreshSaveButton(); // por si ya había checkpoint activo
        }

        // Consultamos la tecla Esc sin la necesidad imperativa de un asset InputAction intermedio 
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isPaused)
                ContinueGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (pausePanel == null) return;

        _isPaused = true;
        pausePanel.SetActive(true);

        // Actualizar visibilidad del botón guardar según checkpoint activo
        if (saveButton != null)
            saveButton.SetActive(CheckpointManager.Instance != null && CheckpointManager.Instance.CanSave);
        
        // Literalmente paraliza el motor de físicas de Unity y los renders basados en tiempo.
        Time.timeScale = 0f;
    }

    public void ContinueGame()
    {
        _isPaused = false;

        if (saveGamePanel != null)
            saveGamePanel.SetActive(false);
        
        if (pausePanel != null)
            pausePanel.SetActive(false);

        // Restaura la velocidad de las físicas
        Time.timeScale = 1f;
    }

    /// <summary>Abre el panel de slots de guardado desde el botón del menú de pausa.</summary>
    public void OpenSavePanel()
    {
        if (saveGamePanel != null)
            saveGamePanel.SetActive(true);
    }

    /// <summary>Cierra el panel de slots sin cerrar la pausa.</summary>
    public void CloseSavePanel()
    {
        if (saveGamePanel != null)
            saveGamePanel.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        /* 
           ¡IMPORTANTE!
           Si no regresamos la escala del tiempo a 1 antes de saltar de escena, 
           tu Menú Principal cargará con el tiempo paralizado y dará fallos gráficos.
        */
        Time.timeScale = 1f;
        
        Debug.Log("Volviendo desde Pausa al Main Menu...");
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
