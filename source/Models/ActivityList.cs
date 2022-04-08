using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models
{
    public class ActivityList : Activity
    {
        public Game GameContext { get; set; }
    }


    public class ActivityListGrouped
    {
        public Game GameContext { get; set; }

        public string dtString { get; set; }
        public string TimeAgo { get; set; }

        public string SourceIcon => TransformIcon.Get(PlayniteTools.GetSourceName(GameContext.Id));

        public string CoverImage => API.Instance.Database.GetFullFilePath(GameContext.CoverImage);

        public List<Activity> Activities { get; set; } = new List<Activity>();

        public List<Activity> ActivitiesOrdered => Activities.OrderByDescending(x => x.DateActivity.ToString("yyyy-MM-dd")).ThenBy(x => x.Type).ToList();
    }
}
