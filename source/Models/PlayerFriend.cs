using CommonPlayniteShared.Commands;
using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;

namespace PlayerActivities.Models
{
    /// <summary>
    /// Represents a friend of the player from a specific game client/platform.
    /// Stores information about the friend, their games, statistics, and related metadata.
    /// </summary>
    public class PlayerFriend : ObservableObject
    {
        /// <summary>
        /// Name of the client/platform (e.g., Steam, GOG, Epic) where the friend is registered.
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Unique identifier of the friend on the client/platform.
        /// </summary>
        public string FriendId { get; set; }

        /// <summary>
        /// Unique identifier of the friend on the client/platform.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Display name or pseudonym of the friend.
        /// </summary>
        public string FriendPseudo { get; set; }

        /// <summary>
        /// URL or path to the friend's avatar image.
        /// </summary>
        public string FriendsAvatar { get; set; }

        /// <summary>
        /// URL to the friend's profile or related page.
        /// </summary>
        public string FriendsLink { get; set; }

        /// <summary>
        /// Indicates if this friend entry represents the current user.
        /// </summary>
        public bool IsUser { get; set; }

        /// <summary>
        /// When true, the friend's game library (or profile) is private/hidden — Games stay empty and the UI shows a private marker.
        /// </summary>
        public bool IsPrivate { get; set; }

        private bool _isUpdating;

        /// <summary>
        /// When true, deferred friend data (e.g. achievement enrich) is running — UI shows an updating marker.
        /// Not serialized.
        /// </summary>
        [DontSerialize]
        public bool IsUpdating
        {
            get => _isUpdating;
            set => SetValue(ref _isUpdating, value);
        }

        /// <summary>
        /// Gets the icon associated with the client/platform.
        /// Not serialized.
        /// </summary>
        [DontSerialize]
        public string ClientIcon => TransformIcon.Get(ClientName);

        /// <summary>
        /// Date and time when the friendship was accepted, if available.
        /// </summary>
        public DateTime? AcceptedAt { get; set; }

        private PlayerStats _stats;

        /// <summary>
        /// Statistics related to the friend (e.g., games owned, achievements, playtime).
        /// </summary>
        public PlayerStats Stats
        {
            get => _stats;
            set => SetValue(ref _stats, value);
        }

        /// <summary>
        /// List of games associated with the friend.
        /// </summary>
        public List<PlayerGame> Games { get; set; } = new List<PlayerGame>();

        /// <summary>
        /// Command to navigate to a specified URL (typically the friend's profile).
        /// Not serialized.
        /// </summary>
        [DontSerialize]
        public RelayCommand<object> NavigateUrl { get; } = new RelayCommand<object>((url) => GlobalCommands.NavigateUrl(url));

        /// <summary>
        /// The last time the friend's data was refreshed.
        /// </summary>
        public DateTime LastUpdate { get; set; } = DateTime.Now;
    }
}
