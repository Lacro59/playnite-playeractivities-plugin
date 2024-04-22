using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models
{
    public class FriendsDataLoading : ObservableObject
    {
        private string sourceName = string.Empty;
        public string SourceName { get => sourceName; set => SetValue(ref sourceName, value); }
    
        private string friendName = string.Empty;
        public string FriendName { get => friendName; set => SetValue(ref friendName, value); }

        private int actualCount = 0;
        public int ActualCount { get => actualCount; set => SetValue(ref actualCount, value); }

        private int friendCount = 0;
        public int FriendCount { get => friendCount; set => SetValue(ref friendCount, value); }
    }
}
