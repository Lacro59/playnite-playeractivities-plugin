using CommonPluginsShared;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Controls
{
    /// <summary>
    /// Helper class to interact with the HowLongToBeat plugin.
    /// </summary>
    public class HowLongToBeatPlugin
    {
        // Reference to the plugin database
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        // Gets the HowLongToBeat plugin instance based on its GUID
        private static Plugin Plugin => API.Instance?.Addons?.Plugins?
            .FirstOrDefault(p => p.Id == PlayniteTools.GetPluginId(ExternalPlugin.HowLongToBeat));

        /// <summary>
        /// Checks if the HowLongToBeat plugin is installed.
        /// </summary>
        public static bool IsInstalled => Plugin != null;

        /// <summary>
        /// Triggers the HowLongToBeat plugin's main view for the specified game.
        /// </summary>
        /// <param name="game">The game to show HowLongToBeat data for.</param>
        public static void HowLongToBeatView(Game game)
        {
            if (game == null || Plugin == null)
            {
                return;
            }

            try
            {
                // Retrieve game-specific menu items from the plugin
                IEnumerable<GameMenuItem> pluginMenus = Plugin.GetGameMenuItems(new GetGameMenuItemsArgs
                {
                    Games = new List<Game> { game },
                    IsGlobalSearchRequest = false
                });

                // Invoke the first available plugin menu item
                var firstMenu = pluginMenus.FirstOrDefault();
                if (firstMenu != null && firstMenu.Action != null)
                {
                    firstMenu.Action.Invoke(null);
                }
            }
            catch (Exception ex)
            {
                // Log the error without interrupting the UI
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
    }
}