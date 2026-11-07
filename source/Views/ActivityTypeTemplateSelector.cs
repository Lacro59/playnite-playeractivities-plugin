using PlayerActivities.Models;
using PlayerActivities.Models.Enumerations;
using System.Windows;
using System.Windows.Controls;

namespace PlayerActivities.Views
{
    /// <summary>
    /// Selects a timeline activity <see cref="DataTemplate"/> based on <see cref="Activity.Type"/>.
    /// </summary>
    public class ActivityTypeTemplateSelector : DataTemplateSelector
    {
        /// <summary>Template for <see cref="ActivityType.AchievementsUnlocked"/>.</summary>
        public DataTemplate AchievementsUnlockedTemplate { get; set; }

        /// <summary>Template for <see cref="ActivityType.AchievementsGoal"/>.</summary>
        public DataTemplate AchievementsGoalTemplate { get; set; }

        /// <summary>Template for <see cref="ActivityType.ScreenshotsTaken"/>.</summary>
        public DataTemplate ScreenshotsTakenTemplate { get; set; }

        /// <summary>Template for <see cref="ActivityType.PlaytimeFirst"/>.</summary>
        public DataTemplate PlaytimeFirstTemplate { get; set; }

        /// <summary>Template for <see cref="ActivityType.PlaytimeGoal"/>.</summary>
        public DataTemplate PlaytimeGoalTemplate { get; set; }

        /// <summary>Template for <see cref="ActivityType.HowLongToBeatCompleted"/>.</summary>
        public DataTemplate HowLongToBeatCompletedTemplate { get; set; }

        /// <inheritdoc />
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Activity activity = item as Activity;
            if (activity == null)
            {
                return base.SelectTemplate(item, container);
            }

            switch (activity.Type)
            {
                case ActivityType.AchievementsUnlocked:
                    return AchievementsUnlockedTemplate ?? base.SelectTemplate(item, container);
                case ActivityType.AchievementsGoal:
                    return AchievementsGoalTemplate ?? base.SelectTemplate(item, container);
                case ActivityType.ScreenshotsTaken:
                    return ScreenshotsTakenTemplate ?? base.SelectTemplate(item, container);
                case ActivityType.PlaytimeFirst:
                    return PlaytimeFirstTemplate ?? base.SelectTemplate(item, container);
                case ActivityType.PlaytimeGoal:
                    return PlaytimeGoalTemplate ?? base.SelectTemplate(item, container);
                case ActivityType.HowLongToBeatCompleted:
                    return HowLongToBeatCompletedTemplate ?? base.SelectTemplate(item, container);
                default:
                    return base.SelectTemplate(item, container);
            }
        }
    }
}
