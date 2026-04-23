using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(SpriteRenderer))]
public class FireflyGuide : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Light2D guideLight;

    [Header("Day - Rest")]
    [SerializeField] private Vector3 restPosition = Vector3.zero;
    [SerializeField] private float moveSpeed = 3f;

    [Header("Night - Float")]
    [SerializeField] private float amplitude = 0.4f;
    [SerializeField] private float frequency = 0.8f;
    [SerializeField] private float followOffset = 0.8f;

    private bool _wasDay;
    private float _timeOffset;

    private void Awake()
    {
        _timeOffset = Random.Range(0f, 100f);

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    private void Start()
    {
        _wasDay = DayLightController.Instance != null && DayLightController.Instance.IsDay;
        ApplyDayState(_wasDay);
    }

    private void Update()
    {
        if (DayLightController.Instance == null) return;

        bool isDay = DayLightController.Instance.IsDay;

        if (isDay != _wasDay)
        {
            _wasDay = isDay;
            ApplyDayState(isDay);
        }

        if (isDay)
        {
            transform.position = Vector3.MoveTowards(transform.position, restPosition, moveSpeed * Time.deltaTime);
        }
        else
        {
            if (player == null) return;
            float t = Time.time * frequency + _timeOffset;
            Vector3 orbit = new Vector3(
                Mathf.Sin(t) * amplitude,
                Mathf.Cos(t * 0.7f) * amplitude + followOffset,
                0f
            );
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position + orbit,
                moveSpeed * Time.deltaTime
            );
        }
    }

    private void ApplyDayState(bool isDay)
    {
        if (guideLight != null) guideLight.enabled = !isDay;
    }

    /// <summary>Turns the guide light on or off. Called by FireflyMorseController during morse playback.</summary>
    public void SetMorseLight(bool on)
    {
        if (guideLight != null) guideLight.enabled = on;
    }
}
