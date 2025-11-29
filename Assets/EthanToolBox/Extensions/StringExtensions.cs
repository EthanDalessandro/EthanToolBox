using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Formats the string with the specified color for Unity Console (Rich Text).
        /// </summary>
        public static string Color(this string text, Color color)
        {
            string hex = ColorUtility.ToHtmlStringRGBA(color);
            return $"<color=#{hex}>{text}</color>";
        }

        /// <summary>
        /// Formats the string to be bold.
        /// </summary>
        public static string Bold(this string text)
        {
            return $"<b>{text}</b>";
        }

        /// <summary>
        /// Formats the string to be italic.
        /// </summary>
        public static string Italic(this string text)
        {
            return $"<i>{text}</i>";
        }
        
        /// <summary>
        /// Truncates the string to a maximum length and adds "..." if truncated.
        /// </summary>
        public static string Truncate(this string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Parses the string to an int. Returns default value if parsing fails.
        /// </summary>
        public static int ToInt(this string text, int defaultValue = 0)
        {
            if (int.TryParse(text, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Parses the string to a float. Returns default value if parsing fails.
        /// </summary>
        public static float ToFloat(this string text, float defaultValue = 0f)
        {
            if (float.TryParse(text, out float result))
            {
                return result;
            }
            return defaultValue;
        }
    }
}
