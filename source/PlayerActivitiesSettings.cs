using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities
{
    public class PlayerActivitiesSettings : ObservableObject
    {
        #region Settings variables
        public bool MenuInExtensions { get; set; } = true;

        public DateTime LastFriendsRefresh { get; set; } = DateTime.Now;

        public bool EnableIntegrationButtonHeader { get; set; } = false;
        public bool EnableIntegrationButtonSide { get; set; } = true;


        public bool EnableSteamFriends { get; set; } = false;
        public bool EnableGogFriends { get; set; } = false;
        public bool EnableOriginFriends { get; set; } = false;


        public bool IsFirstRun { get; set; } = true;


        public bool EnableSuccessStoryData { get; set; } = true;
        public bool EnableScreenshotsVisualizerData { get; set; } = true;
        public bool EnableHowLongToBeatData { get; set; } = true;
        #endregion

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed
        #endregion
    }

    public class PlayerActivitiesSettingsViewModel : ObservableObject, ISettings
    {
        private PlayerActivities Plugin { get; }
        private PlayerActivitiesSettings editingClone { get; set; }

        private PlayerActivitiesSettings settings;
        public PlayerActivitiesSettings Settings { get => settings; set => SetValue(ref settings, value); }



        public PlayerActivitiesSettingsViewModel(PlayerActivities plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            Plugin = plugin;

            // Load saved settings.
            PlayerActivitiesSettings savedSettings = plugin.LoadPluginSettings<PlayerActivitiesSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            Settings = savedSettings ?? new PlayerActivitiesSettings();
        }

        // Code executed when settings view is opened and user starts editing values.
        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        // Code executed when user decides to cancel any changes made since BeginEdit was called.
        // This method should revert any changes made to Option1 and Option2.
        public void CancelEdit()
        {
            Settings = editingClone;
        }

        // Code executed when user decides to confirm changes made since BeginEdit was called.
        // This method should save settings made to Option1 and Option2.
        public void EndEdit()
        {
            PlayerActivities.SteamApi.Save();
            PlayerActivities.SteamApi.CurrentUser = null;
            if (Settings.EnableSteamFriends)
            {
                _ = PlayerActivities.SteamApi.CurrentUser;
            }

            Plugin.SavePluginSettings(Settings);
            PlayerActivities.PluginDatabase.PluginSettings = this;

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