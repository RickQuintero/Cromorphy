using UnityEngine;

/// <summary>
/// Singleton de escena que lleva el registro del checkpoint activo.
/// El PauseMenuManager lo consulta para saber si debe mostrar el botón de guardar.
/// </summary>
public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    // ── Estado ────────────────────────────────────────────────────────────────
    /// <summary>Índice del último checkpoint alcanzado. -1 = ninguno.</summary>
    public int LastCheckpointIndex { get; private set; } = -1;

    /// <summary>Posición del último checkpoint alcanzado.</summary>
    public Vector3 LastCheckpointPosition { get; private set; }

    /// <summary>True si el jugador está en un checkpoint y puede guardar.</summary>
    public bool CanSave => LastCheckpointIndex >= 0;

    // ── Evento ────────────────────────────────────────────────────────────────
    /// <summary>Se dispara cada vez que se registra un nuevo checkpoint.</summary>
    public event System.Action OnCheckpointReached;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Llamado por CheckpointZone cuando el jugador entra al trigger.</summary>
    public void RegisterCheckpoint(CheckpointZone zone)
    {
        if (zone.checkpointIndex <= LastCheckpointIndex) return; // no retroceder

        LastCheckpointIndex    = zone.checkpointIndex;
        LastCheckpointPosition = zone.transform.position;

        OnCheckpointReached?.Invoke();
        Debug.Log($"[CheckpointManager] Checkpoint activo → {LastCheckpointIndex}");
    }

    /// <summary>
    /// Restaura el estado desde un SaveData al cargar partida.
    /// Llama esto antes de mover al jugador a la posición guardada.
    /// </summary>
    public void RestoreFromSave(SaveSystem.SaveData data)
    {
        LastCheckpointIndex    = data.checkpointIndex;
        LastCheckpointPosition = data.playerPosition;
    }
}
