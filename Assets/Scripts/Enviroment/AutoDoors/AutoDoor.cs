using UnityEngine;

public class AutoDoor : MonoBehaviour
{
    [Header("Requirements")]
    public int numberOfCyclesToOpen = 1;
    public int numberOfFirefliesRescued = 0; // in progress

    [Header("Indicator")]
    public SpriteRenderer indicatorRenderer;
    public Color checkColor = Color.green;
    public Color failColor = Color.red;
    public Color neutralColor = Color.white;
    public float colorLerpSpeed = 3f;

    [Header("Pulse")]
    [Range(0f, 1f)] public float pulseAlphaMin = 0.4f;
    [Range(0f, 1f)] public float pulseAlphaMax = 1f;
    public float pulseSpeed = 3f;

    private Animator _animator;
    private Color _targetColor;
    private bool _isOpen = false;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _targetColor = neutralColor;
        if (indicatorRenderer != null)
            indicatorRenderer.color = neutralColor;
    }

    private void Update()
    {
        if (indicatorRenderer == null) return;

        Color current = Color.Lerp(indicatorRenderer.color, _targetColor, Time.deltaTime * colorLerpSpeed);
        current.a = Mathf.Lerp(pulseAlphaMin, pulseAlphaMax, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
        indicatorRenderer.color = current;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || _isOpen) return;

        if (ScoreManager.Instance != null && ScoreManager.Instance.numberOfCycles >= numberOfCyclesToOpen)
        {
            _isOpen = true;
            _targetColor = checkColor;
            _animator?.SetBool("IsOpen", true);
        }
        else
        {
            _targetColor = failColor;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || _isOpen) return;

        _targetColor = neutralColor;
    }
}
