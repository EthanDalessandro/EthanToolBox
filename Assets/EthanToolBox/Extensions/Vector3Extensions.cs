using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class Vector3Extensions
    {
        /// <summary>
        /// Returns a new Vector3 with the specified X value.
        /// </summary>
        public static Vector3 WithX(this Vector3 vector, float x)
        {
            return new Vector3(x, vector.y, vector.z);
        }

        /// <summary>
        /// Returns a new Vector3 with the specified Y value.
        /// </summary>
        public static Vector3 WithY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, y, vector.z);
        }

        /// <summary>
        /// Returns a new Vector3 with the specified Z value.
        /// </summary>
        public static Vector3 WithZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }

        /// <summary>
        /// Returns the vector with y = 0. Useful for top-down logic.
        /// </summary>
        public static Vector3 Flat(this Vector3 vector)
        {
            return new Vector3(vector.x, 0, vector.z);
        }

        /// <summary>
        /// Returns the direction from this vector to the target.
        /// </summary>
        public static Vector3 DirectionTo(this Vector3 source, Vector3 target)
        {
            return (target - source).normalized;
        }
        
        /// <summary>
        /// Returns the distance to the target.
        /// </summary>
        public static float DistanceTo(this Vector3 source, Vector3 target)
        {
            return Vector3.Distance(source, target);
        }

        /// <summary>
        /// Adds to the X value of the vector.
        /// </summary>
        public static Vector3 AddX(this Vector3 vector, float x)
        {
            return new Vector3(vector.x + x, vector.y, vector.z);
        }

        /// <summary>
        /// Adds to the Y value of the vector.
        /// </summary>
        public static Vector3 AddY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, vector.y + y, vector.z);
        }

        /// <summary>
        /// Adds to the Z value of the vector.
        /// </summary>
        public static Vector3 AddZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, vector.z + z);
        }

        /// <summary>
        /// Finds the closest position from a list of positions.
        /// </summary>
        public static Vector3 Closest(this Vector3 position, System.Collections.Generic.IEnumerable<Vector3> otherPositions)
        {
            Vector3 closest = Vector3.zero;
            float minDistance = float.MaxValue;

            foreach (var other in otherPositions)
            {
                float distance = Vector3.SqrMagnitude(position - other);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = other;
                }
            }
            return closest;
        }
    }
}
