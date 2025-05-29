using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayerActivities.Models.SuccessStory
{
    public class GameAchievements
    {
        public List<Achievements> Items { get; set; } = new List<Achievements>();

        [DontSerialize]
        public int Total => Items.Count();

        [DontSerialize]
        public int Unlocked => Items.Count(x => x.IsUnlock);

        [DontSerialize]
        public int Progression => (int)Math.Ceiling((double)(Unlocked * 100) / Total);
    }
}