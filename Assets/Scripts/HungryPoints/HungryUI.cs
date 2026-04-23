using UnityEngine;
using UnityEngine.UI;

public class HungryUI : MonoBehaviour
{
    [Tooltip("GameObject prefab to instantiate — must have a Image component.")]
    public GameObject hungerSlotPrefab;

    [Tooltip("Color when the slot is filled (active hunger point).")]
    public Color activeColor = Color.white;

    [Tooltip("Color when the slot is empty (lost hunger point).")]
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.25f);

    [Tooltip("Total number of hunger slots to spawn.")]
    private int maxHungryPoints;

    private Image[] _slots;
    private int _lastPoints = -1;

    void Start()
    {
        maxHungryPoints = HungryPointManager.Instance.maxHungryPoints;
        SpawnSlots();
    }

    void SpawnSlots()
    {
        if (hungerSlotPrefab == null)
        {
            Debug.LogError("HungryUI: hungerSlotPrefab is not assigned!", this);
            return;
        }

        foreach (Transform child in transform)
            Destroy(child.gameObject);

        _slots = new Image[maxHungryPoints];
        for (int i = 0; i < maxHungryPoints; i++)
        {
            GameObject go = Instantiate(hungerSlotPrefab, transform);
            Image slot = go.GetComponentInChildren<Image>();
            if (slot == null)
            {
                Debug.LogError("HungryUI: prefab has no Image component!", go);
                return;
            }
            slot.color = inactiveColor;
            _slots[i]  = slot;
        }

        _lastPoints = -1;
    }

    void Update()
    {
        if (_slots == null) return;
        int current = Mathf.Clamp(HungryPointManager.Instance.currentHungryPoints, 0, maxHungryPoints);
        if (current == _lastPoints) return;
        _lastPoints = current;

        for (int i = 0; i < _slots.Length; i++)
            _slots[i].color = i < current ? activeColor : inactiveColor;
    }
}
