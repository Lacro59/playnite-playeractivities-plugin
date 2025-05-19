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

namespace PlayerActivities.Services
{
    public class PlayerActivitiesDatabase : PluginDatabaseObject<PlayerActivitiesSettingsViewModel, PlayerActivitiesCollection, PlayerActivitiesData, Activity>
    {
        private string SuccessStoryPath { get; }
        private string GameActivityPath { get; }
        private string ScreenshotsVisuliazerPath { get; }
        private string HowLongToBeatPath { get; }


        private bool _friendsDataIsDownloaded = true;
        public bool FriendsDataIsDownloaded { get => _friendsDataIsDownloaded; set => SetValue(ref _friendsDataIsDownloaded, value); }

        private bool _friendsDataIsCanceled = false;
        public bool FriendsDataIsCanceled { get => _friendsDataIsCanceled; set => SetValue(ref _friendsDataIsCanceled, value); }

        private Window WindowFriendsDataLoading { get; set; } = null;
        private Stopwatch StopWatchFriendsDataLoading { get; set; } = new Stopwatch();

        private FriendsDataLoading _friendsDataLoading = new FriendsDataLoading();
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


        public PlayerActivitiesDatabase(PlayerActivitiesSettingsViewModel pluginSettings, string pluginUserDataPath) : base(pluginSettings, "PlayerActivities", pluginUserDataPath)
        {
            SuccessStoryPath = Path.Combine(Paths.PluginUserDataPath, "..", "cebe6d32-8c46-4459-b993-5a5189d60788", "SuccessStory");
            GameActivityPath = Path.Combine(Paths.PluginUserDataPath, "..", "afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4", "GameActivity");
            ScreenshotsVisuliazerPath = Path.Combine(Paths.PluginUserDataPath, "..", "c6c8276f-91bf-48e5-a1d1-4bee0b493488", "ScreenshotsVisualizer");
            HowLongToBeatPath = Path.Combine(Paths.PluginUserDataPath, "..", "e08cd51f-9c9a-4ee3-a094-fde03b55492f", "HowLongToBeat");
        }


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
                            y.Items.RemoveAll(x => x.Type == ActivityType.ScreenshotsTaked);
                            y.Items.RemoveAll(x => x.Type == ActivityType.HowLongToBeatCompleted);
                        }
                    });
                }

                FirstScanSuccessStory(id);
                FirstScanScreenshotsVisualizer(id);
                if (!forced)
                {
                    FirstScanGameActivity(id);
                }
                FirstScanHowLongToBeat(id);

                Database.EndBufferUpdate();
            }, globalProgressOptions);
        }


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

        public void SetAchievements(Guid id)
        {
            try
            {
                List<ulong> AchievementsGoals = new List<ulong> { 100, 90, 75, 50, 25 };

                string PathData = Path.Combine(SuccessStoryPath, id + ".json");
                if (File.Exists(PathData))
                {
                    GameAchievements obj = Serialization.FromJsonFile<GameAchievements>(PathData);
                    if (obj.Items?.Count() > 0 && obj.Progression > 0)
                    {
                        PlayerActivitiesData playerActivitiesData = Get(id);
                        if (playerActivitiesData == null)
                        {
                            return;
                        }

                        int Unlocked = 0;
                        obj.Items.Where(x => x.DateUnlocked != null && (DateTime)x.DateUnlocked != default(DateTime))
                            .Select(x => x.DateUnlocked?.ToString("yyyy-MM-dd")).Distinct().OrderBy(x => x).ForEach(x =>
                        {
                            List<Achievements> ach = obj.Items.FindAll(y => y.DateUnlocked?.ToString("yyyy-MM-dd").IsEqual(x) ?? false).ToList();

                            // by day
                            DateTime DateActivity = (DateTime)obj.Items.FindAll(y => y.DateUnlocked?.ToString("yyyy-MM-dd").IsEqual(x) ?? false).First().DateUnlocked;
                            ulong Value = (ulong)ach.Count();

                            playerActivitiesData.Items.Add(new Activity
                            {
                                DateActivity = DateActivity,
                                Type = ActivityType.AchievementsUnlocked,
                                Value = Value
                            });

                            // by goal
                            Unlocked += ach.Count();
                            ulong Progression = (ulong)Math.Ceiling((double)(Unlocked * 100 / obj.Items.Count()));
                            AchievementsGoals.ForEach(z =>
                            {
                                if (Progression >= z
                                    && playerActivitiesData.Items.Find(y => y.DateActivity == DateActivity && y.Type == ActivityType.AchievementsGoal) == null
                                    && playerActivitiesData.Items.Find(y => y.Type == ActivityType.AchievementsGoal && y.Value == z) == null)
                                {
                                    playerActivitiesData.Items.Add(new Activity
                                    {
                                        DateActivity = DateActivity,
                                        Type = ActivityType.AchievementsGoal,
                                        Value = z
                                    });
                                    return;
                                }
                            });
                        });

                        AddOrUpdate(playerActivitiesData);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }


        public void FirstScanScreenshotsVisualizer(Guid id = default)
        {
            if (Directory.Exists(SuccessStoryPath))
            {
                _ = Parallel.ForEach(Directory.EnumerateFiles(ScreenshotsVisuliazerPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out var fileId))
                        {
                            if (id == default(Guid) || fileId == id)
                            {
                                SetScreenshots(fileId);
                            }
                        }
                    });
            }
        }

        public void SetScreenshots(Guid id)
        {
            try
            {
                List<ulong> AchievementsGoals = new List<ulong> { 100, 90, 75, 50, 25 };

                string PathData = Path.Combine(ScreenshotsVisuliazerPath, id + ".json");
                if (File.Exists(PathData))
                {
                    GameScreenshots obj = Serialization.FromJsonFile<GameScreenshots>(PathData);
                    if (obj.Items?.Count() > 0)
                    {
                        PlayerActivitiesData playerActivitiesData = Get(id);
                        if (playerActivitiesData == null)
                        {
                            return;
                        }

                        obj.Items.Select(x => x.Modifed.ToString("yyyy-MM-dd")).Distinct().OrderBy(x => x).ForEach(x =>
                        {
                            List<Screenshot> screen = obj.Items.FindAll(y => y.Modifed.ToString("yyyy-MM-dd").IsEqual(x)).ToList();

                            // by day
                            DateTime DateActivity = (DateTime)obj.Items.FindAll(y => y.Modifed.ToString("yyyy-MM-dd").IsEqual(x)).First().Modifed;
                            ulong Value = (ulong)screen.Count();

                            playerActivitiesData.Items.Add(new Models.Activity
                            {
                                DateActivity = DateActivity,
                                Type = ActivityType.ScreenshotsTaked,
                                Value = Value
                            });
                        });

                        AddOrUpdate(playerActivitiesData);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }


        public void FirstScanGameActivity(Guid id = default)
        {
            if (Directory.Exists(SuccessStoryPath))
            {
                _ = Parallel.ForEach(Directory.EnumerateFiles(GameActivityPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out Guid fileId))
                        {
                            if (id == default(Guid) || fileId == id)
                            {
                                SetGameActivity(fileId);
                            }
                        }
                    });
            }
        }

        private void SetGameActivity(Guid id)
        {
            try
            {
                string PathData = Path.Combine(GameActivityPath, id + ".json");
                if (File.Exists(PathData))
                {
                    GameActivities obj = Serialization.FromJsonFile<GameActivities>(PathData);
                    if (obj.Items?.Count() > 0)
                    {
                        PlayerActivitiesData playerActivitiesData = Get(id);
                        if (playerActivitiesData == null)
                        {
                            return;
                        }

                        // Playtime first
                        DateTime dtFirst = obj.Items.Select(x => x.DateSession).Min();

                        playerActivitiesData.Items.Add(new Activity
                        {
                            DateActivity = dtFirst,
                            Type = ActivityType.PlaytimeFirst
                        });

                        // Playtime goal
                        List<ulong> PlaytimeGoals = new List<ulong> { 1000, 900, 800, 700, 600, 500, 400, 300, 200, 100, 50, 25, 10, 5 };
                        ulong ElapsedSeconds = 0;
                        obj.Items.OrderBy(x => x.DateSession).ForEach(x =>
                        {
                            ElapsedSeconds += x.ElapsedSeconds;
                            PlaytimeGoals.ForEach(y =>
                            {
                                if (ElapsedSeconds >= 3600 * y
                                        && playerActivitiesData.Items.Find(z => z.Type == ActivityType.PlaytimeGoal && z.Value == y) == null)
                                {
                                    playerActivitiesData.Items.Add(new Activity
                                    {
                                        DateActivity = x.DateSession,
                                        Type = ActivityType.PlaytimeGoal,
                                        Value = y
                                    });
                                    return;
                                }
                            });
                        });

                        AddOrUpdate(playerActivitiesData);
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }


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

        public void SetHowLongToBeat(Guid id)
        {
            try
            {
                string pathData = Path.Combine(HowLongToBeatPath, id + ".json");
                if (File.Exists(pathData))
                {
                    GameHowLongToBeat obj = Serialization.FromJsonFile<GameHowLongToBeat>(pathData);
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
                            List<TitleList> titleLists = HltbUserStats.TitlesList.FindAll(x => x.Id == obj.GetData().Id).ToList();
                            titleLists.ForEach(x =>
                            {
                                DateTime? dt = x.Completion;
                                if (dt != null)
                                {
                                    playerActivitiesData.Items.Add(new Activity
                                    {
                                        DateActivity = (DateTime)dt,
                                        Type = ActivityType.HowLongToBeatCompleted
                                    });
                                }
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
                    DateActivity = y.DateActivity,
                    Type = y.Type,
                    Value = y.Value
                }))
                .OrderByDescending(x => x.DateActivity)
                .ToList();

            // Step 2: Build the list of activity types to include based on plugin settings
            var activityTypes = new List<ActivityType> { ActivityType.PlaytimeFirst, ActivityType.PlaytimeGoal };

            if (PluginSettings.Settings.EnableHowLongToBeatData)
                activityTypes.Add(ActivityType.HowLongToBeatCompleted);

            if (PluginSettings.Settings.EnableScreenshotsVisualizerData)
                activityTypes.Add(ActivityType.ScreenshotsTaked);

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


        public List<PlayerFriends> GetFriends(PlayerActivities plugin, bool force = false)
        {
            List<PlayerFriends> playerFriends = new List<PlayerFriends>();

            if (force)
            {
                List<PlayerFriends> gogs = new List<PlayerFriends>();
                if (PluginSettings.Settings.EnableGogFriends)
                {
                    FriendsDataLoading.FriendName = string.Empty;
                    FriendsDataLoading.ActualCount = 0;
                    FriendsDataLoading.FriendCount = 0;
                    FriendsDataLoading.SourceName = "Gog";

                    GogFriends gogFriends = new GogFriends();
                    gogs = gogFriends.GetFriends();
                }

                List<PlayerFriends> steams = new List<PlayerFriends>();
                if (PluginSettings.Settings.EnableSteamFriends && !FriendsDataIsCanceled)
                {
                    FriendsDataLoading.FriendName = string.Empty;
                    FriendsDataLoading.ActualCount = 0;
                    FriendsDataLoading.FriendCount = 0;
                    FriendsDataLoading.SourceName = "Steam";

                    SteamFriends steamFriends = new SteamFriends();
                    steams = steamFriends.GetFriends();
                }

                List<PlayerFriends> origin = new List<PlayerFriends>();
                if (PluginSettings.Settings.EnableOriginFriends && !FriendsDataIsCanceled)
                {
                    FriendsDataLoading.FriendName = string.Empty;
                    FriendsDataLoading.ActualCount = 0;
                    FriendsDataLoading.FriendCount = 0;
                    FriendsDataLoading.SourceName = "Origin";

                    OriginFriends originFriends = new OriginFriends();
                    origin = originFriends.GetFriends();
                }

                List<PlayerFriends> epic = new List<PlayerFriends>();
                if (PluginSettings.Settings.EnableEpicFriends && !FriendsDataIsCanceled)
                {
                    FriendsDataLoading.FriendName = string.Empty;
                    FriendsDataLoading.ActualCount = 0;
                    FriendsDataLoading.FriendCount = 0;
                    FriendsDataLoading.SourceName = "Epic";

                    EpicFriends epicFriends = new EpicFriends();
                    epic = epicFriends.GetFriends();
                }

                playerFriends = playerFriends.Concat(gogs).Concat(steams).Concat(origin).Concat(epic).ToList();

                PluginSettings.Settings.LastFriendsRefresh = DateTime.Now;
                plugin.SavePluginSettings(PluginSettings.Settings);

                File.WriteAllText(Path.Combine(Paths.PluginUserDataPath, "PlayerFriends.json"), Serialization.ToJson(playerFriends));
            }
            else
            {
                string PathPlayerFriends = Path.Combine(Paths.PluginUserDataPath, "PlayerFriends.json");
                if (File.Exists(PathPlayerFriends))
                {
                    try
                    {
                        playerFriends = Serialization.FromJsonFile<List<PlayerFriends>>(PathPlayerFriends);
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false);
                    }
                }
            }

            return playerFriends;
        }

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
                FriendsDataLoaderClose(string.Empty);
            });
        }

        public void RefreshFriends(PlayerActivities plugin)
        {
            FriendsDataIsDownloaded = false;

            StopWatchFriendsDataLoading = new Stopwatch();
            StopWatchFriendsDataLoading.Start();

            Database = new PlayerActivitiesCollection(Paths.PluginDatabasePath);
            Database.SetGameInfo<Activity>();

            _ = GetFriends(plugin, true);
            FriendsDataLoaderClose(string.Empty);
        }

        public void RefreshFriends(PlayerActivities plugin, string clientName, PlayerFriends pf)
        {
            FriendsDataIsDownloaded = false;

            StopWatchFriendsDataLoading = new Stopwatch();
            StopWatchFriendsDataLoading.Start();

            List<PlayerFriends> playerFriends = new List<PlayerFriends>();

            string pathPlayerFriends = Path.Combine(Paths.PluginUserDataPath, "PlayerFriends.json");
            if (File.Exists(pathPlayerFriends))
            {
                try
                {
                    playerFriends = Serialization.FromJsonFile<List<PlayerFriends>>(pathPlayerFriends);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false);
                }
            }


            if (PluginSettings.Settings.EnableGogFriends && clientName.IsEqual("GOG"))
            {
                GogFriends gogFriends = new GogFriends();
                pf = gogFriends.GetFriends(pf);
            }

            if (PluginSettings.Settings.EnableSteamFriends && clientName.IsEqual("STEAM"))
            {
                SteamFriends steamFriends = new SteamFriends();
                pf = steamFriends.GetFriends(pf);
            }

            if (PluginSettings.Settings.EnableOriginFriends && clientName.IsEqual("EA"))
            {
                OriginFriends originFriends = new OriginFriends();
                pf = originFriends.GetFriends(pf);
            }

            if (PluginSettings.Settings.EnableEpicFriends && clientName.IsEqual("EPIC"))
            {
                EpicFriends epicFriends = new EpicFriends();
                pf = epicFriends.GetFriends(pf);
            }

            int index = playerFriends.FindIndex(item => item.FriendId.IsEqual(pf.FriendId));
            if (index != -1)
            {
                playerFriends[index] = pf;
            }

            File.WriteAllText(Path.Combine(Paths.PluginUserDataPath, "PlayerFriends.json"), Serialization.ToJson(playerFriends));
            FriendsDataLoaderClose(pf.FriendPseudo);
        }

        private void FriendsDataLoaderClose(string pseudo)
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
                + (!pseudo.IsNullOrEmpty() ? $" - {pseudo} " : "")
                + $" - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");

            FriendsDataIsCanceled = false;
            FriendsDataIsDownloaded = true;
        }


        public override void SetThemesResources(Game game)
        {
            PluginSettings.Settings.HasData = Database.Get(game.Id)?.HasData ?? false;
        }
    }
}