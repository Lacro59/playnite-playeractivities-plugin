using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace PlayerActivities
{
    public class PlayerActivitiesSettings : PluginSettings
    {
        #region Settings variables
        public DateTime LastFriendsRefresh { get; set; } = DateTime.Now;

        public bool EnableIntegrationButtonHeader { get; set; } = false;
        public bool EnableIntegrationButtonSide { get; set; } = true;

        private bool _enableIntegrationActivities = true;
        public bool EnableIntegrationActivities { get => _enableIntegrationActivities; set => SetValue(ref _enableIntegrationActivities, value); }

        public bool EnableSteamFriends { get; set; } = false;
        public bool EnableGogFriends { get; set; } = false;
        public bool EnableEpicFriends { get; set; } = false;
        public bool EnableOriginFriends { get; set; } = false;


        public bool IsFirstRun { get; set; } = true;


        public bool EnableSuccessStoryData { get; set; } = true;
        public bool EnableScreenshotsVisualizerData { get; set; } = true;
        public bool EnableHowLongToBeatData { get; set; } = true;

        public bool SaveColumnOrder { get; set; } = false;


        public StoreSettings SteamStoreSettings { get; set; } = new StoreSettings { ForceAuth = true, UseAuth = true, UseApi = false };
        public StoreSettings GogStoreSettings { get; set; } = new StoreSettings { ForceAuth = true, UseAuth = true };
        public StoreSettings EpicStoreSettings { get; set; } = new StoreSettings { ForceAuth = true, UseAuth = true };
        #endregion

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed

        #endregion
    }

    /// <summary>
    /// Playnite settings view-model for PlayerActivities (edit lifecycle + store sync).
    /// </summary>
    public class PlayerActivitiesSettingsViewModel : PluginSettingsViewModel, IPluginSettingsViewModel
    {
        private PlayerActivities Plugin { get; }
        private PlayerActivitiesSettings EditingClone { get; set; }

        private PlayerActivitiesSettings _settings;
        public PlayerActivitiesSettings Settings { get => _settings; set => SetValue(ref _settings, value); }

        IPluginSettings IPluginSettingsViewModel.Settings => Settings;

        public PlayerActivitiesSettingsViewModel(PlayerActivities plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            Plugin = plugin;

            // Load saved settings.
            PlayerActivitiesSettings savedSettings = plugin.LoadPluginSettings<PlayerActivitiesSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            Settings = savedSettings ?? new PlayerActivitiesSettings();

            // Temporary: force Steam auth until store settings migration is complete (see .tasks/TODO-FIXME.md).
            Settings.SteamStoreSettings.ForceAuth = true;
        }

        // Code executed when settings view is opened and user starts editing values.
        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
        }

        // Code executed when user decides to cancel any changes made since BeginEdit was called.
        // This method should revert any changes made to Option1 and Option2.
        public void CancelEdit()
        {
            CopySettingsValues(EditingClone, Settings);
        }

        // Code executed when user decides to confirm changes made since BeginEdit was called.
        // This method should save settings made to Option1 and Option2.
        public void EndEdit()
        {
            // StoreAPI intialization
            PlayerActivities.SteamApi.SaveSettings(Settings.SteamStoreSettings, Settings.PluginState.SteamIsEnabled && Settings.EnableSteamFriends);
            PlayerActivities.EpicApi.SaveSettings(Settings.EpicStoreSettings, Settings.PluginState.EpicIsEnabled && Settings.EnableEpicFriends);
            PlayerActivities.GogApi.SaveSettings(Settings.GogStoreSettings, Settings.PluginState.GogIsEnabled && Settings.EnableGogFriends);

            PersistSettings(Plugin, Settings);
            PlayerActivities.PluginDatabase.PluginSettings = Settings;

            if (API.Instance.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                Plugin.TopPanelItem.Visible = Settings.EnableIntegrationButtonHeader;
                Plugin.SidebarItem.Visible = Settings.EnableIntegrationButtonSide;
            }

            this.OnPropertyChanged();
        }

        // Code execute when user decides to confirm changes made since BeginEdit was called.
        // Executed before EndEdit is called and EndEdit is not called if false is returned.
        // List of errors is presented to user if verification fails.
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
