using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Panel de "Guardar Partida" que aparece desde el menú de pausa.
/// Muestra los slots disponibles y permite sobrescribir o crear nueva partida.
/// </summary>
public class SaveGamePanel : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public GameObject  root;
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI infoText;
        public Button      saveButton;
        public Button      deleteButton;
    }

    [Header("Slots (debe haber 10)")]
    public SlotUI[] slots;

    [Header("Referencia al panel padre (para cerrarlo)")]
    public GameObject panelRoot;

    [Header("Referencia al jugador (para guardar posición)")]
    [Tooltip("Si se deja vacío, se busca automáticamente por tag 'Player'.")]
    public Transform playerTransform;

    private void OnEnable()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
        RefreshSlots();
    }

    // ── Refresca la UI ────────────────────────────────────────────────────────
    public void RefreshSlots()
    {
        for (int i = 0; i < SaveSystem.MAX_SLOTS; i++)
        {
            if (i >= slots.Length) break;

            SlotUI ui   = slots[i];
            int    slot = i;

            if (ui.labelText != null) ui.labelText.text = $"Ranura {i + 1}";

            if (SaveSystem.SlotExists(i))
            {
                SaveSystem.SaveData data = SaveSystem.Load(i);
                if (ui.infoText != null)
                    ui.infoText.text = $"Checkpoint {data.checkpointIndex + 1}\n{data.timestamp}";

                if (ui.deleteButton != null)
                {
                    ui.deleteButton.gameObject.SetActive(true);
                    ui.deleteButton.onClick.RemoveAllListeners();
                    ui.deleteButton.onClick.AddListener(() => DeleteSlot(slot));
                }
            }
            else
            {
                if (ui.infoText     != null) ui.infoText.text = "Vacío";
                if (ui.deleteButton != null) ui.deleteButton.gameObject.SetActive(false);
            }

            if (ui.saveButton != null)
            {
                ui.saveButton.onClick.RemoveAllListeners();
                ui.saveButton.onClick.AddListener(() => SaveToSlot(slot));
            }
        }
    }

    // ── Guardar en slot ───────────────────────────────────────────────────────
    private void SaveToSlot(int slot)
    {
        Vector3 pos = playerTransform != null
            ? playerTransform.position
            : (CheckpointManager.Instance != null
                ? CheckpointManager.Instance.LastCheckpointPosition
                : Vector3.zero);

        int checkpointIdx = CheckpointManager.Instance != null
            ? CheckpointManager.Instance.LastCheckpointIndex
            : 0;

        SaveSystem.SaveData data = new SaveSystem.SaveData
        {
            exists           = true,
            checkpointIndex  = checkpointIdx,
            sceneName        = SceneManager.GetActiveScene().name,
            playerPosition   = pos
        };

        SaveSystem.Save(slot, data);
        RefreshSlots();

        Debug.Log($"[SaveGamePanel] Guardado en slot {slot}.");
    }

    // ── Borrar slot ───────────────────────────────────────────────────────────
    private void DeleteSlot(int slot)
    {
        SaveSystem.DeleteSlot(slot);
        RefreshSlots();
    }

    // ── Cerrar panel ──────────────────────────────────────────────────────────
    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
