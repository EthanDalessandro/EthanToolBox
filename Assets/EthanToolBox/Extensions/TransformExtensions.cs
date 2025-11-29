using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class TransformExtensions
    {
        /// <summary>
        /// Resets position, rotation, and scale to identity.
        /// </summary>
        public static void Reset(this Transform transform)
        {
            transform.position = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Destroys all child objects of this transform.
        /// </summary>
        public static void DestroyChildren(this Transform transform)
        {
            foreach (Transform child in transform)
            {
                Object.Destroy(child.gameObject);
            }
        }
        
        /// <summary>
        /// Destroys all child objects immediately (Editor only).
        /// </summary>
        public static void DestroyChildrenImmediate(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Rotates the transform to look at the target in 2D (Z-axis rotation).
        /// </summary>
        public static void LookAt2D(this Transform transform, Vector3 target)
        {
            Vector3 dir = target - transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        /// <summary>
        /// Sets the X position of the transform.
        /// </summary>
        public static void SetPositionX(this Transform transform, float x)
        {
            transform.position = new Vector3(x, transform.position.y, transform.position.z);
        }

        /// <summary>
        /// Sets the Y position of the transform.
        /// </summary>
        public static void SetPositionY(this Transform transform, float y)
        {
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
        }

        /// <summary>
        /// Sets the Z position of the transform.
        /// </summary>
        public static void SetPositionZ(this Transform transform, float z)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, z);
        }

        /// <summary>
        /// Sets a uniform scale for the transform.
        /// </summary>
        public static void SetLocalScale(this Transform transform, float scale)
        {
            transform.localScale = Vector3.one * scale;
        }
    }
}
