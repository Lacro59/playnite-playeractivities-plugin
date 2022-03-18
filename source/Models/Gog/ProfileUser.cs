using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models.Gog
{
    public class ProfileUser
    {
        public string username { get; set; }
        public DateTime created_date { get; set; }
        public string userId { get; set; }
        public string avatar { get; set; }
        public Settings settings { get; set; }
        public Stats stats { get; set; }
        public Background background { get; set; }
    }

    public class Settings
    {
        public string allow_to_be_invited_by { get; set; }
    }

    public class Stats
    {
        public int games_owned { get; set; }
        public int achievements { get; set; }
        public int hours_played { get; set; }
    }

    public class Background
    {
        public string id { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string src { get; set; }
        public List<int> background_dominant_color { get; set; }
    }
}
