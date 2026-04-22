using UnityEngine;
using UnityEngine.InputSystem;

public class UIParallaxJuice : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public RectTransform target;
        public Vector2 strength = new Vector2(20f, 10f);
        [HideInInspector] public Vector2 originPosition;
    }

    [SerializeField] private ParallaxLayer[] layers;
    [SerializeField] [Range(0f, 20f)] private float smoothSpeed = 8f;

    private Vector2 _currentOffset;

    void Start()
    {
        foreach (var layer in layers)
            if (layer.target != null)
                layer.originPosition = layer.target.anchoredPosition;
    }

    void Update()
    {
        if (Mouse.current == null) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 mouseNormalized = new Vector2(
            (mousePos.x / Screen.width  - 0.5f) * 2f,
            (mousePos.y / Screen.height - 0.5f) * 2f
        );

        _currentOffset = Vector2.Lerp(_currentOffset, mouseNormalized, Time.deltaTime * smoothSpeed);

        foreach (var layer in layers)
        {
            if (layer.target == null) continue;
            layer.target.anchoredPosition = layer.originPosition + _currentOffset * layer.strength;
        }
    }
}
