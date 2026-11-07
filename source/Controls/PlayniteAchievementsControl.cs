using CommonPlayniteShared;
using CommonPluginsShared;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Controls
{
    /// <summary>
    /// Utility class to detect the PlayniteAchievements plugin, resolve its cache path, and open its view.
    /// </summary>
    public static class PlayniteAchievementsPlugin
    {
        /// <summary>
        /// File name of the PlayniteAchievements SQLite cache under ExtensionsData.
        /// </summary>
        public const string AchievementCacheFileName = "achievement_cache.db";

        /// <summary>
        /// Localization key for the single-game "View achievements" menu item (not <c>FirstOrDefault</c>).
        /// </summary>
        public const string ViewAchievementsMenuLocKey = "LOCPlayAch_Menu_ViewAchievements";

        private static readonly Plugin CachedPlugin = API.Instance?.Addons?.Plugins?
            .FirstOrDefault(p => p.Id == GetPluginId(ExternalPlugin.PlayniteAchievements));

        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        /// <summary>
        /// Indicates whether the PlayniteAchievements plugin is installed.
        /// </summary>
        public static bool IsInstalled => CachedPlugin != null;

        /// <summary>
        /// Gets the absolute path to <c>achievement_cache.db</c> under the plugin's ExtensionsData folder.
        /// </summary>
        /// <returns>Full path; the file may be missing if the plugin has never written a cache.</returns>
        public static string GetAchievementCacheDatabasePath()
        {
            return Path.Combine(
                PlaynitePaths.ExtensionsDataPath,
                GetPluginId(ExternalPlugin.PlayniteAchievements).ToString(),
                AchievementCacheFileName);
        }

        /// <summary>
        /// Gets the cached Playnite SDK plugin instance when installed; otherwise <c>null</c>.
        /// </summary>
        internal static Plugin CachedInstance => CachedPlugin;

        /// <summary>
        /// Opens the PlayniteAchievements view for the specified game via the View Achievements menu action.
        /// </summary>
        /// <param name="game">Target game.</param>
        public static void ViewAchievements(Game game)
        {
            if (game == null)
            {
                return;
            }

            if (CachedPlugin == null)
            {
                Common.LogDebug($"[PlayniteAchievementsPlugin] ViewAchievements skipped installed=false gameId={game.Id}");
                return;
            }

            try
            {
                IEnumerable<GameMenuItem> pluginMenus = CachedPlugin.GetGameMenuItems(new GetGameMenuItemsArgs
                {
                    Games = new List<Game> { game },
                    IsGlobalSearchRequest = false
                });

                string viewDescription = ResourceProvider.GetString(ViewAchievementsMenuLocKey);
                GameMenuItem viewItem = pluginMenus?.FirstOrDefault(x =>
                    x != null
                    && !string.IsNullOrEmpty(x.Description)
                    && (string.Equals(x.Description, viewDescription, StringComparison.Ordinal)
                        || string.Equals(x.Description, ViewAchievementsMenuLocKey, StringComparison.Ordinal)));

                if (viewItem?.Action == null)
                {
                    Common.LogDebug(
                        $"[PlayniteAchievementsPlugin] ViewAchievements menu item missing gameId={game.Id} desc={viewDescription}");
                    return;
                }

                viewItem.Action.Invoke(null);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase?.PluginName ?? "PlayerActivities");
            }
        }
    }
}
