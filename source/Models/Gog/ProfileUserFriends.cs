using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models.Gog
{
    public class ProfileUserFriends
    {
        public string id { get; set; }
        public User user { get; set; }
        public int status { get; set; }
        public DateCreated date_created { get; set; }
        public DateAccepted date_accepted { get; set; }
        public Stats stats { get; set; }
    }

    public class User
    {
        public string id { get; set; }
        public string username { get; set; }
        public DateTime created_date { get; set; }
        public string avatar { get; set; }
        public bool is_employee { get; set; }
        public List<object> tags { get; set; }
    }

    public class DateCreated
    {
        public string date { get; set; }
        public int timezone_type { get; set; }
        public string timezone { get; set; }
    }

    public class DateAccepted
    {
        public string date { get; set; }
        public int timezone_type { get; set; }
        public string timezone { get; set; }
    }
}
