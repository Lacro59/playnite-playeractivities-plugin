using CommonPluginsControls.Stores;
using CommonPluginsControls.Stores.Models;
using PlayerActivities.Services;
using System.ComponentModel;
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
            InitializeComponent();
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
                RefreshStorePanels();
                RequestStorePanelsBackgroundAuthRefresh();
                return;
            }

            _storeUiInitialized = true;
            StoreSettingsLog.Debug("PlayerActivities settings view initializing store panels");
            RegisterStorePanels();
            RefreshStorePanels();
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
                || propertyName == nameof(PlayerActivitiesSettings.EnableGogFriends);
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

        private static bool IsSteamStoreEnabled(PlayerActivitiesSettings settings)
        {
            return settings != null
                && settings.PluginState.SteamIsEnabled
                && settings.EnableSteamFriends;
        }

        private static bool IsEpicStoreEnabled(PlayerActivitiesSettings settings)
        {
            return settings != null
                && settings.PluginState.EpicIsEnabled
                && settings.EnableEpicFriends;
        }

        private static bool IsGogStoreEnabled(PlayerActivitiesSettings settings)
        {
            return settings != null
                && settings.PluginState.GogIsEnabled
                && settings.EnableGogFriends;
        }
    }
}
