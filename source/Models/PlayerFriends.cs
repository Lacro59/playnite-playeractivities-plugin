using CommonPluginsShared;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models
{
    public class PlayerFriends
    {
        public string ClientName { get; set; }
        public string FriendId { get; set; }
        public string FriendPseudo { get; set; }
        public string FriendsAvatar { get; set; }
        public string FriendsLink { get; set; }


        [DontSerialize]
        public string ClientIcon => TransformIcon.Get(ClientName);

        public DateTime? AcceptedAt { get; set; }

        public PlayerStats Stats {get;set;}
    }


    public class PlayerStats
    {
        public int GamesOwned { get; set; }
        public int GamesCompleted { get; set; }
        public int Achievements { get; set; }
        public double HoursPlayed { get; set; }
    }
}
