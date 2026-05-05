using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HungryUI : MonoBehaviour
{
    [Tooltip("TextMeshPro that shows the current hunger number.")]
    public TextMeshProUGUI label;

    [Tooltip("Image whose alpha is driven by the fade.")]
    public Image image;

    [Tooltip("Seconds to stay fully visible after a change before fading.")]
    public float holdDuration = 0.5f;

    [Tooltip("Seconds the fade-out takes.")]
    public float fadeDuration = 3f;

    // Countdown timer: (holdDuration + fadeDuration) → 0
    float _timer = 0f;
    int   _lastPoints = -1;

    void Start()
    {
        _lastPoints = HungryPointManager.Instance.currentHungryPoints;
        UpdateText(_lastPoints);
        SetAlpha(0f);
    }

    void Update()
    {
        int current = HungryPointManager.Instance.currentHungryPoints;

        if (current != _lastPoints)
        {
            _lastPoints = current;
            UpdateText(current);
            _timer = holdDuration + fadeDuration; // reset countdown on every change
        }

        if (_timer <= 0f) return;

        _timer -= Time.deltaTime;

        // Hold phase → full alpha; fade phase → lerp to 0
        float alpha = _timer > fadeDuration
            ? 1f
            : Mathf.Clamp01(_timer / fadeDuration);

        SetAlpha(alpha);
    }

    void UpdateText(int points)
    {
        if (label) label.text = points.ToString();
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
