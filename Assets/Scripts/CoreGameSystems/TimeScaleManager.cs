using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this script to any GameObject in your scene.
/// Assign the Slider field in the Inspector.
/// The Slider value (0 to 1) directly controls Time.timeScale.
/// </summary>
public class TimeScaleManager : MonoBehaviour
{
    [Header("Time Scale Control")]
    [Range(0f, 1f)]
    public float timeScale = 1f;
 
    void Update()
    {
        Time.timeScale = timeScale;
 
        // Keep physics in sync with timescale
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }
}