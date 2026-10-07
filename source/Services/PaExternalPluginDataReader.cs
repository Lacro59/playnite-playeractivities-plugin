using CommonPlayniteShared;
using CommonPluginsShared;
using PlayerActivities.Models;
using PlayerActivities.Models.ScreenshotsVisualizer;
using PlayerActivities.Models.SuccessStory;
using Playnite.SDK.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Services
{
    /// <summary>
    /// Reads SuccessStory / ScreenshotsVisualizer JSON under ExtensionsData on demand,
    /// caches display DTOs per game, and filters by activity day.
    /// </summary>
    public sealed class PaExternalPluginDataReader
    {
        private static readonly Lazy<PaExternalPluginDataReader> LazyInstance =
            new Lazy<PaExternalPluginDataReader>(() => new PaExternalPluginDataReader());

        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".webp", ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff", ".jfif", ".avif"
        };

        private readonly string _successStoryPath;
        private readonly string _screenshotsVisualizerPath;
        private readonly ConcurrentDictionary<Guid, List<PaAchievementDisplay>> _achievementsByGame =
            new ConcurrentDictionary<Guid, List<PaAchievementDisplay>>();
        private readonly ConcurrentDictionary<Guid, List<PaScreenshotDisplay>> _screenshotsByGame =
            new ConcurrentDictionary<Guid, List<PaScreenshotDisplay>>();
        private readonly ConcurrentDictionary<string, bool> _pathExistsCache =
            new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the shared reader instance (process-wide memory cache).
        /// </summary>
        public static PaExternalPluginDataReader Instance => LazyInstance.Value;

        private PaExternalPluginDataReader()
        {
            _successStoryPath = Path.Combine(
                PlaynitePaths.ExtensionsDataPath,
                GetPluginId(ExternalPlugin.SuccessStory).ToString(),
                "SuccessStory");
            _screenshotsVisualizerPath = Path.Combine(
                PlaynitePaths.ExtensionsDataPath,
                GetPluginId(ExternalPlugin.ScreenshotsVisualizer).ToString(),
                "ScreenshotsVisualizer");
        }

        /// <summary>
        /// Returns unlocked achievements for <paramref name="gameId"/> whose unlock date matches <paramref name="day"/>.
        /// </summary>
        /// <param name="gameId">Playnite game id.</param>
        /// <param name="day">Activity day (date portion used).</param>
        /// <returns>Filtered display rows; empty when JSON is missing or has no match.</returns>
        public IReadOnlyList<PaAchievementDisplay> GetUnlockedForDay(Guid gameId, DateTime day)
        {
            if (gameId == Guid.Empty)
            {
                return Array.Empty<PaAchievementDisplay>();
            }

            DateTime dayDate = day.Date;
            List<PaAchievementDisplay> all = GetOrLoadAchievements(gameId);
            return all.Where(x => x.DateUnlocked.Date == dayDate).ToList();
        }

        /// <summary>
        /// Returns up to <paramref name="maxCount"/> image screenshots for the day that still exist on disk.
        /// Existence is probed lazily (not for the whole game JSON) to avoid UI freezes on cloud paths.
        /// </summary>
        /// <param name="gameId">Playnite game id.</param>
        /// <param name="day">Activity day (date portion used).</param>
        /// <param name="maxCount">Maximum existing files to return.</param>
        /// <returns>Filtered display rows; empty when JSON is missing or has no match.</returns>
        public IReadOnlyList<PaScreenshotDisplay> GetExistingScreenshotsForDay(Guid gameId, DateTime day, int maxCount)
        {
            if (gameId == Guid.Empty || maxCount <= 0)
            {
                return Array.Empty<PaScreenshotDisplay>();
            }

            DateTime dayDate = day.Date;
            List<PaScreenshotDisplay> dayItems = GetOrLoadScreenshots(gameId)
                .Where(x => MatchesActivityDay(x.DateTaken, dayDate))
                .ToList();

            List<PaScreenshotDisplay> result = new List<PaScreenshotDisplay>(Math.Min(maxCount, dayItems.Count));
            int probed = 0;
            int skippedMissing = 0;
            // Bound Exists probes (cloud paths are slow) even if many files are missing.
            int maxProbes = Math.Max(maxCount * 4, 32);

            foreach (PaScreenshotDisplay item in dayItems)
            {
                if (result.Count >= maxCount || probed >= maxProbes)
                {
                    break;
                }

                probed++;
                if (!PathExistsCached(item.FilePath))
                {
                    skippedMissing++;
                    continue;
                }

                result.Add(item);
            }

            if (skippedMissing > 0 || probed > maxCount)
            {
                Common.LogDebug(
                    $"[PaExternalPluginDataReader] screenshots dayProbe gameId={gameId} day={dayDate:yyyy-MM-dd} " +
                    $"kept={result.Count} skippedMissing={skippedMissing} probed={probed} dayTotal={dayItems.Count}");
            }

            return result;
        }

        /// <summary>
        /// Clears in-memory caches (e.g. after external plugin data refresh).
        /// </summary>
        public void ClearCache()
        {
            _achievementsByGame.Clear();
            _screenshotsByGame.Clear();
            _pathExistsCache.Clear();
            Common.LogDebug("[PaExternalPluginDataReader] cache cleared");
        }

        private List<PaAchievementDisplay> GetOrLoadAchievements(Guid gameId)
        {
            if (_achievementsByGame.TryGetValue(gameId, out List<PaAchievementDisplay> cached))
            {
                return cached;
            }

            List<PaAchievementDisplay> loaded = LoadAchievements(gameId);
            if (_achievementsByGame.TryAdd(gameId, loaded))
            {
                Common.LogDebug($"[PaExternalPluginDataReader] achievements cacheMiss gameId={gameId} count={loaded.Count}");
                return loaded;
            }

            return _achievementsByGame[gameId];
        }

        private List<PaScreenshotDisplay> GetOrLoadScreenshots(Guid gameId)
        {
            if (_screenshotsByGame.TryGetValue(gameId, out List<PaScreenshotDisplay> cached))
            {
                return cached;
            }

            List<PaScreenshotDisplay> loaded = LoadScreenshots(gameId);
            if (_screenshotsByGame.TryAdd(gameId, loaded))
            {
                Common.LogDebug($"[PaExternalPluginDataReader] screenshots cacheMiss gameId={gameId} count={loaded.Count}");
                return loaded;
            }

            return _screenshotsByGame[gameId];
        }

        private List<PaAchievementDisplay> LoadAchievements(Guid gameId)
        {
            string pathData = Path.Combine(_successStoryPath, gameId + ".json");
            if (!File.Exists(pathData))
            {
                Common.LogDebug($"[PaExternalPluginDataReader] achievements missing path={pathData}");
                return new List<PaAchievementDisplay>();
            }

            try
            {
                bool ok = Serialization.TryFromJsonFile(pathData, out GameAchievements obj, out Exception ex);
                if (!ok || ex != null)
                {
                    Common.LogDebug($"[PaExternalPluginDataReader] achievements parseFailed gameId={gameId} msg={ex?.Message}");
                    return new List<PaAchievementDisplay>();
                }

                if (obj?.Items == null || obj.Items.Count == 0)
                {
                    return new List<PaAchievementDisplay>();
                }

                return obj.Items
                    .Where(x => x != null && x.IsUnlock && x.DateWhenUnlocked.HasValue)
                    .Select(x => new PaAchievementDisplay
                    {
                        Name = x.Name ?? string.Empty,
                        IconUrl = x.UrlUnlocked ?? string.Empty,
                        DateUnlocked = x.DateWhenUnlocked.Value
                    })
                    .OrderBy(x => x.DateUnlocked)
                    .ToList();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"[PaExternalPluginDataReader] achievements loadFailed gameId={gameId}", false, "PlayerActivities");
                return new List<PaAchievementDisplay>();
            }
        }

        private List<PaScreenshotDisplay> LoadScreenshots(Guid gameId)
        {
            string pathData = Path.Combine(_screenshotsVisualizerPath, gameId + ".json");
            if (!File.Exists(pathData))
            {
                Common.LogDebug($"[PaExternalPluginDataReader] screenshots missing path={pathData}");
                return new List<PaScreenshotDisplay>();
            }

            try
            {
                bool ok = Serialization.TryFromJsonFile(pathData, out GameScreenshots obj, out Exception ex);
                if (!ok || ex != null)
                {
                    Common.LogDebug($"[PaExternalPluginDataReader] screenshots parseFailed gameId={gameId} msg={ex?.Message}");
                    return new List<PaScreenshotDisplay>();
                }

                if (obj?.Items == null || obj.Items.Count == 0)
                {
                    return new List<PaScreenshotDisplay>();
                }

                // Do not File.Exists here: cloud paths × thousands freeze the UI. Probe lazily in GetExistingScreenshotsForDay.
                return obj.Items
                    .Where(x => x != null && IsImagePath(x.FileName))
                    .Select(x => new PaScreenshotDisplay
                    {
                        FilePath = x.FileName,
                        DateTaken = x.Modifed
                    })
                    .OrderBy(x => x.DateTaken)
                    .ToList();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"[PaExternalPluginDataReader] screenshots loadFailed gameId={gameId}", false, "PlayerActivities");
                return new List<PaScreenshotDisplay>();
            }
        }

        private bool PathExistsCached(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return _pathExistsCache.GetOrAdd(path, p =>
            {
                try
                {
                    return File.Exists(p);
                }
                catch (IOException)
                {
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            });
        }

        private static bool MatchesActivityDay(DateTime taken, DateTime activityDay)
        {
            if (taken.Date == activityDay)
            {
                return true;
            }

            if (taken.Kind == DateTimeKind.Utc && taken.ToLocalTime().Date == activityDay)
            {
                return true;
            }

            return false;
        }

        private static bool IsImagePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            string ext = Path.GetExtension(filePath);
            return !string.IsNullOrEmpty(ext) && ImageExtensions.Contains(ext);
        }
    }
}
