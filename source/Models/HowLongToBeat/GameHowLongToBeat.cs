using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models.HowLongToBeat
{
    public class GameHowLongToBeat
    {
        public List<HltbDataUser> Items { get; set; }

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
