using UnityEngine;

/// <summary>
/// Coloca este script en un trigger (Collider2D o Collider con Is Trigger = true).
/// Cuando el jugador entra, registra el checkpoint activo en CheckpointManager
/// y muestra una animación/indicador visual opcional.
/// </summary>
public class CheckpointZone : MonoBehaviour
{
    [Header("Identificación")]
    [Tooltip("Índice único de este checkpoint en la escena. Empieza en 0.")]
    public int checkpointIndex = 0;

    [Tooltip("Tag del jugador.")]
    public string playerTag = "Player";

    [Header("Visual (opcional)")]
    [Tooltip("Objeto que se activa al alcanzar el checkpoint (ej: partícula, luz, bandera).")]
    public GameObject activatedVisual;

    private bool _activated = false;

    private void Start()
    {
        // Si este checkpoint ya fue el último guardado, mostrarlo como activo
        if (CheckpointManager.Instance != null &&
            CheckpointManager.Instance.LastCheckpointIndex == checkpointIndex)
        {
            SetActivated();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryActivate(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryActivate(other.gameObject);
    }

    private void TryActivate(GameObject other)
    {
        if (_activated) return;
        if (!other.CompareTag(playerTag)) return;

        SetActivated();
        CheckpointManager.Instance?.RegisterCheckpoint(this);

        Debug.Log($"[CheckpointZone] Checkpoint {checkpointIndex} alcanzado.");
    }

    private void SetActivated()
    {
        _activated = true;
        if (activatedVisual != null)
            activatedVisual.SetActive(true);
    }
}
