using System;
using System.Collections.Generic;

namespace PlayerActivities.Models
{
    /// <summary>
    /// Represents a collection of the player's friends and the last time the data was updated.
    /// </summary>
    public class FriendsData
    {
        private DateTime _lastUpdate = DateTime.UtcNow;

        /// <summary>
        /// List of friends associated with the player.
        /// </summary>
        public List<PlayerFriend> PlayerFriends { get; set; } = new List<PlayerFriend>();

        /// <summary>
        /// Gets or sets the date and time when the friends data was last updated.
        /// The value is stored in UTC and returned in local time.
        /// </summary>
        public DateTime LastUpdate
        {
            get => _lastUpdate.ToLocalTime();
            set => _lastUpdate = value.ToUniversalTime();
        }
    }
}