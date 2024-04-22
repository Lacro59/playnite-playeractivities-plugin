using CommonPluginsShared.Converters;
using CommonPluginsShared.Extensions;
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
                int years = DateActivity.GetYearsBetween();
                int months = DateActivity.GetMonthsBetween();
                int days = DateActivity.GetDaysBetween();

                //if (years > 0)
                //{
                //    return years == 1 ? string.Format(ResourceProvider.GetString("LOCPaYearAgo"), years) : string.Format(ResourceProvider.GetString("LOCPaYearsAgo"), years);
                //}
                if (months > 0)
                {
                    return months == 1 ? string.Format(ResourceProvider.GetString("LOCPaMonthAgo"), months) : string.Format(ResourceProvider.GetString("LOCPaMonthsAgo"), months);
                }
                if (days >= 0)
                {
                    return days >= 0 ? string.Format(ResourceProvider.GetString("LOCPaDayAgo"), days) : string.Format(ResourceProvider.GetString("LOCPaDaysAgo"), days);
                }

                return string.Empty;
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
