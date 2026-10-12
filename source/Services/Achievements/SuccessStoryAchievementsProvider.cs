using CommonPlayniteShared;
using CommonPluginsShared;
using PlayerActivities.Controls;
using PlayerActivities.Models.SuccessStory;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static CommonPluginsShared.PlayniteTools;
using SsAchievement = PlayerActivities.Models.SuccessStory.Achievements;

namespace PlayerActivities.Services.Achievements
{
    /// <summary>
    /// Reads SuccessStory per-game JSON under ExtensionsData.
    /// </summary>
    public sealed class SuccessStoryAchievementsProvider : IPaAchievementsProvider
    {
        private const string PluginName = "PlayerActivities";

        /// <inheritdoc />
        public AchievementsProviderKind Kind => AchievementsProviderKind.SuccessStory;

        /// <inheritdoc />
        public bool IsAvailable => SuccessStoryPlugin.IsInstalled;

        private static string SuccessStoryPath => Path.Combine(
            PlaynitePaths.ExtensionsDataPath,
            GetPluginId(ExternalPlugin.SuccessStory).ToString(),
            "SuccessStory");

        /// <inheritdoc />
        public PaGameAchievementsData GetGameAchievements(Guid gameId)
        {
            var result = new PaGameAchievementsData { GameId = gameId };
            if (gameId == Guid.Empty)
            {
                return result;
            }

            string pathData = Path.Combine(SuccessStoryPath, gameId + ".json");
            if (!File.Exists(pathData))
            {
                return result;
            }

            try
            {
                bool ok = Serialization.TryFromJsonFile(pathData, out GameAchievements obj, out Exception ex);
                if (!ok || ex != null)
                {
                    Common.LogDebug($"[SuccessStoryAchievementsProvider] parseFailed gameId={gameId} msg={ex?.Message}");
                    return result;
                }

                if (obj?.Items == null || obj.Items.Count == 0)
                {
                    return result;
                }

                foreach (SsAchievement item in obj.Items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    DateTime? unlockUtc = null;
                    if (item.DateWhenUnlocked.HasValue)
                    {
                        unlockUtc = item.DateWhenUnlocked.Value.ToUniversalTime();
                    }

                    result.Items.Add(new PaAchievementItem
                    {
                        DisplayName = item.Name ?? string.Empty,
                        Description = item.Description ?? string.Empty,
                        UnlockedIconPath = item.UrlUnlocked ?? string.Empty,
                        UnlockTimeUtc = unlockUtc,
                        Unlocked = item.IsUnlock
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"[SuccessStoryAchievementsProvider] loadFailed gameId={gameId}", false, PluginName);
            }

            return result;
        }

        /// <inheritdoc />
        public IEnumerable<Guid> EnumerateGameIds()
        {
            if (!Directory.Exists(SuccessStoryPath))
            {
                return Enumerable.Empty<Guid>();
            }

            var ids = new List<Guid>();
            foreach (string objectFile in Directory.EnumerateFiles(SuccessStoryPath, "*.json"))
            {
                if (Guid.TryParse(Path.GetFileNameWithoutExtension(objectFile), out Guid fileId))
                {
                    ids.Add(fileId);
                }
            }

            return ids;
        }
    }
}
