using System;

namespace PlayerActivities.Services.Achievements
{
    /// <summary>
    /// Normalized achievement row aligned on PlayniteAchievements field names
    /// (<c>DisplayName</c>, <c>UnlockTimeUtc</c>, <c>UnlockedIconPath</c>).
    /// </summary>
    public sealed class PaAchievementItem
    {
        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description when present.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the unlocked icon as an absolute path or http(s) URL.
        /// </summary>
        public string UnlockedIconPath { get; set; }

        /// <summary>
        /// Gets or sets the unlock timestamp in UTC, or null when locked / unknown.
        /// </summary>
        public DateTime? UnlockTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets whether the achievement is unlocked.
        /// </summary>
        public bool Unlocked { get; set; }

        /// <summary>
        /// Gets the local unlock date/time when <see cref="UnlockTimeUtc"/> is set.
        /// </summary>
        public DateTime? UnlockTimeLocal => UnlockTimeUtc?.ToLocalTime();
    }
}
