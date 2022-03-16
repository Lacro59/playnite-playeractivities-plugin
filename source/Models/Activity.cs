using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models
{
    public class Activity
    {
        private static IResourceProvider resources = new ResourceProvider();


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

                if (years > 0)
                {
                    return years == 1 ? string.Format(resources.GetString("LOCPaYearAgo"), years) : string.Format(resources.GetString("LOCPaYearsAgo"), years);
                }
                if (months > 0)
                {
                    return months == 1 ? string.Format(resources.GetString("LOCPaMonthAgo"), months) : string.Format(resources.GetString("LOCPaMonthsAgo"), months);
                }
                if (days > 0)
                {
                    return days == 1 ? string.Format(resources.GetString("LOCPaDayAgo"), days) : string.Format(resources.GetString("LOCPaDaysAgo"), days);
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
