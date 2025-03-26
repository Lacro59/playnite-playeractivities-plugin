using CommonPluginsShared;
using CommonPluginsShared.Controls;
using CommonPluginsShared.PlayniteExtended;
using CommonPluginsStores.Epic;
using CommonPluginsStores.Gog;
using CommonPluginsStores.Steam;
using PlayerActivities.Clients;
using PlayerActivities.Controls;
using PlayerActivities.Models;
using PlayerActivities.Services;
using PlayerActivities.Views;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlayerActivities
{
    public class PlayerActivities : PluginExtended<PlayerActivitiesSettingsViewModel, PlayerActivitiesDatabase>
    {
        public override Guid Id => Guid.Parse("3dbe2ee4-d6c9-4c09-b377-7a086369603e");

        internal TopPanelItem TopPanelItem { get; set; }
        internal SidebarItem SidebarItem { get; set; }
        internal SidebarItemControl SidebarItemControl { get; set; }

        public static SteamApi SteamApi { get; set; }
        public static GogApi GogApi { get; set; }
        public static EpicApi EpicApi { get; set; }


        public PlayerActivities(IPlayniteAPI api) : base(api)
        {
            // Custom theme button
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnCustomThemeButtonClick));

            // Custom elements integration
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                ElementList = new List<string> { "PluginActivities" },
                SourceName = "PlayerActivities"
            });

            // Settings integration
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "PlayerActivities",
                SettingsRoot = $"{nameof(PluginSettings)}.{nameof(PluginSettings.Settings)}"
            });

            // Initialize top & side bar
            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                TopPanelItem = new PaTopPanelItem(this);
                SidebarItem = new PaViewSidebar(this);
            }
        }



        #region Custom event
        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {

        }
        #endregion


        #region Theme integration
        // Button on top panel
        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            yield return TopPanelItem;
        }

        // List custom controls
        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            return args.Name == "PluginActivities" ? new PluginActivities() : null;
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            yield return SidebarItem;
        }
        #endregion


        #region Menus
        // To add new game menu items override GetGameMenuItems
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            Game GameMenu = args.Games.First();
            List<Guid> Ids = args.Games.Select(x => x.Id).ToList();
            PlayerActivitiesData playerActivities = PluginDatabase.Get(GameMenu);

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>();


#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = ResourceProvider.GetString("LOCPa"),
                Description = "-"
            });
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = ResourceProvider.GetString("LOCPa"),
                Description = "Test",
                Action = (mainMenuItem) =>
                {

                }
            });
#endif

            return gameMenuItems;
        }

        // To add new main menu items override GetMainMenuItems
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string MenuInExtensions = string.Empty;
            if (PluginSettings.Settings.MenuInExtensions)
            {
                MenuInExtensions = "@";
            }

            List<MainMenuItem> mainMenuItems = new List<MainMenuItem>();

            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCPa"),
                Description = ResourceProvider.GetString("LOCPaView"),
                Action = (mainMenuItem) =>
                {
                    WindowOptions windowOptions = new WindowOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = true,
                        ShowCloseButton = true,
                        CanBeResizable = true,
                        Width = 1280,
                        Height = 740
                    };

                    PaView ViewExtension = new PaView(this);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCPa"), ViewExtension, windowOptions);
                    windowExtension.ShowDialog();
                }
            });

            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCPa"),
                Description = "-"
            });

            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCPa"),
                Description = ResourceProvider.GetString("LOCCommonRefreshAllData"),
                Action = (mainMenuItem) =>
                {
                    PluginDatabase.InitializePluginData(true);
                }
            });

            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCPa"),
                Description = ResourceProvider.GetString("LOCPaRefreshFriendsData"),
                Action = (mainMenuItem) =>
                {
                    Task.Run(() => PluginDatabase.RefreshFriendsDataLoader(this));
                }
            });


#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCPa"),
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCPa"),
                Description = "Test",
                Action = (mainMenuItem) =>
                {

                }
            });
#endif

            return mainMenuItems;
        }
        #endregion


        #region Game event
        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            try
            {
                if (args.NewValue?.Count == 1 && PluginDatabase.IsLoaded)
                {
                    PluginDatabase.GameContext = args.NewValue[0];
                    PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        // Add code to be executed when game is finished installing.
        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {

        }

        // Add code to be executed when game is uninstalled.
        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStarting(OnGameStartingEventArgs args)
        {

        }

        // Add code to be executed when game is started running.
        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            try
            {
                PlayerActivitiesData playerActivities = PluginDatabase.Get(args.Game);
                if (playerActivities.IsFirst())
                {
                    playerActivities.Items.Add(new Activity
                    {
                        Type = ActivityType.PlaytimeFirst
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            Task.Run(() =>
            {
                try
                {
                    Thread.Sleep(10000);
                    PlayerActivitiesData playerActivities = PluginDatabase.Get(args.Game);

                    // Playtime first
                    if (args.Game.PlayCount == 1 && playerActivities.Items.Find(x => x.Type == ActivityType.PlaytimeFirst) == null)
                    {
                        playerActivities.Items.Add(new Activity
                        {
                            Type = ActivityType.PlaytimeFirst
                        });
                    }

                    // Playtime goal
                    List<ulong> PlaytimeGoals = new List<ulong> { 1000, 900, 800, 700, 600, 500, 400, 300, 200, 100, 50, 25, 10, 5 };
                    PlaytimeGoals.ForEach(x =>
                    {
                        if (args.Game.Playtime >= 3600 * x && playerActivities.Items.Find(z => z.Type == ActivityType.PlaytimeGoal && z.Value == x) == null)
                        {
                            playerActivities.Items.Add(new Activity
                            {
                                Type = ActivityType.PlaytimeGoal,
                                Value = x
                            });
                            return;
                        }
                    });

                    // Achievements
                    playerActivities.Items.RemoveAll(x => x.Type == ActivityType.AchievementsGoal);
                    playerActivities.Items.RemoveAll(x => x.Type == ActivityType.AchievementsUnlocked);
                    PluginDatabase.SetAchievements(args.Game.Id);

                    // Screenshots
                    playerActivities.Items.RemoveAll(x => x.Type == ActivityType.ScreenshotsTaked);
                    PluginDatabase.SetScreenshots(args.Game.Id);

                    // HowLongToBeat
                    playerActivities.Items.RemoveAll(x => x.Type == ActivityType.HowLongToBeatCompleted);
                    PluginDatabase.SetHowLongToBeat(args.Game.Id);

                    PluginDatabase.AddOrUpdate(playerActivities);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, true, PluginDatabase.PluginName);
                }
            });
        }
        #endregion


        #region Application event
        // Add code to be executed when Playnite is initialized.
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            if (PluginSettings.Settings.IsFirstRun)
            {
                PluginDatabase.InitializePluginData();
                PluginSettings.Settings.IsFirstRun = false;
                this.SavePluginSettings(PluginSettings.Settings);
            }

            // StoreAPI intialization
            if (PluginDatabase.PluginSettings.Settings.PluginState.SteamIsEnabled)
            {
                SteamApi = new SteamApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.CheckDlc);
                SteamApi.SetLanguage(API.Instance.ApplicationSettings.Language);
                SteamApi.StoreSettings = PluginDatabase.PluginSettings.Settings.SteamStoreSettings;
                _ = SteamApi.CurrentAccountInfos;
            }

            if (PluginDatabase.PluginSettings.Settings.PluginState.GogIsEnabled)
            {
                GogApi = new GogApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.CheckDlc);
                GogApi.SetLanguage(API.Instance.ApplicationSettings.Language);
                GogApi.SetForceAuth(true);
                GogApi.StoreSettings = PluginDatabase.PluginSettings.Settings.GogStoreSettings;
                _ = GogApi.CurrentAccountInfos;
            }

            if (PluginDatabase.PluginSettings.Settings.PluginState.EpicIsEnabled)
            {
                EpicApi = new EpicApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.CheckDlc);
                EpicApi.SetLanguage(API.Instance.ApplicationSettings.Language);
                EpicApi.SetForceAuth(true);
                EpicApi.StoreSettings = PluginDatabase.PluginSettings.Settings.EpicStoreSettings;
                _ = EpicApi.CurrentAccountInfos;
            }
        }

        // Add code to be executed when Playnite is shutting down.
        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {

        }
        #endregion


        // Add code to be executed when library is updated.
        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {

        }


        #region Settings
        public override ISettings GetSettings(bool firstRunSettings)
        {
            return PluginSettings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new PlayerActivitiesSettingsView();
        }
        #endregion
    }
}