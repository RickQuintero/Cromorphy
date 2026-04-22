using System.Collections;
using UnityEngine;

public class CloakingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Material cloakMaterial;

    [Header("Camuflaje Settings")]
    [SerializeField] private float Camuflaje_Max_Time = 5f;

    [Header("Glitch Settings")]
    [SerializeField] private float glitchDuration = 1f;
    [SerializeField] private float glitchSpeed = 0.1f;
    [SerializeField] private float glitchMinValue = 0.6f;
    [SerializeField] private float glitchMaxValue = 1f;

    public bool IsVisible { get; private set; } = true;

    private static readonly int CloakingValueID = Shader.PropertyToID("_CloackingValue");

    // Ranges from -Camuflaje_Max_Time (fully recharged) to +Camuflaje_Max_Time (fully drained)
    private float _timer;
    private bool _isGlitching;
    private Coroutine _glitchCoroutine;

    private bool CanCloak => _timer <= 0f && !_isGlitching;

    private void Awake()
    {
        _timer = -Camuflaje_Max_Time;
    }

    private void Update()
    {
        if (_isGlitching) return;

        bool shouldCloak = inputReader.CrouchHeld && CanCloak;

        if (shouldCloak)
        {
            _timer += Time.deltaTime;
            if (_timer >= Camuflaje_Max_Time)
            {
                _timer = Camuflaje_Max_Time;
                StartGlitch();
                return;
            }
        }
        else if (!inputReader.CrouchHeld)
        {
            _timer = Mathf.Max(_timer - Time.deltaTime, -Camuflaje_Max_Time);
        }

        SetVisible(!shouldCloak);
    }

    private void SetVisible(bool visible)
    {
        IsVisible = visible;
        cloakMaterial.SetFloat(CloakingValueID, visible ? 0f : 1f);
    }

    private void StartGlitch()
    {
        SetVisible(true);
        _isGlitching = true;
        if (_glitchCoroutine != null) StopCoroutine(_glitchCoroutine);
        _glitchCoroutine = StartCoroutine(GlitchRoutine());
    }

    private IEnumerator GlitchRoutine()
    {
        float elapsed = 0f;
        bool toggle = false;
        while (elapsed < glitchDuration)
        {
            cloakMaterial.SetFloat(CloakingValueID, toggle ? glitchMaxValue : glitchMinValue);
            toggle = !toggle;
            elapsed += glitchSpeed;
            yield return new WaitForSeconds(glitchSpeed);
        }
        SetVisible(true);
        _isGlitching = false;
    }
}
