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
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using PlayerActivities.Clients;
using System.Windows;
using System.Windows.Threading;
using PlayerActivities.Views;
using System.Collections.ObjectModel;

namespace PlayerActivities.Services
{
    public class PlayerActivitiesDatabase : PluginDatabaseObject<PlayerActivitiesSettingsViewModel, PlayerActivitiesCollection, Models.PlayerActivitiesData, Activity>
    {
        private string SuccessStoryPath { get; }
        private string GameActivityPath { get; }
        private string ScreenshotsVisuliazerPath { get; }
        private string HowLongToBeatPath { get; }


        private bool friendsDataIsDownloaded = true;
        public bool FriendsDataIsDownloaded { get => friendsDataIsDownloaded; set => SetValue(ref friendsDataIsDownloaded, value); }

        private bool friendsDataIsCanceled = false;
        public bool FriendsDataIsCanceled { get => friendsDataIsCanceled; set => SetValue(ref friendsDataIsCanceled, value); }

        private Window WindowFriendsDataLoading { get; set; } = null;
        private Stopwatch StopWatchFriendsDataLoading { get; set; } = new Stopwatch();

        private FriendsDataLoading friendsDataLoading = new FriendsDataLoading();
        public FriendsDataLoading FriendsDataLoading { get => friendsDataLoading; set => SetValue(ref friendsDataLoading, value); }


        private HltbUserStats hltbUserStats;
        private HltbUserStats HltbUserStats
        {
            get
            {
                if (hltbUserStats == null)
                {
                    string PathHltbUserStats = Path.Combine(HowLongToBeatPath, "..", "HltbUserStats.json");
                    if (File.Exists(PathHltbUserStats))
                    {
                        try
                        {
                            hltbUserStats = Serialization.FromJsonFile<HltbUserStats>(PathHltbUserStats);
                            hltbUserStats.TitlesList = hltbUserStats.TitlesList.Where(x => x != null).ToList();
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false);
                        }
                    }
                }
                return hltbUserStats;
            }
        }


        public PlayerActivitiesDatabase(PlayerActivitiesSettingsViewModel PluginSettings, string PluginUserDataPath) : base(PluginSettings, "PlayerActivities", PluginUserDataPath)
        {
            SuccessStoryPath = Path.Combine(Paths.PluginUserDataPath, "..", "cebe6d32-8c46-4459-b993-5a5189d60788", "SuccessStory");
            GameActivityPath = Path.Combine(Paths.PluginUserDataPath, "..", "afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4", "GameActivity");
            ScreenshotsVisuliazerPath = Path.Combine(Paths.PluginUserDataPath, "..", "c6c8276f-91bf-48e5-a1d1-4bee0b493488", "ScreenshotsVisualizer");
            HowLongToBeatPath = Path.Combine(Paths.PluginUserDataPath, "..", "e08cd51f-9c9a-4ee3-a094-fde03b55492f", "HowLongToBeat");
        }


        protected override bool LoadDatabase()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Database = new PlayerActivitiesCollection(Paths.PluginDatabasePath);
                Database.SetGameInfo<Activity>();

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"LoadDatabase with {Database.Count} items - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }

            return true;
        }


        public override PlayerActivitiesData Get(Guid Id, bool OnlyCache = false, bool Force = false)
        {
            PlayerActivitiesData playerActivities = base.GetOnlyCache(Id);
            if (playerActivities == null)
            {
                Game game = API.Instance.Database.Games.Get(Id);
                if (game != null)
                {
                    playerActivities = GetDefault(game);
                    Add(playerActivities);
                }
            }
            return playerActivities;
        }


        #region Plugin data
        public void InitializePluginData(bool forced = false, Guid Id = default)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {ResourceProvider.GetString("LOCCommonProcessing")}",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            _ = API.Instance.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Database.BeginBufferUpdate();
                Thread.Sleep(5000);

                // Remove existing data
                if (forced)
                {
                    Database.ForEach(y =>
                    {
                        if (Id == default || y.Game?.Id == Id)
                        {
                            y.Items.RemoveAll(x => x.Type == ActivityType.AchievementsGoal);
                            y.Items.RemoveAll(x => x.Type == ActivityType.AchievementsUnlocked);
                            y.Items.RemoveAll(x => x.Type == ActivityType.ScreenshotsTaked);
                            y.Items.RemoveAll(x => x.Type == ActivityType.HowLongToBeatCompleted);
                        }
                    });
                }

                FirstScanSuccessStory(Id);
                FirstScanScreenshotsVisualizer(Id);
                if (!forced)
                {
                    FirstScanGameActivity(Id);
                }
                FirstScanHowLongToBeat(Id);

                Database.EndBufferUpdate();
            }, globalProgressOptions);
        }


        public void FirstScanSuccessStory(Guid Id = default)
        {
            if (Directory.Exists(SuccessStoryPath))
            {
                _ = Parallel.ForEach(Directory.EnumerateFiles(SuccessStoryPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out Guid fileId))
                        {
                            if (Id == default || fileId == Id)
                            {
                                SetAchievements(fileId);
                            }
                        }
                    });
            }
        }

        public void SetAchievements(Guid Id)
        {
            try
            {
                List<ulong> AchievementsGoals = new List<ulong> { 100, 90, 75, 50, 25 };

                string PathData = Path.Combine(SuccessStoryPath, Id + ".json");
                if (File.Exists(PathData))
                {
                    GameAchievements obj = Serialization.FromJsonFile<GameAchievements>(PathData);
                    if (obj.Items?.Count() > 0 && obj.Progression > 0)
                    {
                        PlayerActivitiesData playerActivitiesData = Get(Id);
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


        public void FirstScanScreenshotsVisualizer(Guid Id = default)
        {
            if (Directory.Exists(SuccessStoryPath))
            {
                _ = Parallel.ForEach(Directory.EnumerateFiles(ScreenshotsVisuliazerPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out var fileId))
                        {
                            if (Id == default(Guid) || fileId == Id)
                            {
                                SetScreenshots(fileId);
                            }
                        }
                    });
            }
        }

        public void SetScreenshots(Guid Id)
        {
            try
            {
                List<ulong> AchievementsGoals = new List<ulong> { 100, 90, 75, 50, 25 };

                string PathData = Path.Combine(ScreenshotsVisuliazerPath, Id + ".json");
                if (File.Exists(PathData))
                {
                    GameScreenshots obj = Serialization.FromJsonFile<GameScreenshots>(PathData);
                    if (obj.Items?.Count() > 0)
                    {
                        PlayerActivitiesData playerActivitiesData = Get(Id);
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


        public void FirstScanGameActivity(Guid Id = default)
        {
            if (Directory.Exists(SuccessStoryPath))
            {
                _ = Parallel.ForEach(Directory.EnumerateFiles(GameActivityPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out Guid fileId))
                        {
                            if (Id == default(Guid) || fileId == Id)
                            {
                                SetGameActivity(fileId);
                            }
                        }
                    });
            }
        }

        private void SetGameActivity(Guid Id)
        {
            try
            {
                string PathData = Path.Combine(GameActivityPath, Id + ".json");
                if (File.Exists(PathData))
                {
                    GameActivities obj = Serialization.FromJsonFile<GameActivities>(PathData);
                    if (obj.Items?.Count() > 0)
                    {
                        PlayerActivitiesData playerActivitiesData = Get(Id);
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


        public void FirstScanHowLongToBeat(Guid Id = default)
        {
            if (Directory.Exists(HowLongToBeatPath))
            {
                _ = Parallel.ForEach(Directory.EnumerateFiles(HowLongToBeatPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out Guid fileId))
                        {
                            if (Id == default || fileId == Id)
                            {
                                SetHowLongToBeat(fileId);
                            }
                        }
                    });
            }
        }

        public void SetHowLongToBeat(Guid Id)
        {
            try
            {
                string PathData = Path.Combine(HowLongToBeatPath, Id + ".json");
                if (File.Exists(PathData))
                {
                    GameHowLongToBeat obj = Serialization.FromJsonFile<GameHowLongToBeat>(PathData);
                    if (obj.Items?.Count() > 0)
                    {
                        PlayerActivitiesData playerActivitiesData = Get(Id);
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


        public ObservableCollection<ActivityListGrouped> GetActivitiesData(Guid Id)
        {
            ObservableCollection<ActivityListGrouped> data = GetActivitiesData(false);
            return data.Where(x => x.GameContext.Id == Id)?.ToObservable();
        }

        public ObservableCollection<ActivityListGrouped> GetActivitiesData(bool grouped = true)
        {
            ObservableCollection<ActivityList> activityLists = new ObservableCollection<ActivityList>();
            Database.ForEach(x =>
            {
                x.Items.ForEach(y =>
                {
                    activityLists.Add(new ActivityList
                    {
                        GameContext = x.Game,
                        DateActivity = y.DateActivity,
                        Type = y.Type,
                        Value = y.Value
                    });
                });
            });


            // Options
            List<ActivityType> activityTypes = new List<ActivityType> { ActivityType.PlaytimeFirst, ActivityType.PlaytimeGoal };
            if (PluginSettings.Settings.EnableHowLongToBeatData)
            {
                activityTypes.Add(ActivityType.HowLongToBeatCompleted);
            }
            if (PluginSettings.Settings.EnableScreenshotsVisualizerData)
            {
                activityTypes.Add(ActivityType.ScreenshotsTaked);
            }
            if (PluginSettings.Settings.EnableSuccessStoryData)
            {
                activityTypes.Add(ActivityType.AchievementsGoal);
                activityTypes.Add(ActivityType.AchievementsUnlocked);
            }


            ObservableCollection<ActivityListGrouped> activityListsGrouped = new ObservableCollection<ActivityListGrouped>();
            Game GameContext = null;
            string TimeAgo = string.Empty;

            activityLists = activityLists.OrderByDescending(x => x.DateActivity).ToObservable();
            activityLists.Where(x => activityTypes.Any(y => y == x.Type)).ForEach(x =>
            {
                IEnumerable<ActivityListGrouped> finded = activityListsGrouped.Where(z => z.GameContext.Id == x.GameContext.Id && (!grouped || z.TimeAgo.IsEqual(x.TimeAgo)));
                if (finded.Count() > 0)
                {
                    finded.First().Activities.Add(new Activity
                    {
                        DateActivity = x.DateActivity,
                        Value = x.Value,
                        Type = x.Type
                    });
                }
                else
                {
                    GameContext = x.GameContext;
                    TimeAgo = x.TimeAgo;
                    activityListsGrouped.Add(new ActivityListGrouped
                    {
                        GameContext = x.GameContext,
                        DtString = x.DateActivity.ToString("yyyy-MM-dd"),
                        TimeAgo = x.TimeAgo,
                        Activities = new List<Activity>
                            {
                                new Activity
                                {
                                    DateActivity = x.DateActivity,
                                    Value = x.Value,
                                    Type = x.Type
                                }
                            }
                    });
                }
            });

            // Order grouped activities
            activityListsGrouped.ForEach(x =>
            {
                _ = x.Activities.OrderByDescending(y => y.DateActivity).ThenBy(y => y.Type);
            });

            return activityListsGrouped;
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

                playerFriends = playerFriends.Concat(gogs).Concat(steams).Concat(origin).ToList();

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
                FriendsDataLoaderClose();
            });
        }

        private void FriendsDataLoaderClose()
        {
            if (WindowFriendsDataLoading != null)
            {
                _ = Application.Current.Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    WindowFriendsDataLoading.Close();
                }));

                StopWatchFriendsDataLoading.Stop();
                TimeSpan ts = StopWatchFriendsDataLoading.Elapsed;
                Logger.Info($"RefreshFriendsDataLoader" + (FriendsDataIsCanceled ? " (canceled) " : "")
                    + $" - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");

                FriendsDataIsCanceled = false;
                FriendsDataIsDownloaded = true;
            }
        }
    }
}
