using System;

namespace PlayerActivities.Models
{
    /// <summary>
    /// Represents a game associated with a player or a friend.
    /// Stores basic information about the game, such as its identifier, name, link, achievements, and playtime.
    /// </summary>
    public class PlayerGame
    {
        /// <summary>
        /// Unique identifier of the game on the platform or client.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the game.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// URL to the game's page or profile on the platform.
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// Indicates whether the game is common/shared between the user and the friend.
        /// </summary>
        public bool IsCommun { get; set; }

        /// <summary>
        /// Number of achievements earned in the game.
        /// </summary>
        public int Achievements { get; set; }

        /// <summary>
        /// Whether <see cref="Achievements"/> was loaded from the store API (deferred enrichment).
        /// </summary>
        public bool AchievementsLoaded { get; set; }

        /// <summary>
        /// UTC timestamp of the last successful achievements fetch for this game (12h cache).
        /// </summary>
        public DateTime? AchievementsFetchedAt { get; set; }

        /// <summary>
        /// Total playtime in the game, in seconds (Playnite convention).
        /// </summary>
        public int Playtime { get; set; }
    }
}
