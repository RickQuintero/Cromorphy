using UnityEngine;

public class FireflyTargetTrigger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool isActive = true;

    [Header("Gizmo")]
    [SerializeField] private float gizmoRadius = 0.5f;
    [SerializeField] private Color gizmoColor  = new Color(1f, 0.8f, 0f, 0.4f);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || !other.CompareTag("Player")) return;
        Trigger();
    }

    /// <summary>Advances the path to the next target. Callable from code (e.g. FireflyPathGuide when swarm reaches this point).</summary>
    public void Trigger()
    {
        if (!isActive) return;
        isActive = false;
        FireflyPathGuide.Instance?.AdvanceTarget();
    }

    /// <summary>Re-enables this trigger so it can fire again.</summary>
    public void Reset() => isActive = true;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoRadius);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
}
