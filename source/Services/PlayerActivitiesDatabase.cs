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

namespace PlayerActivities.Services
{
    public class PlayerActivitiesDatabase : PluginDatabaseObject<PlayerActivitiesSettingsViewModel, PlayerActivitiesCollection, Models.PlayerActivitiesData, Activity>
    {
        public string successStoryPath { get; set; }
        public string gameActivityPath { get; set; }
        public string screenshotsVisuliazerPath { get; set; }
        public string howLongToBeatPath { get; set; }


        private bool _FriendsDataIsDownloaded = true;
        public bool FriendsDataIsDownloaded { get => _FriendsDataIsDownloaded; set => SetValue(ref _FriendsDataIsDownloaded, value); }

        private bool _FriendsDataIsCanceled = false;
        public bool FriendsDataIsCanceled { get => _FriendsDataIsCanceled; set => SetValue(ref _FriendsDataIsCanceled, value); }

        Window windowFriendsDataLoading = null;
        Stopwatch stopWatchFriendsDataLoading = new Stopwatch();

        private FriendsDataLoading _friendsDataLoading = new FriendsDataLoading();
        public FriendsDataLoading friendsDataLoading { get => _friendsDataLoading; set => SetValue(ref _friendsDataLoading, value); }


        private HltbUserStats _hltbUserStats;
        private HltbUserStats hltbUserStats
        {
            get
            {
                if (_hltbUserStats == null)
                {
                    string PathHltbUserStats = Path.Combine(howLongToBeatPath, "..", "HltbUserStats.json");
                    if (File.Exists(PathHltbUserStats))
                    {
                        try
                        {
                            _hltbUserStats = Serialization.FromJsonFile<HltbUserStats>(PathHltbUserStats);
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


        public PlayerActivitiesDatabase(IPlayniteAPI PlayniteApi, PlayerActivitiesSettingsViewModel PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, "PlayerActivities", PluginUserDataPath)
        {
            successStoryPath = Path.Combine(Paths.PluginUserDataPath, "..", "cebe6d32-8c46-4459-b993-5a5189d60788", "SuccessStory");
            gameActivityPath = Path.Combine(Paths.PluginUserDataPath, "..", "afbb1a0d-04a1-4d0c-9afa-c6e42ca855b4", "GameActivity");
            screenshotsVisuliazerPath = Path.Combine(Paths.PluginUserDataPath, "..", "c6c8276f-91bf-48e5-a1d1-4bee0b493488", "ScreenshotsVisualizer");
            howLongToBeatPath = Path.Combine(Paths.PluginUserDataPath, "..", "e08cd51f-9c9a-4ee3-a094-fde03b55492f", "HowLongToBeat");
        }


        protected override bool LoadDatabase()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Database = new PlayerActivitiesCollection(Paths.PluginDatabasePath);
                Database.SetGameInfo<Activity>(PlayniteApi);

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"LoadDatabase with {Database.Count} items - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
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
                Game game = PlayniteApi.Database.Games.Get(Id);
                if (game != null)
                {
                    playerActivities = GetDefault(game);
                    Add(playerActivities);
                }
            }
            return playerActivities;
        }


        #region Plugin data
        public void InitializePluginData(bool forced = false)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                $"{PluginName} - {resources.GetString("LOCCommonProcessing")}",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Thread.Sleep(5000);

                // Remove existing data
                if (forced)
                {
                    Database.ForEach(y =>
                    {
                        y.Items.RemoveAll(x => x.Type == ActivityType.AchievementsGoal);
                        y.Items.RemoveAll(x => x.Type == ActivityType.AchievementsUnlocked);
                        y.Items.RemoveAll(x => x.Type == ActivityType.ScreenshotsTaked);
                        y.Items.RemoveAll(x => x.Type == ActivityType.HowLongToBeatCompleted);
                    });
                }

                FirstScanSuccessStory();
                FirstScanScreenshotsVisualizer();
                if (!forced)
                {
                    FirstScanGameActivity();
                }
                FirstScanHowLongToBeat();
            }, globalProgressOptions);
        }


        public void FirstScanSuccessStory()
        {
            if (Directory.Exists(successStoryPath))
            {
                Parallel.ForEach(Directory.EnumerateFiles(successStoryPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out var fileId))
                        {
                            SetAchievements(fileId);
                        }
                    });
            }
        }

        public void SetAchievements(Guid Id)
        {
            try
            {
                List<ulong> AchievementsGoals = new List<ulong> { 100, 90, 75, 50, 25 };

                string PathData = Path.Combine(successStoryPath, Id + ".json");
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

                            playerActivitiesData.Items.Add(new Models.Activity
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
                                    playerActivitiesData.Items.Add(new Models.Activity
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


        public void FirstScanScreenshotsVisualizer()
        {
            if (Directory.Exists(successStoryPath))
            {
                Parallel.ForEach(Directory.EnumerateFiles(screenshotsVisuliazerPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out var fileId))
                        {
                            SetScreenshots(fileId);
                        }
                    });
            }
        }

        public void SetScreenshots(Guid Id)
        {
            try
            {
                List<ulong> AchievementsGoals = new List<ulong> { 100, 90, 75, 50, 25 };

                string PathData = Path.Combine(screenshotsVisuliazerPath, Id + ".json");
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


        public void FirstScanGameActivity()
        {
            if (Directory.Exists(successStoryPath))
            {
                Parallel.ForEach(Directory.EnumerateFiles(gameActivityPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out Guid fileId))
                        {
                            SetGameActivity(fileId);
                        }
                    });
            }
        }

        private void SetGameActivity(Guid Id)
        {
            try
            {
                string PathData = Path.Combine(gameActivityPath, Id + ".json");
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


        public void FirstScanHowLongToBeat()
        {
            if (Directory.Exists(howLongToBeatPath))
            {
                Parallel.ForEach(Directory.EnumerateFiles(howLongToBeatPath, "*.json"),
                    (objectFile) =>
                    {
                        if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out Guid fileId))
                        {
                            SetHowLongToBeat(fileId);
                        }
                    });
            }
        }

        public void SetHowLongToBeat(Guid Id)
        {
            try
            {
                if (Id.ToString().IsEqual("bfee5d46-904d-45f2-b1f1-5ba97f28c74a"))
                {

                }

                string PathData = Path.Combine(howLongToBeatPath, Id + ".json");
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
                        if (hltbUserStats != null)
                        {
                            List<TitleList> titleLists = hltbUserStats.TitlesList.FindAll(x => x.Id == obj.GetData().Id).ToList();
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
        #endregion


        public List<PlayerFriends> GetFriends(PlayerActivities plugin, bool force = false)
        {
            List<PlayerFriends> playerFriends = new List<PlayerFriends>();

            if (force)
            {
                List<PlayerFriends> gogs = new List<PlayerFriends>();
                if (PluginSettings.Settings.EnableGogFriends)
                {
                    friendsDataLoading.FriendName = string.Empty;
                    friendsDataLoading.ActualCount = 0;
                    friendsDataLoading.FriendCount = 0;
                    friendsDataLoading.SourceName = "Gog";

                    GogFriends gogFriends = new GogFriends();
                    gogs = gogFriends.GetFriends();
                }

                List<PlayerFriends> steams = new List<PlayerFriends>();
                if (PluginSettings.Settings.EnableSteamFriends && !FriendsDataIsCanceled)
                {
                    friendsDataLoading.FriendName = string.Empty;
                    friendsDataLoading.ActualCount = 0;
                    friendsDataLoading.FriendCount = 0;
                    friendsDataLoading.SourceName = "Steam";

                    SteamFriends steamFriends = new SteamFriends();
                    steams = steamFriends.GetFriends();
                }

                List<PlayerFriends> origin = new List<PlayerFriends>();
                if (PluginSettings.Settings.EnableOriginFriends && !FriendsDataIsCanceled)
                {
                    friendsDataLoading.FriendName = string.Empty;
                    friendsDataLoading.ActualCount = 0;
                    friendsDataLoading.FriendCount = 0;
                    friendsDataLoading.SourceName = "Origin";

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
                else
                {
                    return GetFriends(plugin, true);
                }
            }

            return playerFriends;
        }

        public async Task RefreshFriendsDataLoader(PlayerActivities plugin)
        {
            FriendsDataIsDownloaded = false;
            await Task.Run(() =>
            {
                stopWatchFriendsDataLoading = new Stopwatch();
                stopWatchFriendsDataLoading.Start();

                Database = new PlayerActivitiesCollection(Paths.PluginDatabasePath);
                Database.SetGameInfo<Activity>(PlayniteApi);

                WindowOptions windowOptions = new WindowOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = false,
                    ShowCloseButton = false,
                    CanBeResizable = false
                };

                Application.Current.Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    PaFriendDataLoading view = new PaFriendDataLoading();
                    windowFriendsDataLoading = PlayniteUiHelper.CreateExtensionWindow(API.Instance, resources.GetString("LOCCommonGettingData"), view, windowOptions);
                    windowFriendsDataLoading.ShowDialog();
                }));

                GetFriends(plugin, true);
                FriendsDataLoaderClose();
            });
        }

        private void FriendsDataLoaderClose()
        {
            if (windowFriendsDataLoading != null)
            {
                Application.Current.Dispatcher?.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    windowFriendsDataLoading.Close();
                }));

                stopWatchFriendsDataLoading.Stop();
                TimeSpan ts = stopWatchFriendsDataLoading.Elapsed;
                logger.Info($"RefreshFriendsDataLoader" + (FriendsDataIsCanceled ? " (canceled) " : "")
                    + $" - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");

                FriendsDataIsCanceled = false;
                FriendsDataIsDownloaded = true;
            }
        }
    }
}
