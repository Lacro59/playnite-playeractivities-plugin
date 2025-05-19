namespace PlayerActivities.Models
{
    /// <summary>
    /// Represents statistical data related to a player or friend.
    /// Stores information about owned games, completed games, achievements, and total playtime.
    /// </summary>
    public class PlayerStats
    {
        /// <summary>
        /// Total number of games owned by the player.
        /// </summary>
        public int GamesOwned { get; set; }

        /// <summary>
        /// Number of games completed by the player.
        /// </summary>
        public int GamesCompleted { get; set; }

        /// <summary>
        /// Total number of achievements earned by the player.
        /// </summary>
        public int Achievements { get; set; }

        /// <summary>
        /// Total playtime across all games, typically measured in minutes.
        /// </summary>
        public int Playtime { get; set; }
    }
}