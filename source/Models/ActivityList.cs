using CommonPluginsShared;
using PlayerActivities.Controls;
using PlayerActivities.Models.Enumerations;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.Linq;

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
    /// Provides helpers to retrieve plugin states and game info.
    /// </summary>
    public class ActivityListGrouped
    {
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        /// <summary>
        /// The game associated with this activity group.
        /// </summary>
        public Game GameContext { get; set; }

        /// <summary>
        /// Helper to get plugin data associated with the current game.
        /// </summary>
        private PlayerActivitiesData Data => GameContext == null ? null : PluginDatabase.Get(GameContext.Id);

        /// <summary>
        /// Localized display date string.
        /// </summary>
        public string DtString { get; set; }

        /// <summary>
        /// Human-readable time ago string (e.g., "2 days ago").
        /// </summary>
        public string TimeAgo { get; set; }

        /// <summary>
        /// Icon representing the game's source (e.g., Steam, Epic).
        /// </summary>
        public string SourceIcon => GameContext == null ? string.Empty : TransformIcon.Get(PlayniteTools.GetSourceName(GameContext.Id));

        /// <summary>
        /// Full path to the game's cover image if available.
        /// </summary>
        public string CoverImage => GameContext?.CoverImage == null ? string.Empty : API.Instance.Database.GetFullFilePath(GameContext.CoverImage);

        /// <summary>
        /// All recorded activities.
        /// </summary>
        public List<Activity> Activities { get; set; } = new List<Activity>();

        /// <summary>
        /// Activities ordered by date descending and type ascending.
        /// </summary>
        public List<Activity> ActivitiesOrdered => Activities
            .OrderByDescending(x => x.DateActivity.ToString("yyyy-MM-dd"))
            .ThenBy(x => x.Type)
            .ToList();

        /// <summary>
        /// Indicates whether HowLongToBeat data is available.
        /// </summary>
        public bool HasHowLongToBeat => Data?.Items?.Any(x => x.Type == ActivityType.HowLongToBeatCompleted) == true;

        /// <summary>
        /// Indicates whether HowLongToBeat plugin is installed.
        /// </summary>
        public bool InstalledHowLongToBeat => HowLongToBeatPlugin.IsInstalled;

        /// <summary>
        /// Whether to show HowLongToBeat menu.
        /// </summary>
        public bool ShowHowLongToBeatMenu => InstalledHowLongToBeat;

        /// <summary>
        /// Indicates whether the game has achievement data.
        /// </summary>
        public bool HasSuccessStory => Data?.Items?.Any(x =>
            x.Type == ActivityType.AchievementsGoal || x.Type == ActivityType.AchievementsUnlocked) == true;

        /// <summary>
        /// Indicates whether SuccessStory plugin is installed.
        /// </summary>
        public bool InstalledSuccessStory => SuccessStoryPlugin.IsInstalled;

        /// <summary>
        /// Whether to show SuccessStory menu.
        /// </summary>
        public bool ShowSuccessStoryMenu => HasSuccessStory && InstalledSuccessStory;

        /// <summary>
        /// Indicates whether the game has screenshot data.
        /// </summary>
        public bool HasScreenshotsVisualizer => Data?.Items?.Any(x => x.Type == ActivityType.ScreenshotsTaken) == true;

        /// <summary>
        /// Indicates whether ScreenshotsVisualizer plugin is installed.
        /// </summary>
        public bool InstalledScreenshotsVisualizer => ScreenshotsVisualizerPlugin.IsInstalled;

        /// <summary>
        /// Whether to show ScreenshotsVisualizer menu.
        /// </summary>
        public bool ShowScreenshotsVisualizerMenu => HasScreenshotsVisualizer && InstalledScreenshotsVisualizer;

        /// <summary>
        /// Indicates whether the game has playtime tracking data.
        /// </summary>
        public bool HasPlaytime => Data?.Items?.Any(x =>
            x.Type == ActivityType.PlaytimeFirst || x.Type == ActivityType.PlaytimeGoal) == true;
    }
}