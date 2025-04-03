using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
using NodaTime;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models
{
    public class Activity
    {
        public DateTime DateActivity { get; set; } = DateTime.Now.ToUniversalTime();
        public ActivityType Type { get; set; }

        public ulong Value { get; set; }

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


        public List<ActivityElement> ActivityElements { get; set; } = new List<ActivityElement>();
    }


    public enum ActivityType
    {
        HowLongToBeatCompleted,
        AchievementsGoal, AchievementsUnlocked,
        ScreenshotsTaked,
        PlaytimeGoal, PlaytimeFirst
    }
}
