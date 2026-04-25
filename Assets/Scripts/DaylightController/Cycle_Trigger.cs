using System.Collections;
using UnityEngine;

public class Cycle_Trigger : MonoBehaviour
{
    [Tooltip("Minimum hungry points the player must have to activate the cycle.")]
    public int requiredPoints = 5;

    [Tooltip("How many hungry points are removed during the cycle, one at a time.")]
    public int pointsToRemove = 4;

    [Tooltip("Delay in seconds between each individual point removal.")]
    public float pointRemovalInterval = 0.5f;

    [Tooltip("Cooldown in seconds before this trigger can activate again.")]
    public float cooldown = 5f;

    private float lastTriggerTime = float.NegativeInfinity;
    private bool isProcessing = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (isProcessing) return;
        if (Time.time - lastTriggerTime < cooldown) return;

        if (HungryPointManager.Instance == null || ArtSceneManager.Instance == null || DayLightController.Instance == null) return;
        if (HungryPointManager.Instance.currentHungryPoints < requiredPoints) return;

        StartCoroutine(HandleCycle());
    }

    private IEnumerator HandleCycle()
    {
        isProcessing = true;
        lastTriggerTime = Time.time;

        ArtSceneManager.Instance.TriggerCycleTransition();

        for (int i = 0; i < pointsToRemove; i++)
        {
            HungryPointManager.Instance.RemovePoint(1);
            yield return new WaitForSeconds(pointRemovalInterval);
        }

        isProcessing = false;
    }
}
