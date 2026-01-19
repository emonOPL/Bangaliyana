using System;

namespace Bangaliyana.Extensions
{
    public static class DateTimeExtensions
    {
        // Bangladesh Standard Time is UTC+6
        private static readonly TimeZoneInfo BangladeshTimeZone = TimeZoneInfo.CreateCustomTimeZone(
            "Bangladesh Standard Time",
            TimeSpan.FromHours(6),
            "Bangladesh Standard Time",
            "Bangladesh Standard Time"
        );

        // Default format: dd MMMM yyyy, hh:mm:ss tt
        private const string DefaultFormat = "dd MMMM yyyy, hh:mm:ss tt";
        private const string DateOnlyFormat = "dd MMMM yyyy";
        private const string TimeOnlyFormat = "hh:mm:ss tt";
        private const string ShortDateFormat = "dd MMM yyyy";
        private const string ShortDateTimeFormat = "dd MMM yyyy, hh:mm tt";

        /// <summary>
        /// Converts UTC DateTime to Bangladesh Standard Time
        /// </summary>
        public static DateTime ToBangladeshTime(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind == DateTimeKind.Local)
            {
                utcDateTime = utcDateTime.ToUniversalTime();
            }
            else if (utcDateTime.Kind == DateTimeKind.Unspecified)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, BangladeshTimeZone);
        }

        /// <summary>
        /// Formats DateTime to Bangladesh Standard Time with full format (dd MMMM yyyy, hh:mm:ss AM/PM)
        /// </summary>
        public static string ToBangladeshString(this DateTime dateTime)
        {
            var bdTime = dateTime.ToBangladeshTime();
            return bdTime.ToString(DefaultFormat);
        }

        /// <summary>
        /// Formats DateTime to Bangladesh Standard Time with custom format
        /// </summary>
        public static string ToBangladeshString(this DateTime dateTime, string format)
        {
            var bdTime = dateTime.ToBangladeshTime();
            return bdTime.ToString(format);
        }

        /// <summary>
        /// Formats DateTime to Bangladesh date only (dd MMMM yyyy)
        /// </summary>
        public static string ToBangladeshDateString(this DateTime dateTime)
        {
            var bdTime = dateTime.ToBangladeshTime();
            return bdTime.ToString(DateOnlyFormat);
        }

        /// <summary>
        /// Formats DateTime to Bangladesh time only (hh:mm:ss AM/PM)
        /// </summary>
        public static string ToBangladeshTimeString(this DateTime dateTime)
        {
            var bdTime = dateTime.ToBangladeshTime();
            return bdTime.ToString(TimeOnlyFormat);
        }

        /// <summary>
        /// Formats DateTime to short Bangladesh date (dd MMM yyyy)
        /// </summary>
        public static string ToBangladeshShortDateString(this DateTime dateTime)
        {
            var bdTime = dateTime.ToBangladeshTime();
            return bdTime.ToString(ShortDateFormat);
        }

        /// <summary>
        /// Formats DateTime to short Bangladesh datetime (dd MMM yyyy, hh:mm AM/PM)
        /// </summary>
        public static string ToBangladeshShortString(this DateTime dateTime)
        {
            var bdTime = dateTime.ToBangladeshTime();
            return bdTime.ToString(ShortDateTimeFormat);
        }

        /// <summary>
        /// Gets current Bangladesh time
        /// </summary>
        public static DateTime GetBangladeshNow()
        {
            return DateTime.UtcNow.ToBangladeshTime();
        }

        /// <summary>
        /// Formats current Bangladesh time
        /// </summary>
        public static string GetBangladeshNowString()
        {
            return GetBangladeshNow().ToString(DefaultFormat);
        }

        /// <summary>
        /// For nullable DateTime
        /// </summary>
        public static string ToBangladeshString(this DateTime? dateTime, string defaultValue = "-")
        {
            return dateTime.HasValue ? dateTime.Value.ToBangladeshString() : defaultValue;
        }

        /// <summary>
        /// For nullable DateTime with custom format
        /// </summary>
        public static string ToBangladeshString(this DateTime? dateTime, string format, string defaultValue = "-")
        {
            return dateTime.HasValue ? dateTime.Value.ToBangladeshString(format) : defaultValue;
        }
    }
}
