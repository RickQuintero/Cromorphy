using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraSwicher : MonoBehaviour
{
    [SerializeField] private Camera activeCam;
    public bool VisualizeInEditor = true;
    private BoxCollider2D _triggerBox;

    private void Start()
    {
        _triggerBox = GetComponent<BoxCollider2D>();
        _triggerBox.isTrigger = true;
        SizeColliderToCamera();
    }

    // Called by Unity in the editor whenever a field changes in the Inspector.
    private void OnValidate()
    {
        SizeColliderToCamera();
    }

    /// <summary>
    /// Resizes the BoxCollider2D to match the orthographic view of activeCam.
    /// Safe to call both in editor and at runtime.
    /// </summary>
    public void SizeColliderToCamera()
    {
        if (activeCam == null || !activeCam.orthographic) return;

        // Fetch the component directly so this works before Start (e.g. OnValidate).
        var box = _triggerBox != null ? _triggerBox : GetComponent<BoxCollider2D>();
        if (box == null) return;

        float orthoHalfHeight = activeCam.orthographicSize;
        float aspect = activeCam.aspect;

        box.isTrigger = true;
        box.size = new Vector2(orthoHalfHeight * aspect * 2f, orthoHalfHeight * 2f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        CameraManager.Instance.SetActiveCamera(activeCam);
    }

    private void OnDrawGizmos()
    {
        if (!VisualizeInEditor) return;
        if (activeCam == null || !activeCam.orthographic) return;

        float orthoHalfHeight = activeCam.orthographicSize;
        float aspect = activeCam.aspect;
        var size = new Vector3(orthoHalfHeight * aspect * 2f, orthoHalfHeight * 2f, 0.1f);

        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawCube(transform.position, size);
        Gizmos.color = new Color(0f, 1f, 0f, 0.9f);
        Gizmos.DrawWireCube(transform.position, size);
    }
}
