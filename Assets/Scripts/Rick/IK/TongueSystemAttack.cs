using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegIk2D : MonoBehaviour {

    public Transform ikTarget;

    public enum IkType {
        Iterative,
        Continuous
    }

    [Header("IK Data")]
    public bool isMounted = false;
    public IkType ikType;
    [Tooltip("Only Used If IkType is Iterative")] public int iterations = 4;

    [Header("Segment Data")]
    public int segmentCount = 4;
    public float segmentLength = 1f;
    public float minThickness = 0.01f;
    public float maxThickness = 0.1f;

    // In 2D we use sprites instead of meshes
    public Sprite segmentSprite;
    public Material segmentMaterial;

    public Segment[] segments;
    public Vector2 offset;
    public bool HasPhysics = false;

    void Start() {
        segments = new Segment[segmentCount];
        for (int i = 0; i < segmentCount; i++) {
            GameObject go = new GameObject("Segment " + i);
            go.transform.parent = transform;
            Transform t = go.transform;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (segmentSprite != null) sr.sprite = segmentSprite;
            if (segmentMaterial != null) sr.material = segmentMaterial;

            if (HasPhysics) {
                go.AddComponent<BoxCollider2D>();
                Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f; // Disable gravity on segments by default
            }

            float st = Mathf.Lerp(minThickness, maxThickness, i / (float)segmentCount);
            // In 2D: X = length (along right axis), Y = thickness
            t.localScale = new Vector3(segmentLength, st, 1f);

            Segment s = new Segment(t, segmentLength);
            segments[i] = s;
        }
    }

    void Update() {
        switch (ikType) {
            case IkType.Continuous:
                ContinuousIK();
                break;
            case IkType.Iterative:
                IterativeIK();
                break;
            default:
                Debug.LogError("IKType Behaviour Not Defined");
                break;
        }
    }

    public void IterativeIK() {
        for (int s = 0; s < segmentCount; s++) {
            // Place segments along the right axis instead of forward
            segments[s].position = (Vector3)((Vector2)transform.position + Vector2.right * s * segmentLength);
        }
        for (int i = 0; i < iterations; i++) {
            segments[segmentCount - 1].AdjustTo(ikTarget.position);
            for (int s = segmentCount - 2; s >= 0; s--) {
                segments[s].AdjustTo(segments[s + 1].tail);
            }
            if (isMounted) Remount();
        }
    }

    public void ContinuousIK() {
        segments[segmentCount - 1].AdjustTo(ikTarget.position);
        for (int s = segmentCount - 2; s >= 0; s--) {
            segments[s].AdjustTo(segments[s + 1].tail - (Vector3)offset);
        }
        if (isMounted) Remount();
    }

    public void Remount() {
        segments[0].tail = transform.position;
        for (int i = 1; i < segmentCount; i++) {
            segments[i].tail = segments[i - 1].head;
        }
    }

    [System.Serializable]
    public class Segment {
        public Transform transform;

        public Vector3 position {
            get { return transform.position; }
            set { transform.position = value; }
        }

        // In 2D, segments extend along their local right axis
        public Vector3 head {
            get { return transform.position + transform.right * halfLength; }
            set { transform.position = value - transform.right * halfLength; }
        }

        public Vector3 tail {
            get { return transform.position - transform.right * halfLength; }
            set { transform.position = value + transform.right * halfLength; }
        }

        public float halfLength {
            get { return length * 0.5f; }
            set { length = value * 2f; }
        }

        float length;

        public Segment(Transform t, float l) {
            transform = t;
            length = l;
        }

        public void AdjustTo(Vector3 p) {
            LookAt(p);
            MoveTo(p);
        }

        // Rotate around Z axis to face the target in 2D
        public void LookAt(Vector3 p) {
            Vector2 dir = (Vector2)(p - position);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public void MoveTo(Vector3 p) {
            head = p;
        }
    }
}
