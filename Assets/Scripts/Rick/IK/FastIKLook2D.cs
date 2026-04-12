using UnityEngine;

namespace DitzelGames.FastIK
{
    public class FastIKLook2D : MonoBehaviour
    {
        /// <summary>
        /// Target to look at
        /// </summary>
        public Transform Target;

        /// <summary>
        /// The initial angle offset from the target direction at startup,
        /// so the object preserves its relative orientation.
        /// </summary>
        protected float StartAngleOffset;

        void Awake()
        {
            if (Target == null)
                return;

            // Record the initial angle from this object to the target
            Vector2 initialDir = (Vector2)(Target.position - transform.position);
            float initialAngle = Mathf.Atan2(initialDir.y, initialDir.x) * Mathf.Rad2Deg;

            // Offset = difference between where the object is pointing and the target direction
            StartAngleOffset = transform.eulerAngles.z - initialAngle;
        }

        void Update()
        {
            if (Target == null)
                return;

            // Compute angle toward target in 2D and apply the initial offset
            Vector2 dir = (Vector2)(Target.position - transform.position);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + StartAngleOffset);
        }
    }
}
