using CommonPluginsShared;
using PlayerActivities.Controls;
using Playnite.SDK.Models;

namespace PlayerActivities.Services.Achievements
{
    /// <summary>
    /// Opens the achievements UI for the provider selected in settings (graceful no-op when unavailable).
    /// </summary>
    public static class PaAchievementsView
    {
        /// <summary>
        /// Gets whether the selected achievements provider plugin is installed.
        /// </summary>
        public static bool IsSelectedProviderInstalled
        {
            get
            {
                PlayerActivitiesSettings settings = PlayerActivities.PluginDatabase?.PluginSettings;
                if (settings == null || !settings.EnableSuccessStoryData)
                {
                    return false;
                }

                switch (settings.SelectedAchievementsProvider)
                {
                    case AchievementsProviderKind.PlayniteAchievements:
                        return PlayniteAchievementsPlugin.IsInstalled;
                    case AchievementsProviderKind.SuccessStory:
                    default:
                        return SuccessStoryPlugin.IsInstalled;
                }
            }
        }

        /// <summary>
        /// Opens the achievements view for <paramref name="game"/> using the selected provider.
        /// </summary>
        /// <param name="game">Target game.</param>
        public static void Open(Game game)
        {
            if (game == null)
            {
                return;
            }

            PlayerActivitiesSettings settings = PlayerActivities.PluginDatabase?.PluginSettings;
            if (settings == null || !settings.EnableSuccessStoryData)
            {
                Common.LogDebug($"[PaAchievementsView] open skipped enabled=false gameId={game.Id}");
                return;
            }

            AchievementsProviderKind kind = settings.SelectedAchievementsProvider;
            switch (kind)
            {
                case AchievementsProviderKind.PlayniteAchievements:
                    if (!PlayniteAchievementsPlugin.IsInstalled)
                    {
                        Common.LogDebug($"[PaAchievementsView] open skipped kind={kind} installed=false gameId={game.Id}");
                        return;
                    }

                    PlayniteAchievementsPlugin.ViewAchievements(game);
                    break;
                case AchievementsProviderKind.SuccessStory:
                default:
                    if (!SuccessStoryPlugin.IsInstalled)
                    {
                        Common.LogDebug($"[PaAchievementsView] open skipped kind={kind} installed=false gameId={game.Id}");
                        return;
                    }

                    SuccessStoryPlugin.SuccessStoryView(game);
                    break;
            }
        }
    }
}
