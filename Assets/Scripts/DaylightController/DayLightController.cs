using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayLightController : MonoBehaviour
{
    public static DayLightController Instance { get; private set; }

    [Header("Light Colors")]
    public Color dayColor = Color.white;
    public Color nightColor = new Color(0.05f, 0.05f, 0.2f);

    [Header("Transition")]
    [Tooltip("Duration in seconds for the day/night color transition.")]
    public float transitionDuration = 2f;

    [Header("Initial State")]
    public bool startAtNight = false;

    [Header("Sky")]
    [Tooltip("Optional — keeps the procedural sky in sync with the light cycle.")]
    public ProceduralSkyController skyController;

    private List<Light2D> lights = new List<Light2D>();
    private bool isDay = true;
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        isDay = !startAtNight;
    }

    void Start()
    {
        lights.AddRange(GetComponentsInChildren<Light2D>());

        if (startAtNight)
        {
            ApplyColor(nightColor);
            skyController?.SetNight();
        }
        else
        {
            ApplyColor(dayColor);
            skyController?.SetDay();
        }
    }

    public bool IsDay => isDay;
    public bool IsTransitioning => isTransitioning;

    public void TriggerCycle()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionCycle());
    }

    private IEnumerator TransitionCycle()
    {
        isTransitioning = true;

        Color fromColor = isDay ? dayColor : nightColor;
        Color toColor   = isDay ? nightColor : dayColor;
        float elapsed   = 0f;

        // Both day→night and night→day pass through a sunrise/sunset look
        skyController?.SetSunrise();

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            ApplyColor(Color.Lerp(fromColor, toColor, Mathf.Clamp01(elapsed / transitionDuration)));
            yield return null;
        }

        ApplyColor(toColor);
        isDay = !isDay;
        isTransitioning = false;

        if (isDay) skyController?.SetDay();
        else       skyController?.SetNight();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.numberOfCycles++;
    }

    private void ApplyColor(Color color)
    {
        foreach (var light in lights)
            if (light != null) light.color = color;
    }
}
