using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadGamePanel : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public GameObject      root;
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI infoText;
        public Button          loadButton;
        public Button          deleteButton;
    }

    [Header("Slots")]
    public SlotUI[] slots;

    [Header("Panel")]
    public GameObject panelRoot;

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

                if (ui.loadButton != null)
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
                if (ui.infoText    != null) ui.infoText.text = "Vacío";
                if (ui.loadButton  != null) ui.loadButton.interactable = false;
                if (ui.deleteButton != null) ui.deleteButton.gameObject.SetActive(false);
            }
        }
    }

    private void LoadSlot(int slot)
    {
        SaveSystem.SaveData data = SaveSystem.Load(slot);
        if (data == null) return;

        PlayerPrefs.SetInt(SaveSystem.PENDING_LOAD_KEY, slot);
        PlayerPrefs.Save();

        SceneManager.LoadScene(data.sceneName);
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
