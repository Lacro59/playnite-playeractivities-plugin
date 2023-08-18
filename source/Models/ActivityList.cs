using CommonPluginsShared;
using PlayerActivities.Controls;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models
{
    public class ActivityList : Activity
    {
        public Game GameContext { get; set; }
    }


    public class ActivityListGrouped
    {
        private static PlayerActivitiesDatabase PluginDatabase = PlayerActivities.PluginDatabase;
        public Game GameContext { get; set; }

        public string dtString { get; set; }
        public string TimeAgo { get; set; }

        public string SourceIcon => TransformIcon.Get(PlayniteTools.GetSourceName(GameContext.Id));

        public string CoverImage => GameContext.CoverImage == null ? string.Empty : API.Instance.Database.GetFullFilePath(GameContext.CoverImage);

        public List<Activity> Activities { get; set; } = new List<Activity>();

        public List<Activity> ActivitiesOrdered => Activities.OrderByDescending(x => x.DateActivity.ToString("yyyy-MM-dd")).ThenBy(x => x.Type).ToList();


        public bool HasHowLongToBeat => PluginDatabase.Get(GameContext.Id).Items.Where(x => x.Type == ActivityType.HowLongToBeatCompleted)?.Count() > 1;
        public bool InstalledHowLongToBeat => HowLongToBeatPlugin.IsInstalled;
        public bool ShowHowLongToBeatMenu => InstalledHowLongToBeat;

        public bool HasSuccessStory => PluginDatabase.Get(GameContext.Id).Items.Where(x => x.Type == ActivityType.AchievementsGoal || x.Type == ActivityType.AchievementsUnlocked)?.Count() > 1;
        public bool InstalledSuccessStory => SuccessStoryPlugin.IsInstalled;
        public bool ShowSuccessStoryMenu => HasSuccessStory && InstalledSuccessStory;

        public bool HasScreenshotsVisualizer => PluginDatabase.Get(GameContext.Id).Items.Where(x => x.Type == ActivityType.ScreenshotsTaked)?.Count() > 1;
        public bool InstalledScreenshotsVisualizer => ScreenshotsVisualizerPlugin.IsInstalled;
        public bool ShowScreenshotsVisualizerMenu => HasScreenshotsVisualizer && InstalledScreenshotsVisualizer;

        public bool HasPlaytime => PluginDatabase.Get(GameContext.Id).Items.Where(x => x.Type == ActivityType.PlaytimeFirst || x.Type == ActivityType.PlaytimeGoal)?.Count() > 1;       
    }
}
