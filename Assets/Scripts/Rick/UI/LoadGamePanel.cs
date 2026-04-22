using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de "Cargar Partida" en el Menú Principal.
/// Requiere exactamente SaveSystem.MAX_SLOTS SlotUI configurados en el Inspector.
/// </summary>
public class LoadGamePanel : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public GameObject  root;           // el panel/card del slot
        public TextMeshProUGUI labelText;  // "Ranura 1", "Ranura 2"…
        public TextMeshProUGUI infoText;   // checkpoint + fecha, o "Vacío"
        public Button      loadButton;
        public Button      deleteButton;
    }

    [Header("Slots (debe haber 10)")]
    public SlotUI[] slots;

    [Header("Escena a cargar")]
    [Tooltip("Nombre exacto de la escena de juego.")]
    public string gameSceneName = "Zone_01";

    [Header("Referencia al panel padre (para cerrarlo)")]
    public GameObject panelRoot;

    private void OnEnable()
    {
        RefreshSlots();
    }

    // ── Refresca la UI de todos los slots ─────────────────────────────────────
    public void RefreshSlots()
    {
        for (int i = 0; i < SaveSystem.MAX_SLOTS; i++)
        {
            if (i >= slots.Length) break;

            SlotUI ui   = slots[i];
            int    slot = i; // captura para lambda

            if (ui.labelText  != null) ui.labelText.text = $"Ranura {i + 1}";

            if (SaveSystem.SlotExists(i))
            {
                SaveSystem.SaveData data = SaveSystem.Load(i);

                if (ui.infoText     != null)
                    ui.infoText.text = $"Checkpoint {data.checkpointIndex + 1}\n{data.timestamp}";

                if (ui.loadButton   != null)
                {
                    ui.loadButton.interactable = true;
                    ui.loadButton.onClick.RemoveAllListeners();
                    ui.loadButton.onClick.AddListener(() => LoadSlot(slot));
                }
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
                if (ui.loadButton   != null) ui.loadButton.interactable = false;
                if (ui.deleteButton != null) ui.deleteButton.gameObject.SetActive(false);
            }
        }
    }

    // ── Cargar slot ───────────────────────────────────────────────────────────
    private void LoadSlot(int slot)
    {
        SaveSystem.SaveData data = SaveSystem.Load(slot);
        if (data == null) return;

        // Guardamos qué slot se está cargando para que la escena lo lea al iniciar
        PlayerPrefs.SetInt("pending_load_slot", slot);
        PlayerPrefs.Save();

        Debug.Log($"[LoadGamePanel] Cargando slot {slot} → escena {data.sceneName}");

        // Usamos ArtSceneManager si existe, si no cargamos directo
        if (ArtSceneManager.Instance != null)
            ArtSceneManager.Instance.LoadSceneByNumber(
                UnityEngine.SceneManagement.SceneManager.GetSceneByName(data.sceneName).buildIndex);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(data.sceneName);
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
