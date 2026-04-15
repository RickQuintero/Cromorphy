#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RicksonDevs.FastIK
{
    [System.Serializable]
    public struct BoneConstraint
    {
        [Tooltip("Min angle relative to parent bone direction (degrees, ≤ 0 = clockwise).")]
        [Range(-180f, 0f)]
        public float MinAngle;

        [Tooltip("Max angle relative to parent bone direction (degrees, ≥ 0 = counter-clockwise).")]
        [Range(0f, 180f)]
        public float MaxAngle;
    }

    /// <summary>
    /// FABRIK IK Solver — 2D version.
    /// All positions are solved in the XY plane; rotations around Z only.
    ///
    /// Constraints: one BoneConstraint per bone segment (index 0 = segment closest to root).
    /// Leave the array empty to run unconstrained.
    /// </summary>
    public class FastIKFabric2D : MonoBehaviour
    {
        public int ChainLength = 2;
        public Transform Target;

        [Header("Pole / Elbow Constraint")]
        [Tooltip("When true, middle joints are biased toward the Pole transform (elbow / knee hint).")]
        public bool IsElbowRequired = false;
        [Tooltip("The pole target — place it where the elbow / knee should point.")]
        public Transform Pole;

        [Header("Joint Constraints (index 0 = closest to root)")]
        [Tooltip("One entry per bone segment. Controls how much each joint can bend relative to its parent.")]
        public BoneConstraint[] Constraints;

        [Header("Solver Parameters")]
        public int Iterations = 10;
        public float Delta = 0.001f;

        [Range(0, 1)]
        public float SnapBackStrength = 1f;

        protected float[]      BonesLength;
        protected float        CompleteLength;
        protected Transform[]  Bones;
        protected Vector2[]    Positions;
        protected Vector2[]    StartDirectionSucc;
        protected Quaternion[] StartRotationBone;
        protected Quaternion   StartRotationTarget;
        protected Transform    Root;

        void Awake() => Init();

        void Init()
        {
            Bones              = new Transform[ChainLength + 1];
            Positions          = new Vector2[ChainLength + 1];
            BonesLength        = new float[ChainLength];
            StartDirectionSucc = new Vector2[ChainLength + 1];
            StartRotationBone  = new Quaternion[ChainLength + 1];

            Root = transform;
            for (int i = 0; i <= ChainLength; i++)
            {
                if (Root == null)
                    throw new UnityException("ChainLength exceeds the ancestor hierarchy!");
                Root = Root.parent;
            }

            if (Target == null)
            {
                Target = new GameObject(gameObject.name + " Target").transform;
                Target.position = transform.position;
            }
            StartRotationTarget = GetRotationRootSpace(Target);

            var current = transform;
            CompleteLength = 0;
            for (int i = Bones.Length - 1; i >= 0; i--)
            {
                Bones[i]             = current;
                StartRotationBone[i] = GetRotationRootSpace(current);

                if (i == Bones.Length - 1)
                    StartDirectionSucc[i] = GetPositionRootSpace(Target) - GetPositionRootSpace(current);
                else
                {
                    StartDirectionSucc[i]  = GetPositionRootSpace(Bones[i + 1]) - GetPositionRootSpace(current);
                    BonesLength[i]         = StartDirectionSucc[i].magnitude;
                    CompleteLength        += BonesLength[i];
                }

                current = current.parent;
            }
        }

        void LateUpdate() => ResolveIK();

        private void ResolveIK()
        {
            if (Target == null) return;
            if (BonesLength.Length != ChainLength) Init();

            for (int i = 0; i < Bones.Length; i++)
                Positions[i] = GetPositionRootSpace(Bones[i]);

            Vector2    targetPosition = GetPositionRootSpace(Target);
            Quaternion targetRotation = GetRotationRootSpace(Target);

            if ((targetPosition - Positions[0]).sqrMagnitude >= CompleteLength * CompleteLength)
            {
                // Out of reach — stretch toward target
                Vector2 direction = (targetPosition - Positions[0]).normalized;
                for (int i = 1; i < Positions.Length; i++)
                    Positions[i] = Positions[i - 1] + direction * BonesLength[i - 1];
            }
            else
            {
                // Snap back toward rest pose
                for (int i = 0; i < Positions.Length - 1; i++)
                    Positions[i + 1] = Vector2.Lerp(
                        Positions[i + 1],
                        Positions[i] + StartDirectionSucc[i],
                        SnapBackStrength);

                for (int iteration = 0; iteration < Iterations; iteration++)
                {
                    // Backward pass (tip → root)
                    for (int i = Positions.Length - 1; i > 0; i--)
                    {
                        Positions[i] = (i == Positions.Length - 1)
                            ? targetPosition
                            : Positions[i + 1] + (Positions[i] - Positions[i + 1]).normalized * BonesLength[i];
                    }

                    // Forward pass (root → tip) with angle clamping
                    for (int i = 1; i < Positions.Length; i++)
                    {
                        Vector2 rawDir = Positions[i] - Positions[i - 1];
                        if (rawDir.sqrMagnitude < 0.00001f) rawDir = StartDirectionSucc[i - 1];
                        rawDir = rawDir.normalized;

                        int ci = i - 1; // constraint index: 0 = root-side segment
                        if (Constraints != null && ci < Constraints.Length)
                        {
                            // Parent bone direction: rest dir for the first segment, previous segment for the rest
                            Vector2 parentDir = (i == 1)
                                ? StartDirectionSucc[0].normalized
                                : (Positions[i - 1] - Positions[i - 2]).normalized;

                            float angle   = Vector2.SignedAngle(parentDir, rawDir);
                            float clamped = Mathf.Clamp(angle, Constraints[ci].MinAngle, Constraints[ci].MaxAngle);

                            if (!Mathf.Approximately(angle, clamped))
                            {
                                float rad = clamped * Mathf.Deg2Rad;
                                float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
                                rawDir = new Vector2(
                                    parentDir.x * cos - parentDir.y * sin,
                                    parentDir.x * sin + parentDir.y * cos);
                            }
                        }

                        Positions[i] = Positions[i - 1] + rawDir * BonesLength[i - 1];
                    }

                    if ((Positions[Positions.Length - 1] - targetPosition).sqrMagnitude < Delta * Delta)
                        break;
                }
            }

            // Pole / elbow constraint — bias every middle joint toward the pole
            if (IsElbowRequired && Pole != null && Positions.Length > 2)
            {
                Vector2 rootPos    = Positions[0];
                Vector2 tipPos     = Positions[Positions.Length - 1];
                Vector2 polePos    = GetPositionRootSpace(Pole);

                Vector2 rootTipDir = (tipPos - rootPos);
                float   rootTipLen = rootTipDir.magnitude;
                if (rootTipLen > 0.0001f)
                {
                    rootTipDir /= rootTipLen;
                    Vector2 perp    = new Vector2(-rootTipDir.y, rootTipDir.x);
                    float   poleSide = Vector2.Dot(polePos - rootPos, perp);

                    for (int i = 1; i < Positions.Length - 1; i++)
                    {
                        Vector2 toJoint = Positions[i] - rootPos;
                        float   along   = Vector2.Dot(toJoint, rootTipDir);
                        float   side    = Vector2.Dot(toJoint, perp);
                        float   dist    = Mathf.Abs(side);

                        // Snap to pole's side, preserving perpendicular distance
                        float newSide = poleSide >= 0f ? dist : -dist;
                        Positions[i] = rootPos + rootTipDir * along + perp * newSide;
                    }
                }
            }

            // Apply positions and rotations
            for (int i = 0; i < Positions.Length; i++)
            {
                if (i == Positions.Length - 1)
                {
                    SetRotationRootSpace(Bones[i],
                        Quaternion.Inverse(targetRotation) *
                        StartRotationTarget *
                        Quaternion.Inverse(StartRotationBone[i]));
                }
                else
                {
                    Vector2 dir        = Positions[i + 1] - Positions[i];
                    float   angle      = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    Vector2 startDir   = StartDirectionSucc[i];
                    float   startAngle = Mathf.Atan2(startDir.y, startDir.x) * Mathf.Rad2Deg;

                    SetRotationRootSpace(Bones[i],
                        Quaternion.Euler(0f, 0f, angle - startAngle) *
                        Quaternion.Inverse(StartRotationBone[i]));
                }

                SetPositionRootSpace(Bones[i], Positions[i]);
            }
        }

        // ── Root-space helpers ────────────────────────────────────────────────

        private Vector2 GetPositionRootSpace(Transform current)
        {
            if (Root == null) return current.position;
            return Quaternion.Inverse(Root.rotation) * (current.position - Root.position);
        }

        private void SetPositionRootSpace(Transform current, Vector2 position)
        {
            if (Root == null) current.position = position;
            else              current.position  = Root.rotation * (Vector3)position + Root.position;
        }

        private Quaternion GetRotationRootSpace(Transform current)
        {
            if (Root == null) return current.rotation;
            return Quaternion.Inverse(current.rotation) * Root.rotation;
        }

        private void SetRotationRootSpace(Transform current, Quaternion rotation)
        {
            if (Root == null) current.rotation = rotation;
            else              current.rotation  = Root.rotation * rotation;
        }

        // ── Gizmos ───────────────────────────────────────────────────────────

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            var current = transform;
            for (int i = 0; i < ChainLength && current != null && current.parent != null; i++)
            {
                float dist  = Vector3.Distance(current.position, current.parent.position);
                float scale = dist * 0.1f;
                Handles.matrix = Matrix4x4.TRS(
                    current.position,
                    Quaternion.FromToRotation(Vector3.up, current.parent.position - current.position),
                    new Vector3(scale, dist, scale));
                Handles.color = Color.green;
                Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
                current = current.parent;
            }

            // Pole target hint
            if (IsElbowRequired && Pole != null)
            {
                // Draw a line from the middle bone to the pole
                var mid = transform;
                int half = ChainLength / 2;
                for (int i = 0; i < half && mid != null; i++) mid = mid.parent;
                if (mid != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(mid.position, Pole.position);
                    Gizmos.DrawWireSphere(Pole.position, 0.05f);
                }
            }
#endif
        }
    }
}
