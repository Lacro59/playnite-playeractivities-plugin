using CommonPluginsShared;
using PlayerActivities.Controls;
using SqlNado;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PlayerActivities.Services.Achievements
{
    /// <summary>
    /// Reads PlayniteAchievements <c>achievement_cache.db</c> via SqlNado (schema = PlayniteAchievements).
    /// </summary>
    public sealed class PlayniteAchievementsDataProvider : IPaAchievementsProvider
    {
        private const string PluginName = "PlayerActivities";

        private const string SqlLatestProgress =
@"SELECT ugp.Id AS Id, ugp.GameId AS GameId, ugp.TotalAchievements AS TotalAchievements
FROM UserGameProgress ugp
INNER JOIN Users u ON u.Id = ugp.UserId AND u.IsCurrentUser = 1
INNER JOIN Games g ON g.Id = ugp.GameId
WHERE g.PlayniteGameId = ?
ORDER BY ugp.LastUpdatedUtc DESC, ugp.Id DESC
LIMIT 1";

        private const string SqlDefinitionsForProgress =
@"SELECT
  ad.DisplayName AS DisplayName,
  ad.Description AS Description,
  ad.UnlockedIconPath AS UnlockedIconPath,
  ua.Unlocked AS Unlocked,
  ua.UnlockTimeUtc AS UnlockTimeUtc
FROM AchievementDefinitions ad
LEFT JOIN UserAchievements ua
  ON ua.AchievementDefinitionId = ad.Id AND ua.UserGameProgressId = ?
WHERE ad.GameId = ?";

        private const string SqlEnumerateGameIds =
@"SELECT DISTINCT g.PlayniteGameId AS PlayniteGameId
FROM Games g
INNER JOIN UserGameProgress ugp ON ugp.GameId = g.Id
INNER JOIN Users u ON u.Id = ugp.UserId AND u.IsCurrentUser = 1
WHERE g.PlayniteGameId IS NOT NULL AND TRIM(g.PlayniteGameId) <> ''";

        /// <inheritdoc />
        public AchievementsProviderKind Kind => AchievementsProviderKind.PlayniteAchievements;

        /// <inheritdoc />
        public bool IsAvailable
        {
            get
            {
                if (!PlayniteAchievementsPlugin.IsInstalled)
                {
                    return false;
                }

                string path = PlayniteAchievementsPlugin.GetAchievementCacheDatabasePath();
                return File.Exists(path);
            }
        }

        /// <inheritdoc />
        public PaGameAchievementsData GetGameAchievements(Guid gameId)
        {
            var result = new PaGameAchievementsData { GameId = gameId };
            if (gameId == Guid.Empty || !IsAvailable)
            {
                return result;
            }

            string dbPath = PlayniteAchievementsPlugin.GetAchievementCacheDatabasePath();
            string iconBasePath = Path.GetDirectoryName(dbPath);

            try
            {
                using (var db = new SQLiteDatabase(dbPath, SQLiteOpenOptions.SQLITE_OPEN_READONLY))
                {
                    ProgressRow progress = db.Load<ProgressRow>(SqlLatestProgress, gameId.ToString())
                        .FirstOrDefault();
                    if (progress == null)
                    {
                        return result;
                    }

                    List<AchievementRow> rows = db.Load<AchievementRow>(
                            SqlDefinitionsForProgress,
                            progress.Id,
                            progress.GameId)
                        .ToList();

                    foreach (AchievementRow row in rows)
                    {
                        if (row == null)
                        {
                            continue;
                        }

                        DateTime? unlockUtc = ParseUnlockTimeUtc(row.UnlockTimeUtc);
                        bool unlocked = row.Unlocked != 0 || unlockUtc.HasValue;

                        result.Items.Add(new PaAchievementItem
                        {
                            DisplayName = row.DisplayName ?? string.Empty,
                            Description = row.Description ?? string.Empty,
                            UnlockedIconPath = ResolveIconPath(iconBasePath, row.UnlockedIconPath),
                            UnlockTimeUtc = unlockUtc,
                            Unlocked = unlocked
                        });
                    }

                    // Prefer catalog size; fall back to UserGameProgress total when definitions missing.
                    if (result.Items.Count == 0 && progress.TotalAchievements > 0)
                    {
                        Common.LogDebug(
                            $"[PlayniteAchievementsDataProvider] noDefinitions gameId={gameId} total={progress.TotalAchievements}");
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"[PlayniteAchievementsDataProvider] loadFailed gameId={gameId}", false, PluginName);
                result.Items = new List<PaAchievementItem>();
            }

            return result;
        }

        /// <inheritdoc />
        public IEnumerable<Guid> EnumerateGameIds()
        {
            if (!IsAvailable)
            {
                return Enumerable.Empty<Guid>();
            }

            string dbPath = PlayniteAchievementsPlugin.GetAchievementCacheDatabasePath();
            var ids = new List<Guid>();

            try
            {
                using (var db = new SQLiteDatabase(dbPath, SQLiteOpenOptions.SQLITE_OPEN_READONLY))
                {
                    foreach (GameIdRow row in db.Load<GameIdRow>(SqlEnumerateGameIds))
                    {
                        if (row != null && Guid.TryParse(row.PlayniteGameId, out Guid id))
                        {
                            ids.Add(id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "[PlayniteAchievementsDataProvider] enumerateFailed", false, PluginName);
            }

            return ids;
        }

        private static string ResolveIconPath(string iconBasePath, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string trimmed = path.Trim();
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || Path.IsPathRooted(trimmed))
            {
                return trimmed;
            }

            if (string.IsNullOrEmpty(iconBasePath))
            {
                return trimmed;
            }

            return Path.Combine(iconBasePath, trimmed);
        }

        private static DateTime? ParseUnlockTimeUtc(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            DateTime parsed;
            if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed))
            {
                if (parsed.Kind == DateTimeKind.Unspecified)
                {
                    return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                }

                return parsed.ToUniversalTime();
            }

            return null;
        }

        private sealed class ProgressRow
        {
            public long Id { get; set; }
            public long GameId { get; set; }
            public long TotalAchievements { get; set; }
        }

        private sealed class AchievementRow
        {
            public string DisplayName { get; set; }
            public string Description { get; set; }
            public string UnlockedIconPath { get; set; }
            public long Unlocked { get; set; }
            public string UnlockTimeUtc { get; set; }
        }

        private sealed class GameIdRow
        {
            public string PlayniteGameId { get; set; }
        }
    }
}
