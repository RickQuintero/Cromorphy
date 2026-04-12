using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureController2D : MonoBehaviour {

    public float moveInputFactor = 5f;
    public Vector2 inputVelocity;
    public Vector2 worldVelocity;
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;
    public float rotationSpeed = 10f;
    private float mSpeed = 0;

    public ProceduralLegPlacement2D[] legs;
    public int index;
    public bool dynamicGait = false;
    public float timeBetweenSteps = 0.25f;
    public float stepDurationRatio = 2f;
    [Tooltip("Used if dynamicGait is true to calculate timeBetweenSteps")]
    public float maxTargetDistance = 1f;
    public float lastStep = 0;

    [Header("Alignment")]
    public bool useAlignment = true;
    public float desiredSurfaceDist = -1f;
    public float dist;
    public bool grounded = false;
    public float gravity = 20f;

    void Start() {
        // No averageRotationRadius calc needed for 2D side-scroller
    }

    void Update() {
        if (useAlignment) CalculateOrientation();
        Move();

        if (dynamicGait) {
            timeBetweenSteps = grounded
                ? maxTargetDistance / Mathf.Max(worldVelocity.magnitude, 0.01f)
                : 0.25f;
        }

        if (legs == null || legs.Length == 0) return;

        if (Time.time > lastStep + (timeBetweenSteps / legs.Length)) {
            index = (index + 1) % legs.Length;
            if (legs[index] == null) return;

            for (int i = 0; i < legs.Length; i++) {
                legs[i].MoveVelocity(CalculateLegVelocity(i));
            }

            legs[index].stepDuration = Mathf.Min(1f, (timeBetweenSteps / legs.Length) * stepDurationRatio);
            legs[index].worldVelocity = CalculateLegVelocity(index);
            legs[index].Step();
            lastStep = Time.time;
        }
    }

    // In 2D there's no rotation component, so leg velocity is purely translational
    public Vector2 CalculateLegVelocity(int legIndex) {
        return (worldVelocity * timeBetweenSteps) / 2f;
    }

    private void Move() {
        mSpeed = Input.GetButton("Fire3") ? sprintSpeed : walkSpeed;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical"); // Use for top-down; remove for side-scrollers

        Vector2 localInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        inputVelocity = Vector2.MoveTowards(inputVelocity, localInput, Time.deltaTime * moveInputFactor);
        worldVelocity = inputVelocity * mSpeed;

        // Flip the sprite horizontally based on movement direction
        if (horizontal > 0.05f)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (horizontal < -0.05f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        transform.position += (Vector3)(worldVelocity * Time.deltaTime);
    }

    private void CalculateOrientation() {
        Vector2 averageUp = Vector2.zero;
        float avgSurfaceDist = 0f;
        grounded = false;

        for (int i = 0; i < legs.Length; i++) {
            // Accumulate surface distance in local space
            avgSurfaceDist += transform.InverseTransformPoint((Vector3)legs[i].stepPoint).y;

            // Average the surface normals from each grounded leg
            if (legs[i].stepNormal != Vector2.zero)
                averageUp += legs[i].stepNormal;
            else
                averageUp = transform.up; // Fall back to current up if ungrounded

            grounded |= legs[i].legGrounded;
        }

        averageUp /= legs.Length;
        avgSurfaceDist /= legs.Length;
        dist = avgSurfaceDist;

        Debug.DrawRay(transform.position, averageUp, Color.red, 0);

        // Rotate around Z to align transform.up with the averaged surface normal
        float targetAngle = Mathf.Atan2(averageUp.x, averageUp.y) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, -targetAngle);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        if (grounded) {
            // Push creature toward the surface
            transform.Translate(0f, -(-avgSurfaceDist + desiredSurfaceDist) * 0.5f, 0f, Space.Self);
        } else {
            // Simple 2D gravity when airborne
            transform.Translate(0f, -gravity * Time.deltaTime, 0f, Space.World);
        }
    }

    public void OnDrawGizmosSelected() {
        // Draw a circle representing the creature's approximate reach
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
