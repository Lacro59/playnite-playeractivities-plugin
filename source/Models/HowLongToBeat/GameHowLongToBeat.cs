using System.Collections.Generic;
using System.Linq;

namespace PlayerActivities.Models.HowLongToBeat
{
    public class GameHowLongToBeat
    {
        public List<HltbDataUser> Items { get; set; } = new List<HltbDataUser>();

        public HltbDataUser GetData()
        {
            if (Items?.Count == 0)
            {
                return null;
            }
            return Items.First();
        }
    }
}