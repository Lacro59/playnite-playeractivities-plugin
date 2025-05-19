using NodaTime;
using PlayerActivities.Models.Enumerations;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

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
        /// The date and time when the activity occurred, in UTC.
        /// Defaults to the current UTC time.
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
                Period period = Period.Between(LocalDateTime.FromDateTime(DateActivity), LocalDateTime.FromDateTime(DateTime.Now), PeriodUnits.YearMonthDay);

                int years = period.Years;
                int months = period.Months;
                int days = period.Days;

                string result = "";

                if (years > 0)
                {
                    result += years == 1 ?
                        string.Format(ResourceProvider.GetString("LOCCommonYear"), years) :
                        string.Format(ResourceProvider.GetString("LOCCommonYears"), years);
                }

                if (months > 0)
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        result += ", ";
                    }
                    result += months == 1 ?
                        string.Format(ResourceProvider.GetString("LOCCommonMonth"), months) :
                        string.Format(ResourceProvider.GetString("LOCCommonMonths"), months);
                }

                if (days > 0)
                {
                    if (!string.IsNullOrEmpty(result))
                    {
                        result += ", ";
                    }
                    result += days == 1 ?
                        string.Format(ResourceProvider.GetString("LOCCommonDay"), days) :
                        string.Format(ResourceProvider.GetString("LOCCommonDays"), days);
                }

                return result;
            }
        }

        /// <summary>
        /// A list of additional detailed elements related to the activity (e.g., session data, metadata).
        /// </summary>
        public List<ActivityElement> ActivityElements { get; set; } = new List<ActivityElement>();
    }
}