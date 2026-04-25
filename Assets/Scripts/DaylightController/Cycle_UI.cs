using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Cycle_UI : MonoBehaviour
{
    [Tooltip("TextMeshPro that shows the current cycle number.")]
    public TextMeshProUGUI label;

    [Tooltip("Image whose alpha is driven by the fade.")]
    public Image image;

    [Tooltip("Seconds to stay fully visible after a change before fading.")]
    public float holdDuration = 0.5f;

    [Tooltip("Seconds the fade-out takes.")]
    public float fadeDuration = 3f;

    float _timer = 0f;
    int   _lastCycles = -1;

    void Start()
    {
        _lastCycles = ScoreManager.Instance.numberOfCycles;
        UpdateText(_lastCycles);
        SetAlpha(0f);
    }

    void Update()
    {
        int current = ScoreManager.Instance.numberOfCycles;

        if (current != _lastCycles)
        {
            _lastCycles = current;
            UpdateText(current);
            _timer = holdDuration + fadeDuration;
        }

        if (_timer <= 0f) return;

        _timer -= Time.deltaTime;

        float alpha = _timer > fadeDuration
            ? 1f
            : Mathf.Clamp01(_timer / fadeDuration);

        SetAlpha(alpha);
    }

    void UpdateText(int cycles)
    {
        if (label) label.text = cycles.ToString();
    }

    void SetAlpha(float alpha)
    {
        if (image)
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        if (label)
        {
            Color c = label.color;
            c.a = alpha;
            label.color = c;
        }
    }
}
