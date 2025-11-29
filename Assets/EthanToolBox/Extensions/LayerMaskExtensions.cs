using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class LayerMaskExtensions
    {
        /// <summary>
        /// Checks if the LayerMask contains the specified layer.
        /// </summary>
        public static bool Contains(this LayerMask mask, int layer)
        {
            return mask == (mask | (1 << layer));
        }
    }
}
