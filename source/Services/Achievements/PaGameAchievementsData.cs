using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerActivities.Services.Achievements
{
    /// <summary>
    /// Snapshot of achievements for one Playnite game (import + bandeau).
    /// </summary>
    public sealed class PaGameAchievementsData
    {
        /// <summary>
        /// Gets or sets the Playnite game id.
        /// </summary>
        public Guid GameId { get; set; }

        /// <summary>
        /// Gets or sets all achievement rows (locked and unlocked).
        /// </summary>
        public List<PaAchievementItem> Items { get; set; } = new List<PaAchievementItem>();

        /// <summary>
        /// Gets the total achievement count.
        /// </summary>
        public int Total => Items?.Count ?? 0;

        /// <summary>
        /// Gets the unlocked achievement count.
        /// </summary>
        public int Unlocked => Items?.Count(x => x != null && x.Unlocked) ?? 0;

        /// <summary>
        /// Gets the progression percentage (ceiling), or 0 when empty.
        /// </summary>
        public int Progression => Total == 0
            ? 0
            : (int)Math.Ceiling((double)(Unlocked * 100) / Total);
    }
}
