#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace DitzelGames.FastIK
{
    /// <summary>
    /// FABRIK IK Solver — 2D version.
    /// All positions are solved in the XY plane.
    /// Rotations are applied around the Z axis only.
    /// </summary>
    public class FastIKFabric2D : MonoBehaviour
    {
        /// <summary>
        /// Chain length of bones
        /// </summary>
        public int ChainLength = 2;

        /// <summary>
        /// Target the chain should reach toward
        /// </summary>
        public Transform Target;

        /// <summary>
        /// Pole target used to control which side the chain bends toward
        /// </summary>
        public Transform Pole;

        [Header("Solver Parameters")]
        public int Iterations = 10;
        public float Delta = 0.001f;

        [Range(0, 1)]
        public float SnapBackStrength = 1f;

        protected float[] BonesLength;       // Length of each bone segment (tip → root order)
        protected float CompleteLength;
        protected Transform[] Bones;
        protected Vector2[] Positions;       // 2D positions in root-local space
        protected Vector2[] StartDirectionSucc;
        protected Quaternion[] StartRotationBone;
        protected Quaternion StartRotationTarget;
        protected Transform Root;

        void Awake()
        {
            Init();
        }

        void Init()
        {
            Bones             = new Transform[ChainLength + 1];
            Positions         = new Vector2[ChainLength + 1];
            BonesLength       = new float[ChainLength];
            StartDirectionSucc = new Vector2[ChainLength + 1];
            StartRotationBone = new Quaternion[ChainLength + 1];

            // Walk up to find the root ancestor
            Root = transform;
            for (int i = 0; i <= ChainLength; i++)
            {
                if (Root == null)
                    throw new UnityException("ChainLength exceeds the ancestor hierarchy!");
                Root = Root.parent;
            }

            // Auto-create a target if none is assigned
            if (Target == null)
            {
                Target = new GameObject(gameObject.name + " Target").transform;
                Target.position = transform.position;
            }
            StartRotationTarget = GetRotationRootSpace(Target);

            // Populate bone chain from tip (this transform) to root
            var current = transform;
            CompleteLength = 0;
            for (int i = Bones.Length - 1; i >= 0; i--)
            {
                Bones[i]             = current;
                StartRotationBone[i] = GetRotationRootSpace(current);

                if (i == Bones.Length - 1)
                {
                    // Leaf bone: direction toward target
                    StartDirectionSucc[i] = GetPositionRootSpace(Target) - GetPositionRootSpace(current);
                }
                else
                {
                    // Mid bone: direction toward child bone
                    StartDirectionSucc[i]  = GetPositionRootSpace(Bones[i + 1]) - GetPositionRootSpace(current);
                    BonesLength[i]         = StartDirectionSucc[i].magnitude;
                    CompleteLength        += BonesLength[i];
                }

                current = current.parent;
            }
        }

        void LateUpdate()
        {
            ResolveIK();
        }

        private void ResolveIK()
        {
            if (Target == null) return;
            if (BonesLength.Length != ChainLength) Init();

            // Snapshot current bone positions in root space
            for (int i = 0; i < Bones.Length; i++)
                Positions[i] = GetPositionRootSpace(Bones[i]);

            Vector2 targetPosition = GetPositionRootSpace(Target);
            Quaternion targetRotation = GetRotationRootSpace(Target);

            // ── FABRIK ──────────────────────────────────────────────────
            if ((targetPosition - Positions[0]).sqrMagnitude >= CompleteLength * CompleteLength)
            {
                // Target is out of reach — stretch the chain straight toward it
                Vector2 direction = (targetPosition - Positions[0]).normalized;
                for (int i = 1; i < Positions.Length; i++)
                    Positions[i] = Positions[i - 1] + direction * BonesLength[i - 1];
            }
            else
            {
                // Snap-back toward rest pose
                for (int i = 0; i < Positions.Length - 1; i++)
                    Positions[i + 1] = Vector2.Lerp(Positions[i + 1], Positions[i] + StartDirectionSucc[i], SnapBackStrength);

                for (int iteration = 0; iteration < Iterations; iteration++)
                {
                    // Backward pass (tip → root)
                    for (int i = Positions.Length - 1; i > 0; i--)
                    {
                        if (i == Positions.Length - 1)
                            Positions[i] = targetPosition;
                        else
                            Positions[i] = Positions[i + 1] + (Positions[i] - Positions[i + 1]).normalized * BonesLength[i];
                    }

                    // Forward pass (root → tip)
                    for (int i = 1; i < Positions.Length; i++)
                        Positions[i] = Positions[i - 1] + (Positions[i] - Positions[i - 1]).normalized * BonesLength[i - 1];

                    if ((Positions[Positions.Length - 1] - targetPosition).sqrMagnitude < Delta * Delta)
                        break;
                }
            }

            // ── POLE TARGET ─────────────────────────────────────────────
            // In 2D the pole is a point that defines which side the chain bends toward.
            // For each mid-bone we compute the signed angle from the current joint direction
            // to the pole direction, then rotate the joint to align with the pole side.
            if (Pole != null)
            {
                Vector2 polePosition = GetPositionRootSpace(Pole);

                for (int i = 1; i < Positions.Length - 1; i++)
                {
                    Vector2 segmentDir  = Positions[i + 1] - Positions[i - 1]; // overall limb direction
                    Vector2 toJoint     = Positions[i]     - Positions[i - 1];
                    Vector2 toPole      = polePosition      - Positions[i - 1];

                    // Project both vectors onto the axis perpendicular to the limb direction
                    // (equivalent to the plane-projection in the 3D version)
                    Vector2 perpAxis = new Vector2(-segmentDir.y, segmentDir.x).normalized;
                    float jointSide  = Vector2.Dot(toJoint, perpAxis);
                    float poleSide   = Vector2.Dot(toPole,  perpAxis);

                    // Signed angle from the current joint position to the pole side
                    float signedAngle = Vector2.SignedAngle(toJoint, toPole);

                    // Only apply if the joint and pole are on opposite sides, 
                    // or if we want to enforce the pole's side direction
                    if (Mathf.Sign(jointSide) != Mathf.Sign(poleSide))
                    {
                        Positions[i] = Positions[i - 1] +
                            (Vector2)(Quaternion.Euler(0f, 0f, signedAngle) * (Vector3)toJoint);
                    }
                }
            }

            // ── APPLY POSITIONS AND ROTATIONS ────────────────────────────
            for (int i = 0; i < Positions.Length; i++)
            {
                if (i == Positions.Length - 1)
                {
                    // Leaf bone: match the target rotation
                    SetRotationRootSpace(Bones[i],
                        Quaternion.Inverse(targetRotation) *
                        StartRotationTarget *
                        Quaternion.Inverse(StartRotationBone[i]));
                }
                else
                {
                    // Mid / root bone: point toward the next bone in the chain
                    Vector2 dir = Positions[i + 1] - Positions[i];
                    float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    Vector2 startDir   = StartDirectionSucc[i];
                    float   startAngle = Mathf.Atan2(startDir.y, startDir.x) * Mathf.Rad2Deg;
                    float   deltaAngle = angle - startAngle;

                    SetRotationRootSpace(Bones[i],
                        Quaternion.Euler(0f, 0f, deltaAngle) *
                        Quaternion.Inverse(StartRotationBone[i]));
                }

                SetPositionRootSpace(Bones[i], Positions[i]);
            }
        }

        // ── ROOT-SPACE HELPERS ───────────────────────────────────────────

        private Vector2 GetPositionRootSpace(Transform current)
        {
            if (Root == null)
                return current.position;
            return Quaternion.Inverse(Root.rotation) * (current.position - Root.position);
        }

        private void SetPositionRootSpace(Transform current, Vector2 position)
        {
            if (Root == null)
                current.position = position;
            else
                current.position = Root.rotation * (Vector3)position + Root.position;
        }

        private Quaternion GetRotationRootSpace(Transform current)
        {
            if (Root == null)
                return current.rotation;
            return Quaternion.Inverse(current.rotation) * Root.rotation;
        }

        private void SetRotationRootSpace(Transform current, Quaternion rotation)
        {
            if (Root == null)
                current.rotation = rotation;
            else
                current.rotation = Root.rotation * rotation;
        }

        // ── GIZMOS ───────────────────────────────────────────────────────
        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            var current = this.transform;
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
#endif
        }
    }
}
