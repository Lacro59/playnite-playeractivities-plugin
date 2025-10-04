using System;
using System.Collections.Generic;

namespace PlayerActivities.Models
{
    /// <summary>
    /// Represents the loading state of friend-related data from a specific game source.
    /// </summary>
    public class FriendsDataLoading : ObservableObject
    {
        private string _sourceName = string.Empty;
        private string _friendName = string.Empty; 
        private int _actualCount;
        private int _friendCount;

        /// <summary>
        /// Gets or sets the name of the source (e.g., Steam, GOG).
        /// </summary>
        public string SourceName
        {
            get => _sourceName;
            set => SetValue(ref _sourceName, value);
        }

        /// <summary>
        /// Gets or sets the name of the friend being processed.
        /// </summary>
        public string FriendName
        {
            get => _friendName;
            set => SetValue(ref _friendName, value);
        }

        /// <summary>
        /// Gets or sets the current number of processed friends.
        /// </summary>
        public int ActualCount
        {
            get => _actualCount;
            set => SetValue(ref _actualCount, value);
        }

        /// <summary>
        /// Gets or sets the total number of friends to process.
        /// </summary>
        public int FriendCount
        {
            get => _friendCount;
            set => SetValue(ref _friendCount, value);
        }
    }
}