using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models.SuccessStory
{
    public class Achievements
    {
        public DateTime? DateUnlocked { get; set; }

        [DontSerialize]
        public bool IsUnlock
        {
            get
            {
                return !(DateUnlocked == default(DateTime) || DateUnlocked == null);
            }
        }
    }
}
