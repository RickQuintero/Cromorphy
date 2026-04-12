using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralLegPlacement2D : MonoBehaviour {

    public bool legGrounded = false;
    public Vector2 stepPoint;
    public Vector2 stepNormal;

    // Resting position is in local 2D space (XY plane)
    public Vector2 optimalRestingPosition = Vector2.right;
    public Vector2 restingPosition {
        get { return (Vector2)transform.TransformPoint(optimalRestingPosition); }
    }

    public Vector2 worldVelocity = Vector2.right;

    public Vector2 desiredPosition {
        get {
            // Random 2D scatter (insideUnitCircle replaces insideUnitSphere)
            return restingPosition + worldVelocity + (Random.insideUnitCircle * placementRandomization);
        }
    }

    public Vector2 worldTarget = Vector2.zero;
    public Transform ikTarget;
    public Transform ikPoleTarget;

    public float placementRandomization = 0;
    public bool autoStep = true;

    public LayerMask solidLayer;
    public float stepRadius = 0.25f;
    public AnimationCurve stepHeightCurve;
    public float stepHeightMultiplier = 0.25f;
    public float stepCooldown = 1f;
    public float stepDuration = 0.5f;
    public float stepOffset;
    public float lastStep = 0;

    public float percent {
        get { return Mathf.Clamp01((Time.time - lastStep) / stepDuration); }
    }

    void Start() {
        worldVelocity = Vector2.zero;
        lastStep = Time.time + stepCooldown * stepOffset;
        ikTarget.position = (Vector3)restingPosition;
        Step();
    }

    void Update() {
        UpdateIkTarget();
        if (Time.time > lastStep + stepCooldown && autoStep) {
            Step();
        }
    }

    public void UpdateIkTarget() {
        stepPoint = AdjustPosition(worldTarget + worldVelocity);

        // In 2D, stepNormal shifts the foot perpendicular to the surface while stepping
        Vector2 lerpedPos = Vector2.Lerp((Vector2)ikTarget.position, stepPoint, percent);
        float heightOffset = stepHeightCurve.Evaluate(percent) * stepHeightMultiplier;
        ikTarget.position = (Vector3)(lerpedPos + stepNormal * heightOffset);
    }

    public void Step() {
        stepPoint = worldTarget = AdjustPosition(desiredPosition);
        lastStep = Time.time;
    }

    public Vector2 AdjustPosition(Vector2 position) {
        Vector2 origin = ikPoleTarget.position;
        Vector2 direction = position - origin;

        // CircleCast2D replaces SphereCast
        RaycastHit2D hit = Physics2D.CircleCast(
            origin,
            stepRadius,
            direction.normalized,
            direction.magnitude * 2f,
            solidLayer
        );

        if (hit.collider != null) {
            Debug.DrawLine(origin, hit.point, Color.green, 0f);
            position = hit.point;
            stepNormal = hit.normal;
            legGrounded = true;
        } else {
            Debug.DrawLine(origin, restingPosition, Color.red, 0f);
            position = restingPosition;
            stepNormal = Vector2.zero;
            legGrounded = false;
        }
        return position;
    }

    public void MoveVelocity(Vector2 newVelocity) {
        worldVelocity = Vector2.Lerp(worldVelocity, newVelocity, 1f - percent);
    }

    public void OnDrawGizmos() {
        if (ikPoleTarget == null) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine((Vector3)restingPosition, (Vector3)worldTarget);
        Gizmos.color = Color.green;
        Gizmos.DrawLine((Vector3)worldTarget, (Vector3)stepPoint);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(restingPosition, 0.02f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere((Vector3)worldTarget, 0.02f);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere((Vector3)stepPoint, 0.02f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(ikPoleTarget.position, 0.02f);
    }
}
