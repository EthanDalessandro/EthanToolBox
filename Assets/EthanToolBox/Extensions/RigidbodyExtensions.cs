using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class RigidbodyExtensions
    {
        /// <summary>
        /// Stops the Rigidbody by setting velocity and angular velocity to zero.
        /// </summary>
        public static void Stop(this Rigidbody rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        /// <summary>
        /// Stops the Rigidbody2D by setting velocity and angular velocity to zero.
        /// </summary>
        public static void Stop(this Rigidbody2D rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}
