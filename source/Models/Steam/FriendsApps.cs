using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models.Steam
{
    public class FriendsApps
    {
        public int appid { get; set; }
        public string name { get; set; }
        public int app_type { get; set; }
        public string logo { get; set; }
        public object friendlyURL { get; set; }
        public AvailStatLinks availStatLinks { get; set; }
        public string hours_forever { get; set; }
        public int last_played { get; set; }
        public int? has_adult_content { get; set; }
        public string friendly_name { get; set; }
        public string hours { get; set; }
        public int? is_visible_in_steam_china { get; set; }
    }

    public class AvailStatLinks
    {
        public bool achievements { get; set; }
        public bool global_achievements { get; set; }
        public bool stats { get; set; }
        public bool gcpd { get; set; }
        public bool leaderboards { get; set; }
        public bool global_leaderboards { get; set; }
    }

}
