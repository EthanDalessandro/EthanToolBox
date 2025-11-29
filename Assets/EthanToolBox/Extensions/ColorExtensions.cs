using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class ColorExtensions
    {
        /// <summary>
        /// Returns a new color with the specified alpha value.
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Returns a new color with the specified Red value.
        /// </summary>
        public static Color WithRed(this Color color, float r)
        {
            return new Color(r, color.g, color.b, color.a);
        }

        /// <summary>
        /// Returns a new color with the specified Green value.
        /// </summary>
        public static Color WithGreen(this Color color, float g)
        {
            return new Color(color.r, g, color.b, color.a);
        }

        /// <summary>
        /// Returns a new color with the specified Blue value.
        /// </summary>
        public static Color WithBlue(this Color color, float b)
        {
            return new Color(color.r, color.g, b, color.a);
        }

        /// <summary>
        /// Returns the hex string representation of the color (e.g., "#FF0000").
        /// </summary>
        public static string ToHex(this Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }
    }
}
