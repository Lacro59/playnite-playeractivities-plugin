using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Extensions;
using PlayerActivities.Models;
using PlayerActivities.Models.GameActivity;
using PlayerActivities.Models.HowLongToBeat;
using PlayerActivities.Models.ScreenshotsVisualizer;
using PlayerActivities.Models.SuccessStory;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using PlayerActivities.Clients;
using System.Windows;
using System.Windows.Threading;
using PlayerActivities.Views;
using System.Collections.ObjectModel;
using PlayerActivities.Models.Enumerations;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Services
{
    public class PlayerActivitiesDatabase : PluginDatabaseObject<PlayerActivitiesSettingsViewModel, PlayerActivitiesCollection, PlayerActivitiesData, Activity>
    {
        #region Fields and Properties

        /// <summary>
        /// Path to the SuccessStory plugin data directory.
        /// </summary>
        private string SuccessStoryPath { get; }

        /// <summary>
        /// Path to the GameActivity plugin data directory.
        /// </summary>
        private string GameActivityPath { get; }

        /// <summary>
        /// Path to the ScreenshotsVisualizer plugin data directory.
        /// </summary>
        private string ScreenshotsVisuliazerPath { get; }

        /// <summary>
        /// Path to the HowLongToBeat plugin data directory.
        /// </summary>
        private string HowLongToBeatPath { get; }


        private bool _friendsDataIsDownloaded = true;
        /// <summary>
        /// Indicates if friends data has been downloaded.
        /// </summary>
        public bool FriendsDataIsDownloaded { get => _friendsDataIsDownloaded; set => SetValue(ref _friendsDataIsDownloaded, value); }

        private bool _friendsDataIsCanceled = false;
        /// <summary>
        /// Indicates if friends data download has been canceled.
        /// </summary>
        public bool FriendsDataIsCanceled { get => _friendsDataIsCanceled; set => SetValue(ref _friendsDataIsCanceled, value); }

        private Window WindowFriendsDataLoading { get; set; } = null;
        private Stopwatch StopWatchFriendsDataLoading { get; set; } = new Stopwatch();

        private FriendsDataLoading _friendsDataLoading = new FriendsDataLoading();
        /// <summary>
        /// Stores the loading state for friends data.
        /// </summary>
        public FriendsDataLoading FriendsDataLoading { get => _friendsDataLoading; set => SetValue(ref _friendsDataLoading, value); }


        private HltbUserStats _hltbUserStats;
        private HltbUserStats HltbUserStats
        {
            get
            {
                if (_hltbUserStats == null)
                {
                    string pathHltbUserStats = Path.Combine(HowLongToBeatPath, "..", "HltbUserStats.json");
                    if (File.Exists(pathHltbUserStats))
                    {
                        try
                        {
                            _hltbUserStats = Serialization.FromJsonFile<HltbUserStats>(pathHltbUserStats);
                            _hltbUserStats.TitlesList = _hltbUserStats.TitlesList.Where(x => x != null).ToList();
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false);
                        }
                    }
                }
                return _hltbUserStats;
            }
        }

        #endregion

        public PlayerActivitiesDatabase(PlayerActivitiesSettingsViewModel pluginSettings, string pluginUserDataPath) : base(pluginSettings, "PlayerActivities", pluginUserDataPath)
        {
            SuccessStoryPath = Path.Combine(Paths.PluginUserDataPath, "..", PlayniteTools.GetPluginId(ExternalPlugin.SuccessStory).ToString(), "SuccessStory");
            GameActivityPath = Path.Combine(Paths.PluginUserDataPath, "..", PlayniteTools.GetPluginId(ExternalPlugin.GameActivity).ToString(), "GameActivity");
            ScreenshotsVisuliazerPath = Path.Combine(Paths.PluginUserDataPath, "..", PlayniteTools.GetPluginId(ExternalPlugin.ScreenshotsVisualizer).ToString(), "ScreenshotsVisualizer");
            HowLongToBeatPath = Path.Combine(Paths.PluginUserDataPath, "..", PlayniteTools.GetPluginId(ExternalPlugin.HowLongToBeat).ToString(), "HowLongToBeat");
        }

        /// <summary>
        /// Retrieves activity data for a specific game by its Guid.
        /// If not found in cache, creates a default entry and adds it to the database.
        /// </summary>
        /// <param name="id">Game Guid.</param>
        /// <param name="onlyCache">If true, only retrieves from cache.</param>
        /// <param name="force">If true, forces retrieval.</param>
        /// <returns>PlayerActivitiesData for the specified game.</returns>
        public override PlayerActivitiesData Get(Guid id, bool onlyCache = false, bool force = false)
        {
            PlayerActivitiesData playerActivities = base.GetOnlyCache(id);
            if (playerActivities == null)
            {
                Game game = API.Instance.Database.Games.Get(id);
                if (game != null)
                {
                    playerActivities = GetDefault(game);
                    Add(playerActivities);
                }
            }
            return playerActivities;
        }

        #region Plugin data

        /// <summary>
        /// Initializes or refreshes plugin data from external sources.
        /// Optionally forces a full rescan and can target a specific game.
        /// </summary>
        /// <param name="forced">If true, removes and reloads all data.</param>
        /// <param name="id">Optional game Guid to target.</param>
        public void InitializePluginData(bool forced = false, Guid id = default)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}")
            {
                Cancelable = false,
                IsIndeterminate = true
            };

            _ = API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Database.BeginBufferUpdate();
                Thread.Sleep(5000);

                // Remove existing data
                if (forced)
                {
                    Database.ForEach(y =>
                    {
                        if (id == default || y.Game?.Id == id)
                        {
                            y.Items.RemoveAll(x => x.Type == ActivityType.AchievementsGoal);
                            y.Items.RemoveAll(x => x.Type == ActivityType.AchievementsUnlocked);
                            y.Items.RemoveAll(x => x.Type == ActivityType.ScreenshotsTaken);
                            y.Items.RemoveAll(x => x.Type == ActivityType.HowLongToBeatCompleted);
                            y.Items.RemoveAll(x => x.Type == ActivityType.PlaytimeGoal);
                            y.Items.RemoveAll(x => x.Type == ActivityType.PlaytimeFirst);
                        }
                    });
                }

                FirstScanSuccessStory(id);
                FirstScanScreenshotsVisualizer(id);
                FirstScanHowLongToBeat(id);
                FirstScanGameActivity(id);

                Database.EndBufferUpdate();
            }, globalProgressOptions);
        }

        /// <summary>
        /// Scans the SuccessStory plugin data directory and imports achievements for all or a specific game.
        /// </summary>
        /// <param name="id">Optional game Guid to target.</param>
        public void FirstScanSuccessStory(Guid id = default)
        {
            if (Directory.Exists(SuccessStoryPath))
            {
                _ = Parallel.ForEach(Directory.EnumerateFiles(SuccessStoryPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out Guid fileId))
                        {
                            if (id == default || fileId == id)
                            {
                                SetAchievements(fileId);
                            }
                        }
                    });
            }
        }

        /// <summary>
        /// Imports and processes achievement data for a specific game from SuccessStory.
        /// Adds achievement unlocks and progression goals as activities.
        /// </summary>
        /// <param name="id">Game Guid.</param>
        public void SetAchievements(Guid id)
        {
            try
            {
                List<ulong> achievementsGoals = new List<ulong> { 100, 90, 75, 50, 25 };

                string pathData = Path.Combine(SuccessStoryPath, id + ".json");
                if (File.Exists(pathData))
                {
                    _ = Serialization.TryFromJsonFile(pathData, out GameAchievements obj, out Exception ex);
                    if (ex != null)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    if (obj.Items?.Count() > 0 && obj.Progression > 0)
                    {
                        PlayerActivitiesData playerActivitiesData = Get(id);
                        if (playerActivitiesData == null)
                        {
                            return;
                        }

                        int unlocked = 0;
                        var unlockedItems = obj.Items.Where(x => x.IsUnlock).ToList();

                        var distinctDates = unlockedItems
                            .Select(x => x.DateWhenUnlocked?.Date)
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList();

                        foreach (var date in distinctDates)
                        {
                            if (date == null) continue;

                            // by day
                            var achievements = unlockedItems
                                .Where(y => y.DateWhenUnlocked?.Date == date)
                                .ToList();

                            var value = (ulong)achievements.Count;

                            playerActivitiesData.Items.Add(new Activity
                            {
                                DateActivity = date.Value,
                                Type = ActivityType.AchievementsUnlocked,
                                Value = value
                            });

                            // by goal
                            unlocked += achievements.Count;
                            ulong progression = (ulong)Math.Ceiling((double)(unlocked * 100 / obj.Items.Count()));

                            foreach (var goal in achievementsGoals)
                            {
                                if (progression >= goal
                                    && !playerActivitiesData.Items.Any(y => y.DateActivity == date.Value && y.Type == ActivityType.AchievementsGoal)
                                    && !playerActivitiesData.Items.Any(y => y.Type == ActivityType.AchievementsGoal && y.Value == goal))
                                {
                                    playerActivitiesData.Items.Add(new Activity
                                    {
                                        DateActivity = date.Value,
                                        Type = ActivityType.AchievementsGoal,
                                        Value = goal
                                    });
                                }
                            }
                        }

                        AddOrUpdate(playerActivitiesData);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        /// <summary>
        /// Scans the ScreenshotsVisualizer plugin data directory and imports screenshots for all or a specific game.
        /// </summary>
        /// <param name="id">Optional game Guid to target.</param>
        public void FirstScanScreenshotsVisualizer(Guid id = default)
        {
            if (Directory.Exists(ScreenshotsVisuliazerPath))
            {
                _ = Parallel.ForEach(Directory.EnumerateFiles(ScreenshotsVisuliazerPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out var fileId))
                        {
                            if (id == default || fileId == id)
                            {
                                SetScreenshots(fileId);
                            }
                        }
                    });
            }
        }

        /// <summary>
        /// Imports and processes screenshot data for a specific game from ScreenshotsVisualizer.
        /// Adds screenshot activities by day.
        /// </summary>
        /// <param name="id">Game Guid.</param>
        public void SetScreenshots(Guid id)
        {
            try
            {
                string pathData = Path.Combine(ScreenshotsVisuliazerPath, id + ".json");
                if (File.Exists(pathData))
                {
                    _ = Serialization.TryFromJsonFile(pathData, out GameScreenshots obj, out Exception ex);
                    if (ex != null)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    if (obj.Items?.Count() > 0)
                    {
                        PlayerActivitiesData playerActivitiesData = Get(id);
                        if (playerActivitiesData == null)
                        {
                            return;
                        }

                        var distinctDates = obj.Items
                            .Select(x => x.Modifed.Date)
                            .Distinct()
                            .OrderBy(x => x)
                            .ToList();

                        foreach (var date in distinctDates)
                        {
                            var screenshots = obj.Items
                                .Where(y => y.Modifed.Date == date)
                                .ToList();

                            var value = (ulong)screenshots.Count;

                            playerActivitiesData.Items.Add(new Models.Activity
                            {
                                DateActivity = date,
                                Type = ActivityType.ScreenshotsTaken,
                                Value = value
                            });
                        }

                        AddOrUpdate(playerActivitiesData);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        /// <summary>
        /// Scans the GameActivity plugin data directory and imports play sessions for all or a specific game.
        /// </summary>
        /// <param name="id">Optional game Guid to target.</param>
        public void FirstScanGameActivity(Guid id = default)
        {
            if (Directory.Exists(GameActivityPath))
            {
                _ = Parallel.ForEach(Directory.EnumerateFiles(GameActivityPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out Guid fileId))
                        {
                            if (id == default || fileId == id)
                            {
                                SetGameActivity(fileId);
                            }
                        }
                    });
            }
        }

        /// <summary>
        /// Imports and processes play session data for a specific game from GameActivity.
        /// Adds first playtime and playtime goal activities.
        /// </summary>
        /// <param name="id">Game Guid.</param>
        private void SetGameActivity(Guid id)
        {
            try
            {
                string pathData = Path.Combine(GameActivityPath, id + ".json");
                if (File.Exists(pathData))
                {
                    _ = Serialization.TryFromJsonFile(pathData, out GameActivities obj, out Exception ex);
                    if (ex != null)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    if (obj.Items?.Count() > 0)
                    {
                        PlayerActivitiesData playerActivitiesData = Get(id);
                        if (playerActivitiesData == null)
                        {
                            return;
                        }

                        // Playtime first
                        if (!playerActivitiesData.Items.Any(x => x.Type == ActivityType.PlaytimeFirst))
                        {
                            DateTime dtFirst = obj.Items.Select(x => x.DateSession).Min();

                            playerActivitiesData.Items.Add(new Activity
                            {
                                DateActivity = dtFirst,
                                Type = ActivityType.PlaytimeFirst
                            });
                        }

                        // Playtime goal
                        List<ulong> playtimeGoals = new List<ulong> { 1000, 900, 800, 700, 600, 500, 400, 300, 200, 100, 50, 25, 10, 5, 1 };
                        ulong elapsedSeconds = 0;

                        foreach (var item in obj.Items.OrderBy(x => x.DateSession))
                        {
                            elapsedSeconds += item.ElapsedSeconds;

                            foreach (var goal in playtimeGoals)
                            {
                                if (elapsedSeconds >= 3600 * goal
                                    && !playerActivitiesData.Items.Any(z => z.Type == ActivityType.PlaytimeGoal && z.Value == goal))
                                {
                                    playerActivitiesData.Items.Add(new Activity
                                    {
                                        DateActivity = item.DateSession,
                                        Type = ActivityType.PlaytimeGoal,
                                        Value = goal
                                    });
                                }
                            }
                        }

                        AddOrUpdate(playerActivitiesData);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        /// <summary>
        /// Scans the HowLongToBeat plugin data directory and imports completion data for all or a specific game.
        /// </summary>
        /// <param name="id">Optional game Guid to target.</param>
        public void FirstScanHowLongToBeat(Guid id = default)
        {
            if (Directory.Exists(HowLongToBeatPath))
            {
                _ = Parallel.ForEach(Directory.EnumerateFiles(HowLongToBeatPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out Guid fileId))
                        {
                            if (id == default || fileId == id)
                            {
                                SetHowLongToBeat(fileId);
                            }
                        }
                    });
            }
        }

        /// <summary>
        /// Imports and processes completion data for a specific game from HowLongToBeat.
        /// Adds completion activities.
        /// </summary>
        /// <param name="id">Game Guid.</param>
        public void SetHowLongToBeat(Guid id)
        {
            try
            {
                string pathData = Path.Combine(HowLongToBeatPath, id + ".json");
                if (File.Exists(pathData))
                {
                    _ = Serialization.TryFromJsonFile(pathData, out GameHowLongToBeat obj, out Exception ex);
                    if (ex != null)
                    {
                        Common.LogError(ex, false, true, PluginName);
                    }

                    if (obj.Items?.Count() > 0)
                    {
                        PlayerActivitiesData playerActivitiesData = Get(id);
                        if (playerActivitiesData == null)
                        {
                            return;
                        }

                        // HowLongToBeatCompleted
                        HltbDataUser hltbDataUser = obj.GetData();
                        if (HltbUserStats != null)
                        {
                            var titleLists = HltbUserStats.TitlesList
                                .Where(x => x.Id == hltbDataUser.Id)
                                .ToList();

                            foreach (var title in titleLists)
                            {
                                DateTime? completionDate = title.Completion;
                                if (completionDate != null)
                                {
                                    playerActivitiesData.Items.Add(new Activity
                                    {
                                        DateActivity = completionDate.Value,
                                        Type = ActivityType.HowLongToBeatCompleted
                                    });
                                }
                            }
                        }

                        AddOrUpdate(playerActivitiesData);

                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        /// <summary>
        /// Retrieves activities related to a specific game identified by its Guid.
        /// </summary>
        /// <param name="id">The unique identifier (Guid) of the game to filter activities for.</param>
        /// <returns>
        /// An <see cref="ObservableCollection{ActivityListGrouped}"/> containing only
        /// the activities associated with the specified game. Returns an empty collection if none are found.
        /// </returns>
        public ObservableCollection<ActivityListGrouped> GetActivitiesData(Guid id)
        {
            ObservableCollection<ActivityListGrouped> data = GetActivitiesData(false);
            return new ObservableCollection<ActivityListGrouped>(data.Where(x => x.GameContext.Id == id));
        }

        /// <summary>
        /// Retrieves and optionally groups recent activity data for all games in the database.
        /// </summary>
        /// <param name="grouped">
        /// If <c>true</c>, groups activities by game and relative time period ("TimeAgo").
        /// If <c>false</c>, each activity is listed independently, grouped only by game.
        /// </param>
        /// <returns>
        /// An <see cref="ObservableCollection{ActivityListGrouped}"/> containing grouped activity data,
        /// ordered by date descending. Returns an empty collection if no activity matches the configured types.
        /// </returns>
        public ObservableCollection<ActivityListGrouped> GetActivitiesData(bool grouped = true)
        {
            // Step 1: Flatten all activity items from games that exist in the database
            var activityLists = Database
                .Where(x => x.GameExist)
                .SelectMany(x => x.Items.Select(y => new ActivityList
                {
                    GameContext = x.Game,
                    DateActivity = y.DateActivity.Date,
                    Type = y.Type,
                    Value = y.Value
                }))
                .OrderByDescending(x => x.DateActivity)
                .ToList();

            // Step 2: Build the list of activity types to include based on plugin settings
            var activityTypes = new List<ActivityType> { ActivityType.PlaytimeFirst, ActivityType.PlaytimeGoal };

            if (PluginSettings.Settings.EnableHowLongToBeatData)
            {
                activityTypes.Add(ActivityType.HowLongToBeatCompleted);
            }

            if (PluginSettings.Settings.EnableScreenshotsVisualizerData)
            {
                activityTypes.Add(ActivityType.ScreenshotsTaken);
            }

            if (PluginSettings.Settings.EnableSuccessStoryData)
            {
                activityTypes.Add(ActivityType.AchievementsGoal);
                activityTypes.Add(ActivityType.AchievementsUnlocked);
            }

            // Step 3: Filter and group activity data
            var filteredActivities = activityLists.Where(x => activityTypes.Contains(x.Type));

            var groupedActivities = new ObservableCollection<ActivityListGrouped>();

            foreach (var activity in filteredActivities)
            {
                var existingGroup = groupedActivities.FirstOrDefault(g =>
                    g.GameContext.Id == activity.GameContext.Id &&
                    (!grouped || g.TimeAgo.IsEqual(activity.TimeAgo)));

                if (existingGroup != null)
                {
                    existingGroup.Activities.Add(new Activity
                    {
                        DateActivity = activity.DateActivity,
                        Value = activity.Value,
                        Type = activity.Type
                    });
                }
                else
                {
                    groupedActivities.Add(new ActivityListGrouped
                    {
                        GameContext = activity.GameContext,
                        DtString = activity.DateActivity.ToString("yyyy-MM-dd"),
                        TimeAgo = activity.TimeAgo,
                        Activities = new List<Activity>
                        {
                            new Activity
                            {
                                DateActivity = activity.DateActivity,
                                Value = activity.Value,
                                Type = activity.Type
                            }
                        }
                    });
                }
            }

            // Step 4: Sort activities within each group
            foreach (var group in groupedActivities)
            {
                group.Activities = group.Activities
                    .OrderByDescending(a => a.DateActivity)
                    .ThenBy(a => a.Type)
                    .ToList();
            }

            return groupedActivities;
        }

        #endregion

        #region Friends Data Management

        private FriendsData LoadFriendsData()
        {
            string friendsFilePath = Path.Combine(Paths.PluginUserDataPath, "FriendsData.json");
            if (File.Exists(friendsFilePath))
            {
                try
                {
                    return Serialization.FromJsonFile<FriendsData>(friendsFilePath);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
            return new FriendsData();
        }

        /// <summary>
        /// Retrieves the list of player friends from all enabled platforms (GOG, Steam, Origin, Epic).
        /// If force is true, fetches fresh data from each platform and updates the local cache.
        /// </summary>
        /// <param name="plugin">The PlayerActivities plugin instance.</param>
        /// <param name="force">If true, forces a refresh from all sources.</param>
        /// <returns>List of PlayerFriends objects.</returns>
        public FriendsData GetFriends(PlayerActivities plugin, bool force = false)
        {
            var friendsData = new FriendsData();
            string friendsFilePath = Path.Combine(Paths.PluginUserDataPath, "FriendsData.json");

            if (force)
            {
                // Helper to fetch friends from a platform if enabled and not canceled
                List<PlayerFriend> FetchFriends(bool enabled, string sourceName, Func<List<PlayerFriend>> getFriendsFunc)
                {
                    if (!enabled || FriendsDataIsCanceled)
                    {
                        return new List<PlayerFriend>();
                    }

                    FriendsDataLoading.FriendName = string.Empty;
                    FriendsDataLoading.ActualCount = 0;
                    FriendsDataLoading.FriendCount = 0;
                    FriendsDataLoading.SourceName = sourceName;
                    return getFriendsFunc();
                }

                var gog = FetchFriends(PluginSettings.Settings.EnableGogFriends, "Gog", () => new GogFriends().GetFriends());
                var steam = FetchFriends(PluginSettings.Settings.EnableSteamFriends, "Steam", () => new SteamFriends().GetFriends());
                var ea = FetchFriends(PluginSettings.Settings.EnableOriginFriends, "EA app", () => new EaFriends().GetFriends());
                var epic = FetchFriends(PluginSettings.Settings.EnableEpicFriends, "Epic", () => new EpicFriends().GetFriends());

                friendsData.PlayerFriends = gog.Concat(steam).Concat(ea).Concat(epic).ToList();
                friendsData.LastUpdate = DateTime.UtcNow;

                if (FriendsDataIsCanceled)
                {
                    return LoadFriendsData();
                }

                try
                {
                    File.WriteAllText(friendsFilePath, Serialization.ToJson(friendsData));
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }
            else
            {
                return LoadFriendsData();
            }

            return friendsData;
        }

        /// <summary>
        /// Asynchronously refreshes friends data, updates the database, and shows a loading window.
        /// </summary>
        /// <param name="plugin">The PlayerActivities plugin instance.</param>
        public async Task RefreshFriendsDataLoader(PlayerActivities plugin)
        {
            FriendsDataIsDownloaded = false;
            await Task.Run(() =>
            {
                StopWatchFriendsDataLoading = new Stopwatch();
                StopWatchFriendsDataLoading.Start();

                Database = new PlayerActivitiesCollection(Paths.PluginDatabasePath);
                Database.SetGameInfo<Activity>();

                WindowOptions windowOptions = new WindowOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = false,
                    ShowCloseButton = false,
                    CanBeResizable = false
                };

                _ = Application.Current.Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    PaFriendDataLoading view = new PaFriendDataLoading();
                    WindowFriendsDataLoading = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCCommonGettingData"), view, windowOptions);
                    _ = WindowFriendsDataLoading.ShowDialog();
                }));

                _ = GetFriends(plugin, true);
                FriendsDataLoaderClose(string.Empty, string.Empty);
            });
        }

        /// <summary>
        /// Synchronously refreshes friends data and updates the database.
        /// </summary>
        /// <param name="plugin">The PlayerActivities plugin instance.</param>
        public void RefreshFriends(PlayerActivities plugin)
        {
            FriendsDataIsDownloaded = false;

            StopWatchFriendsDataLoading = new Stopwatch();
            StopWatchFriendsDataLoading.Start();

            Database = new PlayerActivitiesCollection(Paths.PluginDatabasePath);
            Database.SetGameInfo<Activity>();

            _ = GetFriends(plugin, true);
            FriendsDataLoaderClose(string.Empty, string.Empty);
        }

        /// <summary>
        /// Refreshes friends data for a specific client and friend, updating the local cache.
        /// </summary>
        /// <param name="plugin">The PlayerActivities plugin instance.</param>
        /// <param name="clientName">The name of the client (e.g., "STEAM").</param>
        /// <param name="pf">The PlayerFriends object to update.</param>
        public void RefreshFriends(PlayerActivities plugin, string clientName, PlayerFriend pf)
        {
            FriendsDataIsDownloaded = false;

            StopWatchFriendsDataLoading = new Stopwatch();
            StopWatchFriendsDataLoading.Start();

            List<PlayerFriend> playerFriends = new List<PlayerFriend>();

            string friendsFilePath = Path.Combine(Paths.PluginUserDataPath, "FriendsData.json");
            if (File.Exists(friendsFilePath))
            {
                try
                {
                    FriendsData friendsData = Serialization.FromJsonFile<FriendsData>(friendsFilePath);
                    playerFriends = friendsData.PlayerFriends;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }

            // Refresh friend data based on client
            if (PluginSettings.Settings.EnableGogFriends && clientName.IsEqual("GOG"))
            {
                pf = new GogFriends().GetFriends(pf);
            }
            else if (PluginSettings.Settings.EnableSteamFriends && clientName.IsEqual("STEAM"))
            {
                pf = new SteamFriends().GetFriends(pf);
            }
            else if (PluginSettings.Settings.EnableOriginFriends && clientName.IsEqual("EA"))
            {
                pf = new EaFriends().GetFriends(pf);
            }
            else if (PluginSettings.Settings.EnableEpicFriends && clientName.IsEqual("EPIC"))
            {
                pf = new EpicFriends().GetFriends(pf);
            }

            // Update existing entry
            var index = playerFriends.FindIndex(f => f.FriendId.IsEqual(pf.FriendId));
            if (index >= 0)
            {
                playerFriends[index] = pf;
            }

            // Save updated data
            try
            {
                FriendsData friendsData = new FriendsData { PlayerFriends = playerFriends };
                File.WriteAllText(friendsFilePath, Serialization.ToJson(friendsData));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            FriendsDataLoaderClose(pf.FriendPseudo, pf.ClientName);
        }

        /// <summary>
        /// Closes the friends data loading window and logs the operation duration.
        /// </summary>
        /// <param name="pseudo">Optional friend pseudo to log.</param>
        /// <param name="clientName">The name of the client (e.g., "STEAM").</param>
        private void FriendsDataLoaderClose(string pseudo, string clientName)
        {
            if (WindowFriendsDataLoading != null)
            {
                _ = Application.Current.Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    WindowFriendsDataLoading.Close();
                }));
            }

            StopWatchFriendsDataLoading.Stop();
            TimeSpan ts = StopWatchFriendsDataLoading.Elapsed;
            Logger.Info($"RefreshFriendsDataLoader" + (FriendsDataIsCanceled ? " (canceled) " : "")
                + (!pseudo.IsNullOrEmpty() ? $" - {pseudo} ({clientName})" : "")
                + $" - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");

            FriendsDataIsCanceled = false;
            FriendsDataIsDownloaded = true;
        }

        #endregion

        public override void SetThemesResources(Game game)
        {
            PluginSettings.Settings.HasData = Database.Get(game.Id)?.HasData ?? false;
        }
    }
}