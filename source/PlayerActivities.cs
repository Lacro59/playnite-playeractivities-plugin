using CommonPluginsShared;
using CommonPluginsShared.Controls;
using CommonPluginsShared.PlayniteExtended;
using PlayerActivities.Clients;
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
        public override Guid Id { get; } = Guid.Parse("3dbe2ee4-d6c9-4c09-b377-7a086369603e");

        internal TopPanelItem topPanelItem;
        internal PaViewSidebar paViewSidebar;


        public PlayerActivities(IPlayniteAPI api) : base(api)
        {        
            // Custom theme button
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnCustomThemeButtonClick));

            // Custom elements integration
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                ElementList = new List<string> {  },
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
                topPanelItem = new TopPanelItem()
                {
                    Icon = new TextBlock
                    {
                        Text = "\ueea5",
                        FontSize = 20,
                        FontFamily = resources.GetResource("FontIcoFont") as FontFamily
                    },
                    Title = resources.GetString("LOCSPa"),
                    Activated = () =>
                    {
                        WindowOptions windowOptions = new WindowOptions
                        {
                            ShowMinimizeButton = false,
                            ShowMaximizeButton = true,
                            ShowCloseButton = true,
                            Width = 1280,
                            Height = 740
                        };

                        PaView ViewExtension = new PaView(this);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSPa"), ViewExtension, windowOptions);
                        windowExtension.ResizeMode = ResizeMode.CanResize;
                        windowExtension.ShowDialog();
                    },
                    Visible = PluginSettings.Settings.EnableIntegrationButtonHeader
                };

                paViewSidebar = new PaViewSidebar(this);
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
            yield return topPanelItem;
        }

        // List custom controls
        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            return null;
        }

        // SidebarItem
        public class PaViewSidebar : SidebarItem
        {
            public PaViewSidebar(PlayerActivities plugin)
            {
                Type = SiderbarItemType.View;
                Title = resources.GetString("LOCPa");
                Icon = new TextBlock
                {
                    Text = "\ueea5",
                    FontFamily = resources.GetResource("FontIcoFont") as FontFamily
                };
                Opened = () =>
                {
                    SidebarItemControl sidebarItemControl = new SidebarItemControl(PluginDatabase.PlayniteApi);
                    sidebarItemControl.SetTitle(resources.GetString("LOCPa"));
                    sidebarItemControl.AddContent(new PaView(plugin));

                    return sidebarItemControl;
                };
                Visible = plugin.PluginSettings.Settings.EnableIntegrationButtonSide;
            }
        }

        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            return new List<SidebarItem>
            {
                paViewSidebar
            };
        }
        #endregion


        #region Menus
        // To add new game menu items override GetGameMenuItems
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            Game GameMenu = args.Games.First();
            List<Guid> Ids = args.Games.Select(x => x.Id).ToList();
            Models.PlayerActivitiesData playerActivities = PluginDatabase.Get(GameMenu);

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>();


#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = resources.GetString("LOCPa"),
                Description = "-"
            });
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = resources.GetString("LOCPa"),
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
                MenuSection = MenuInExtensions + resources.GetString("LOCPa"),
                Description = resources.GetString("LOCCommonRefreshAllData"),
                Action = (mainMenuItem) =>
                {
                    PluginDatabase.InitializePluginData(true);
                }
            });


#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCPa"),
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCPa"),
                Description = "Test",
                Action = (mainMenuItem) => 
                {
                    OriginFriends originFriends = new OriginFriends();
                    originFriends.GetFriends();
                }
            });
#endif

            return mainMenuItems;
        }
        #endregion


        #region Game event
        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {

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
                    Thread.Sleep(5000);
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