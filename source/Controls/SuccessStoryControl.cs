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
    /// Utility class to interface with the SuccessStory plugin (menus / view open).
    /// </summary>
    public class SuccessStoryPlugin
    {
        private static readonly Plugin CachedPlugin = API.Instance?.Addons?.Plugins?.FirstOrDefault(p => p.Id == GetPluginId(ExternalPlugin.SuccessStory));

        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        /// <summary>
        /// Indicates whether the SuccessStory plugin is installed.
        /// </summary>
        public static bool IsInstalled => CachedPlugin != null;

        /// <summary>
        /// Opens the SuccessStory view for the specified game.
        /// </summary>
        /// <param name="game">Target game</param>
        public static void SuccessStoryView(Game game)
        {
            if (game == null || CachedPlugin == null)
            {
                return;
            }

            try
            {
                var pluginMenus = CachedPlugin.GetGameMenuItems(new GetGameMenuItemsArgs
                {
                    Games = new List<Game> { game },
                    IsGlobalSearchRequest = false
                });
                pluginMenus?.FirstOrDefault()?.Action?.Invoke(null);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
    }
}
