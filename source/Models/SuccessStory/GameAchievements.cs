using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models.SuccessStory
{
    public class GameAchievements
    {
        public List<Achievements> Items { get; set; }


        [DontSerialize]
        public int Total
        {
            get
            {
                return Items.Count();
            }
        }

        [DontSerialize]
        public int Unlocked
        {
            get
            {
                return Items.FindAll(x => x.IsUnlock).Count;
            }
        }

        [DontSerialize]
        public int Progression
        {
            get
            {
                return (Total != 0) ? (int)Math.Ceiling((double)(Unlocked * 100 / Total)) : 0;
            }
        }
    }
}
