using NodaTime;
using PlayerActivities.Models.Enumerations;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlayerActivities.Models
{
    /// <summary>
    /// Represents a single recorded activity related to a game,
    /// including its timestamp, type, value, and optional elements.
    /// </summary>
    public class Activity
    {
        private DateTime _dateActivity = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// Gets or sets the date and time when the activity occurred.
        /// The value is internally stored in UTC and returned in the local time zone.
        /// When setting the value, it is automatically converted to UTC.
        /// </summary>
        public DateTime DateActivity
        {
            get => _dateActivity.ToLocalTime();
            set => _dateActivity = value.ToUniversalTime();
        }

        /// <summary>
        /// The type of activity (e.g., playtime, achievement, screenshot).
        /// </summary>
        public ActivityType Type { get; set; }

        /// <summary>
        /// A numeric value associated with the activity (e.g., playtime in minutes, number of achievements).
        /// </summary>
        public ulong Value { get; set; }

        /// <summary>
        /// A human-readable string representing how long ago the activity occurred (e.g., "1 year, 2 months").
        /// Not serialized.
        /// </summary>
        [DontSerialize]
        public string TimeAgo
        {
            get
            {
                var now = LocalDateTime.FromDateTime(DateTime.Now);
                var then = LocalDateTime.FromDateTime(DateActivity);
                var period = Period.Between(then, now, PeriodUnits.YearMonthDay);

                var sb = new StringBuilder();

                if (period.Years > 0)
                {
                    sb.AppendFormat(
                        ResourceProvider.GetString(period.Years == 1 ? "LOCCommonYear" : "LOCCommonYears"),
                        period.Years
                    );
                }

                if (period.Months > 0)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.AppendFormat(
                        ResourceProvider.GetString(period.Months == 1 ? "LOCCommonMonth" : "LOCCommonMonths"),
                        period.Months
                    );
                }

                if (period.Days > 0)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.AppendFormat(
                        ResourceProvider.GetString(period.Days == 1 ? "LOCCommonDay" : "LOCCommonDays"),
                        period.Days
                    );
                }

                return sb.ToString();
            }
        }
    }
}