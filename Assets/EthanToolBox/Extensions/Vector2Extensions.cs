using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class Vector2Extensions
    {
        /// <summary>
        /// Returns a new Vector2 with the specified X value.
        /// </summary>
        public static Vector2 WithX(this Vector2 vector, float x)
        {
            return new Vector2(x, vector.y);
        }

        /// <summary>
        /// Returns a new Vector2 with the specified Y value.
        /// </summary>
        public static Vector2 WithY(this Vector2 vector, float y)
        {
            return new Vector2(vector.x, y);
        }

        /// <summary>
        /// Returns the direction from this vector to the target.
        /// </summary>
        public static Vector2 DirectionTo(this Vector2 source, Vector2 target)
        {
            return (target - source).normalized;
        }

        /// <summary>
        /// Returns the distance to the target.
        /// </summary>
        public static float DistanceTo(this Vector2 source, Vector2 target)
        {
            return Vector2.Distance(source, target);
        }
    }
}
