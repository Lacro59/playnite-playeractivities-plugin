using CommonPluginsShared;
using PlayerActivities.Controls;
using PlayerActivities.Models.Enumerations;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;

namespace PlayerActivities.Models
{
    /// <summary>
    /// Represents a single activity with game context.
    /// </summary>
    public class ActivityList : Activity
    {
        /// <summary>
        /// The game this activity belongs to.
        /// </summary>
        public Game GameContext { get; set; }
    }

    /// <summary>
    /// Represents a group of activities associated with a single game.
    /// Display fields are populated once via <see cref="FinalizeDisplayCaches"/> after aggregation.
    /// </summary>
    public class ActivityListGrouped
    {
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        /// <summary>
        /// The game associated with this activity group.
        /// </summary>
        public Game GameContext { get; set; }

        /// <summary>
        /// Localized display date string.
        /// </summary>
        public string DtString { get; set; }

        /// <summary>
        /// Human-readable time ago string (e.g., "2 days ago").
        /// </summary>
        public string TimeAgo { get; set; }

        /// <summary>
        /// Cached store / library source name for the game.
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// Icon representing the game's source (e.g., Steam, Epic).
        /// </summary>
        public string SourceIcon { get; set; }

        /// <summary>
        /// Full path to the game's cover image if available.
        /// </summary>
        public string CoverImage { get; set; }

        /// <summary>
        /// Activities ordered by date descending and type ascending (set by aggregation).
        /// </summary>
        public List<Activity> Activities { get; set; } = new List<Activity>();

        /// <summary>
        /// Binding alias for <see cref="Activities"/> (already sorted by <c>GetActivitiesData</c>).
        /// </summary>
        public List<Activity> ActivitiesOrdered => Activities;

        /// <summary>
        /// Whether to show HowLongToBeat menu.
        /// </summary>
        public bool ShowHowLongToBeatMenu { get; set; }

        /// <summary>
        /// Whether to show SuccessStory menu.
        /// </summary>
        public bool ShowSuccessStoryMenu { get; set; }

        /// <summary>
        /// Whether to show ScreenshotsVisualizer menu.
        /// </summary>
        public bool ShowScreenshotsVisualizerMenu { get; set; }

        /// <summary>
        /// Fills cover path, source icon, and context-menu visibility flags once after grouping.
        /// </summary>
        public void FinalizeDisplayCaches()
        {
            if (string.IsNullOrEmpty(SourceName) && GameContext != null)
            {
                SourceName = PlayniteTools.GetSourceName(GameContext);
            }

            SourceIcon = string.IsNullOrEmpty(SourceName) ? string.Empty : TransformIcon.Get(SourceName);
            CoverImage = GameContext?.CoverImage == null
                ? string.Empty
                : API.Instance.Database.GetFullFilePath(GameContext.CoverImage);

            ShowHowLongToBeatMenu = HowLongToBeatPlugin.IsInstalled;

            PlayerActivitiesData data = GameContext == null ? null : PluginDatabase.Get(GameContext.Id);
            bool hasSuccessStory = false;
            bool hasScreenshots = false;

            if (data?.Items != null)
            {
                foreach (Activity item in data.Items)
                {
                    if (item.Type == ActivityType.AchievementsGoal || item.Type == ActivityType.AchievementsUnlocked)
                    {
                        hasSuccessStory = true;
                    }
                    else if (item.Type == ActivityType.ScreenshotsTaken)
                    {
                        hasScreenshots = true;
                    }

                    if (hasSuccessStory && hasScreenshots)
                    {
                        break;
                    }
                }
            }

            ShowSuccessStoryMenu = hasSuccessStory && SuccessStoryPlugin.IsInstalled;
            ShowScreenshotsVisualizerMenu = hasScreenshots && ScreenshotsVisualizerPlugin.IsInstalled;
        }
    }
}
