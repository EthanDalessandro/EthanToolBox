using UnityEngine;

namespace EthanToolBox.Core.Extensions
{
    public static class MathExtensions
    {
        /// <summary>
        /// Remaps a value from one range to another.
        /// Example: 5.Remap(0, 10, 0, 100) returns 50.
        /// </summary>
        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        /// <summary>
        /// Snaps the value to the nearest interval.
        /// Example: 1.2f.Snap(0.5f) returns 1.0f. 1.3f.Snap(0.5f) returns 1.5f.
        /// </summary>
        public static float Snap(this float value, float interval)
        {
            return Mathf.Round(value / interval) * interval;
        }

        /// <summary>
        /// Returns true if the integer is even.
        /// </summary>
        public static bool IsEven(this int value)
        {
            return value % 2 == 0;
        }

        /// <summary>
        /// Returns true if the integer is odd.
        /// </summary>
        public static bool IsOdd(this int value)
        {
            return value % 2 != 0;
        }

        /// <summary>
        /// Formats a float (0-1) as a percentage string (e.g., 0.5 -> "50%").
        /// </summary>
        public static string ToPercent(this float value)
        {
            return (value * 100f).ToString("F0") + "%";
        }

        /// <summary>
        /// Formats seconds into a "MM:SS" string.
        /// </summary>
        public static string SecondsToFormattedString(this float seconds)
        {
            int totalSeconds = Mathf.FloorToInt(seconds);
            int m = totalSeconds / 60;
            int s = totalSeconds % 60;
            return $"{m:00}:{s:00}";
        }
    }
}
