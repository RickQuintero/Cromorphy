using UnityEngine;

/// <summary>
/// Coloca este script en la escena de juego (Zone_01, etc.).
/// Al iniciar, detecta si hay una partida pendiente de cargar y
/// reposiciona al jugador en el checkpoint guardado.
/// </summary>
public class SaveLoadBootstrap : MonoBehaviour
{
    [Tooltip("Si se deja vacío, se busca automáticamente por tag 'Player'.")]
    public Transform playerTransform;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        int pendingSlot = PlayerPrefs.GetInt("pending_load_slot", -1);
        if (pendingSlot < 0) return; // nueva partida, sin carga

        SaveSystem.SaveData data = SaveSystem.Load(pendingSlot);
        if (data == null) return;

        // Restaurar estado del checkpoint
        if (CheckpointManager.Instance != null)
            CheckpointManager.Instance.RestoreFromSave(data);

        // Mover al jugador a la posición guardada
        if (playerTransform != null)
            playerTransform.position = data.playerPosition;

        // Limpiar la clave para que no se vuelva a aplicar en reinicios
        PlayerPrefs.DeleteKey("pending_load_slot");
        PlayerPrefs.Save();

        Debug.Log($"[SaveLoadBootstrap] Partida cargada desde slot {pendingSlot} — checkpoint {data.checkpointIndex}");
    }
}
