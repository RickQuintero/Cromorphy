using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralLegPlacement : MonoBehaviour
{
    [Header("Grounding")]
    public bool    legGrounded = false;
    public Vector3 stepPoint;
    public Vector3 stepNormal;
    public Vector3 worldTarget = Vector3.zero;
    public Transform  ikTarget;

    // ── Ground detection settings ─────────────────────────────────────────────
    [Header("Detection")]
    public Transform RayCastOrigin;
    public LayerMask solidLayer;
    public float stepRadius = 1.25f;

    // ── Step animation settings ───────────────────────────────────────────────
    [Header("Stepping")]
    public AnimationCurve stepHeightCurve;
    public float stepHeightMultiplier = 1.25f;
    public float stepCooldown         = 1.5f;
    public float stepDuration         = 1.25f;
    public float stepOffset           = 0.1f;
    public float lastStep             = 0.1f;
    public float placementRandomization = 0.1f;

    public float percent
    {
        get { return Mathf.Clamp01((Time.time - lastStep) / stepDuration); }
    }

    public void Step()
    {
        SmoothStep();
        if (percent >= 1f)
        {
            lastStep = Time.time;
        }
    }

    public void Follow()
    {
        if (RayCastOrigin == null || ikTarget == null)
            return;

        // Move ikTarget towards RayCastOrigin if too far
        if (Vector2.Distance(RayCastOrigin.position, ikTarget.position) > stepRadius)
        {
            ikTarget.position = Vector3.Lerp(ikTarget.position, RayCastOrigin.position, Time.deltaTime * 5f);
        }
    }

    public void SmoothStep()
    {
        if (ikTarget == null)
            return;

        AdjustPosition();
        Vector3 stepOffsetVector = stepNormal * stepOffset;
        float height = stepHeightCurve != null ? stepHeightCurve.Evaluate(percent) * stepHeightMultiplier : 0f;
        Vector3 animatedOffset = stepOffsetVector + Vector3.up * height;
        ikTarget.position = Vector3.Lerp(ikTarget.position, stepPoint + animatedOffset, percent);
    }

    public void AdjustPosition()
    {
        Vector2 origin = RayCastOrigin.position;
        RaycastHit2D hit = Physics2D.CircleCast(origin, stepRadius, Vector2.down, Mathf.Infinity, solidLayer);
        if (hit.collider != null)
        {
            Debug.DrawLine(origin, hit.point, Color.green, stepRadius);
            stepNormal = new Vector2(hit.normal.x, hit.normal.y);
            legGrounded = true;
            stepPoint = new Vector3(hit.point.x, hit.point.y, 0f);
        }
        else
        {
            Follow();
            legGrounded = false;
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (RayCastOrigin == null)
            return;

        Gizmos.color = legGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(RayCastOrigin.position, stepPoint);
        Gizmos.DrawWireSphere(RayCastOrigin.position, stepRadius);
    }
}
