using UnityEngine;

/// <summary>
/// Place this component on any trigger zone (BoxCollider, SphereCollider, etc.
/// with "Is Trigger" enabled).
///
/// When an entity enters the zone it is automatically returned to the pool
/// instead of being destroyed, keeping allocation costs at zero.
///
/// Optionally filter by tag (e.g. "Enemy", "NPC") or leave blank to accept
/// any object that has an EntityPoolMember.
///
/// Setup checklist:
///   1. Add a Collider to this GameObject and tick "Is Trigger".
///   2. Make sure every entity prefab has an EntityPoolMember component
///      (EntityPoolManager adds one automatically on first spawn).
///   3. Use the tag filter below if you only want specific entities to trigger the return.
/// </summary>
public class EntityPoolDestroyer : MonoBehaviour
{
    [Header("Filtering")]
    [Tooltip("Only GameObjects with this tag will be returned to the pool. " +
             "Leave blank to accept any object that has an EntityPoolMember.")]
    public string entityTag = "Enemy";

    [Tooltip("Log a message each time an entity is returned (useful during development).")]
    public bool debugLog = false;

    // -------------------------------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        // Optional tag guard — skip the check when no tag is specified
        if (!string.IsNullOrEmpty(entityTag) && !other.CompareTag(entityTag))
            return;

        EntityPoolMember member = other.GetComponent<EntityPoolMember>();

        if (member == null)
            return;

        if (debugLog)
            Debug.Log($"[EntityPoolDestroyer] '{other.name}' entered zone '{name}' — returning to pool.");

        member.ReturnToPool();
    }

    // -------------------------------------------------------------------------
    // Editor helper — draw the trigger bounds in the Scene view
    // -------------------------------------------------------------------------
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);

        Collider col = GetComponent<Collider>();
        if (col == null) return;

        switch (col)
        {
            case BoxCollider box:
                Matrix4x4 prev = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = prev;
                break;

            case SphereCollider sphere:
                Gizmos.DrawSphere(
                    transform.position + sphere.center,
                    sphere.radius * Mathf.Max(
                        transform.lossyScale.x,
                        transform.lossyScale.y,
                        transform.lossyScale.z));
                break;
        }
    }
#endif
}
