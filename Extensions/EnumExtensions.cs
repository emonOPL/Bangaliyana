using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Bangaliyana.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the display name from Display attribute, or falls back to formatted enum name with spaces
        /// </summary>
        public static string GetDisplayName(this Enum enumValue)
        {
            var displayAttr = enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>();

            if (displayAttr?.Name != null)
                return displayAttr.Name;

            // Fall back to adding spaces between PascalCase words
            return FormatEnumName(enumValue.ToString());
        }

        /// <summary>
        /// Converts PascalCase enum name to words with spaces (e.g., "WaitingForCustomer" -> "Waiting For Customer")
        /// </summary>
        public static string ToDisplayString(this Enum enumValue)
        {
            return FormatEnumName(enumValue.ToString());
        }

        /// <summary>
        /// Static helper method to format any string from PascalCase to words with spaces
        /// </summary>
        public static string FormatEnumName(string enumName)
        {
            if (string.IsNullOrEmpty(enumName))
                return enumName;

            return Regex.Replace(enumName, "([a-z])([A-Z])", "$1 $2");
        }
    }

    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Formats a PascalCase string to human-readable format with spaces
        /// e.g., "WrongItem" -> "Wrong Item", "NotAsDescribed" -> "Not As Described"
        /// </summary>
        public static string FormatAsDisplayName(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? string.Empty;

            return Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");
        }

        /// <summary>
        /// Formats and truncates a PascalCase string for display
        /// </summary>
        public static string FormatAndTruncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
                return value ?? string.Empty;

            var formatted = value.FormatAsDisplayName();
            return formatted.Length <= maxLength ? formatted : formatted.Substring(0, maxLength) + "...";
        }
    }
}