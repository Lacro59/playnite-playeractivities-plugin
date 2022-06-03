using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models
{
    public class FriendsDataLoading : ObservableObject
    {
        private string _SourceName = string.Empty;
        public string SourceName { get => _SourceName; set => SetValue(ref _SourceName, value); }
    
        private string _FriendName = string.Empty;
        public string FriendName { get => _FriendName; set => SetValue(ref _FriendName, value); }

        private int _ActualCount = 0;
        public int ActualCount { get => _ActualCount; set => SetValue(ref _ActualCount, value); }

        private int _FriendCount = 0;
        public int FriendCount { get => _FriendCount; set => SetValue(ref _FriendCount, value); }
    }
}
