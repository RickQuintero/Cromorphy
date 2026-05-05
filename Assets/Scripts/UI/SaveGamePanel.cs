using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SaveGamePanel : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public GameObject      root;
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI infoText;
        public Button          saveButton;
        public Button          deleteButton;
    }

    [Header("Slots")]
    public SlotUI[] slots;

    [Header("Panel")]
    public GameObject panelRoot;

    [Header("Player")]
    [Tooltip("Leave empty to auto-find by 'Player' tag.")]
    public Transform playerTransform;

    private void Awake()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    private void OnEnable() => RefreshSlots();

    public void RefreshSlots()
    {
        int count = Mathf.Min(slots.Length, SaveSystem.MAX_SLOTS);
        for (int i = 0; i < count; i++)
        {
            SlotUI ui   = slots[i];
            int    slot = i;

            if (ui.labelText != null)
                ui.labelText.text = $"Ranura {i + 1}";

            SaveSystem.SaveData data = SaveSystem.Load(i);

            if (data != null)
            {
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

    private void SaveToSlot(int slot)
    {
        Vector3 pos = playerTransform != null
            ? playerTransform.position
            : CheckpointManager.Instance?.LastCheckpointPosition ?? Vector3.zero;

        SaveSystem.SaveData data = new SaveSystem.SaveData
        {
            exists          = true,
            checkpointIndex = CheckpointManager.Instance?.LastCheckpointIndex ?? 0,
            sceneName       = SceneManager.GetActiveScene().name,
            playerPosition  = pos
        };

        SaveSystem.Save(slot, data);
        SaveSystem.SetActiveSlot(slot);
        RefreshSlots();
    }

    private void DeleteSlot(int slot)
    {
        SaveSystem.DeleteSlot(slot);
        RefreshSlots();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
