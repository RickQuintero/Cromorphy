using TMPro;
using UnityEngine;

/// <summary>
/// Attach to the "Press Enter to Continue" text GameObject.
/// Pulses alpha while GameManager is in the Death state (prompt phase).
/// Pressing Interact calls GameManager.OpenDeathUI() to reveal the Death UI panel.
/// </summary>
public class ContinuePrompt : MonoBehaviour
{
    [SerializeField] private TMP_Text    _text;
    [SerializeField] private InputReader _input;
    [Tooltip("Speed of the alpha sine-wave pulse.")]
    [SerializeField] private float _pulseSpeed = 2f;

    private void Update()
    {
        if (GameManager.Instance == null || _text == null) return;

        bool isPromptPhase = GameManager.Instance.CurrentState == GameState.Death;

        if (!isPromptPhase)
        {
            SetAlpha(0f);
            return;
        }

        SetAlpha((Mathf.Sin(Time.time * _pulseSpeed) + 1f) * 0.5f);

        if (_input != null && _input.EnterDown)
            GameManager.Instance.OpenDeathUI();
    }

    private void SetAlpha(float a)
    {
        Color c = _text.color;
        c.a = a;
        _text.color = c;
    }
}
