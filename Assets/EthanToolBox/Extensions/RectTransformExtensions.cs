using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class RectTransformExtensions
    {
        /// <summary>
        /// Sets the width of the RectTransform via sizeDelta.
        /// </summary>
        public static void SetWidth(this RectTransform rt, float width)
        {
            rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        }

        /// <summary>
        /// Sets the height of the RectTransform via sizeDelta.
        /// </summary>
        public static void SetHeight(this RectTransform rt, float height)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        }

        /// <summary>
        /// Sets the size of the RectTransform.
        /// </summary>
        public static void SetSize(this RectTransform rt, float width, float height)
        {
            rt.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// Sets the anchor min and max to the same value.
        /// </summary>
        public static void SetAnchor(this RectTransform rt, float x, float y)
        {
            rt.anchorMin = new Vector2(x, y);
            rt.anchorMax = new Vector2(x, y);
        }

        /// <summary>
        /// Sets the pivot of the RectTransform.
        /// </summary>
        public static void SetPivot(this RectTransform rt, float x, float y)
        {
            rt.pivot = new Vector2(x, y);
        }
    }
}
