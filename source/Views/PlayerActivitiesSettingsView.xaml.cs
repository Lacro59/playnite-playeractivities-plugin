using CommonPluginsControls.Stores;
using CommonPluginsControls.Stores.Models;
using CommonPluginsShared;
using PlayerActivities.Services;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace PlayerActivities.Views
{
    /// <summary>
    /// Settings view: registers store auth panels into <see cref="StoresSettingsView"/>.
    /// </summary>
    public partial class PlayerActivitiesSettingsView : UserControl
    {
        private PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        private bool _storeUiInitialized;
        private PlayerActivitiesSettings _subscribedSettings;

        public PlayerActivitiesSettingsView()
        {
            Stopwatch sw = Stopwatch.StartNew();
            InitializeComponent();
            Common.LogDebug($"[PaSettingsOpen] InitializeComponent elapsedMs={sw.ElapsedMilliseconds}");
            Loaded += PlayerActivitiesSettingsView_Loaded;
            Unloaded += PlayerActivitiesSettingsView_Unloaded;
            DataContextChanged += PlayerActivitiesSettingsView_DataContextChanged;
        }

        private void PlayerActivitiesSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            AttachFriendsSettingsListeners();

            if (_storeUiInitialized)
            {
                StoreSettingsLog.Debug("PlayerActivities settings view re-opened; refreshing store panels");
                Stopwatch swReopen = Stopwatch.StartNew();
                RefreshStorePanels();
                RequestStorePanelsBackgroundAuthRefresh();
                Common.LogDebug($"[PaSettingsOpen] Loaded re-open refresh elapsedMs={swReopen.ElapsedMilliseconds} friends={FormatEnabledFriendsSummary()}");
                return;
            }

            // Store register / StoreApi wiring waits until the Libraries tab is selected.
            Common.LogDebug($"[PaSettingsOpen] Loaded deferred store wiring until Libraries tab friends={FormatEnabledFriendsSummary()}");
        }

        private void PART_SettingsTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            if (!ReferenceEquals(PART_SettingsTabs.SelectedItem, PART_LibrariesTab))
            {
                return;
            }

            EnsureStoreUiInitialized();
        }

        /// <summary>
        /// Registers store panels and binds <c>StoreApi</c> once, on first Libraries tab visit.
        /// </summary>
        private void EnsureStoreUiInitialized()
        {
            if (_storeUiInitialized)
            {
                return;
            }

            _storeUiInitialized = true;
            StoreSettingsLog.Debug("PlayerActivities settings view initializing store panels (Libraries tab)");
            Stopwatch sw = Stopwatch.StartNew();
            RegisterStorePanels();
            long registerMs = sw.ElapsedMilliseconds;
            sw.Restart();
            RefreshStorePanels();
            long refreshMs = sw.ElapsedMilliseconds;
            Common.LogDebug($"[PaSettingsOpen] Libraries store wiring registerMs={registerMs} refreshMs={refreshMs} friends={FormatEnabledFriendsSummary()}");
        }

        private void PlayerActivitiesSettingsView_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachFriendsSettingsListeners();
        }

        private void PlayerActivitiesSettingsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!_storeUiInitialized || DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            AttachFriendsSettingsListeners();
            RefreshStorePanels();
        }

        private void AttachFriendsSettingsListeners()
        {
            PlayerActivitiesSettings settings = GetEditingSettings();
            if (ReferenceEquals(_subscribedSettings, settings))
            {
                return;
            }

            DetachFriendsSettingsListeners();

            if (settings == null)
            {
                return;
            }

            _subscribedSettings = settings;
            _subscribedSettings.PropertyChanged += Settings_PropertyChanged;
        }

        private void DetachFriendsSettingsListeners()
        {
            if (_subscribedSettings == null)
            {
                return;
            }

            _subscribedSettings.PropertyChanged -= Settings_PropertyChanged;
            _subscribedSettings = null;
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_storeUiInitialized)
            {
                return;
            }

            if (!IsFriendsStoreVisibilityProperty(e.PropertyName))
            {
                return;
            }

            StoreSettingsLog.Debug($"Friends store visibility changed: {e.PropertyName ?? "(all)"}");
            RefreshStorePanels();
            RequestStorePanelsBackgroundAuthRefresh();
        }

        private static bool IsFriendsStoreVisibilityProperty(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return true;
            }

            return propertyName == nameof(PlayerActivitiesSettings.EnableSteamFriends)
                || propertyName == nameof(PlayerActivitiesSettings.EnableEpicFriends)
                || propertyName == nameof(PlayerActivitiesSettings.EnableGogFriends)
                || propertyName == nameof(PlayerActivitiesSettings.EnableOriginFriends);
        }

        /// <summary>
        /// Binds store panels to API instances when the store is enabled for friends.
        /// </summary>
        private void RefreshStorePanels()
        {
            PlayerActivitiesSettings settings = GetEditingSettings();

            SteamPanel.StoreApi = IsSteamStoreEnabled(settings) ? PlayerActivities.SteamApi : null;
            EpicPanel.StoreApi = IsEpicStoreEnabled(settings) ? PlayerActivities.EpicApi : null;
            GogPanel.StoreApi = IsGogStoreEnabled(settings) ? PlayerActivities.GogApi : null;
            EaPanel.StoreApi = IsEaStoreEnabled(settings) ? PlayerActivities.EaApi : null;

            foreach (StoreSettingsEntry entry in StoresSettings.Stores)
            {
                if (entry.Id == "Steam")
                {
                    entry.IsVisible = IsSteamStoreEnabled(settings);
                }
                else if (entry.Id == "Epic")
                {
                    entry.IsVisible = IsEpicStoreEnabled(settings);
                }
                else if (entry.Id == "Gog")
                {
                    entry.IsVisible = IsGogStoreEnabled(settings);
                }
                else if (entry.Id == "EA")
                {
                    entry.IsVisible = IsEaStoreEnabled(settings);
                }
            }

            StoresSettings.RefreshStores();
        }

        /// <summary>
        /// Schedules a background auth check for each enabled store panel (settings re-open).
        /// </summary>
        private void RequestStorePanelsBackgroundAuthRefresh()
        {
            PlayerActivitiesSettings settings = GetEditingSettings();

            if (IsSteamStoreEnabled(settings))
            {
                (SteamPanel.DataContext as IStorePanelViewModel)?.RequestBackgroundAuthRefresh();
            }

            if (IsEpicStoreEnabled(settings))
            {
                (EpicPanel.DataContext as IStorePanelViewModel)?.RequestBackgroundAuthRefresh();
            }

            if (IsGogStoreEnabled(settings))
            {
                (GogPanel.DataContext as IStorePanelViewModel)?.RequestBackgroundAuthRefresh();
            }

            if (IsEaStoreEnabled(settings))
            {
                (EaPanel.DataContext as IStorePanelViewModel)?.RequestBackgroundAuthRefresh();
            }
        }

        private void RegisterStorePanels()
        {
            PlayerActivitiesSettings settings = GetEditingSettings();

            StoresSettings.RegisterStore(new StoreSettingsEntry
            {
                Id = "Steam",
                NameResourceKey = "LOCCommonStoreSteam",
                CategoryResourceKey = "LOCCommonStoresLaunchers",
                Panel = SteamPanel,
                IsVisible = IsSteamStoreEnabled(settings),
                SortOrder = 0
            });

            StoresSettings.RegisterStore(new StoreSettingsEntry
            {
                Id = "Epic",
                NameResourceKey = "LOCCommonStoreEpic",
                CategoryResourceKey = "LOCCommonStoresLaunchers",
                Panel = EpicPanel,
                IsVisible = IsEpicStoreEnabled(settings),
                SortOrder = 1
            });

            StoresSettings.RegisterStore(new StoreSettingsEntry
            {
                Id = "Gog",
                NameResourceKey = "LOCCommonStoreGog",
                CategoryResourceKey = "LOCCommonStoresLaunchers",
                Panel = GogPanel,
                IsVisible = IsGogStoreEnabled(settings),
                SortOrder = 2
            });

            StoresSettings.RegisterStore(new StoreSettingsEntry
            {
                Id = "EA",
                NameResourceKey = "LOCCommonStoreEa",
                CategoryResourceKey = "LOCCommonStoresLaunchers",
                Panel = EaPanel,
                IsVisible = IsEaStoreEnabled(settings),
                SortOrder = 3
            });
        }

        private PlayerActivitiesSettings GetEditingSettings()
        {
            PlayerActivitiesSettingsViewModel viewModel = DataContext as PlayerActivitiesSettingsViewModel;
            if (viewModel?.Settings != null)
            {
                return viewModel.Settings;
            }

            return PluginDatabase.PluginSettings;
        }

        private string FormatEnabledFriendsSummary()
        {
            PlayerActivitiesSettings settings = GetEditingSettings();
            return string.Format(
                "Steam={0};Epic={1};Gog={2};EA={3}",
                IsSteamStoreEnabled(settings),
                IsEpicStoreEnabled(settings),
                IsGogStoreEnabled(settings),
                IsEaStoreEnabled(settings));
        }

        private static bool IsSteamStoreEnabled(PlayerActivitiesSettings settings)
        {
            return settings != null && settings.EnableSteamFriends;
        }

        private static bool IsEpicStoreEnabled(PlayerActivitiesSettings settings)
        {
            return settings != null && settings.EnableEpicFriends;
        }

        private static bool IsGogStoreEnabled(PlayerActivitiesSettings settings)
        {
            return settings != null && settings.EnableGogFriends;
        }

        private static bool IsEaStoreEnabled(PlayerActivitiesSettings settings)
        {
            return settings != null && settings.EnableOriginFriends;
        }
    }
}
