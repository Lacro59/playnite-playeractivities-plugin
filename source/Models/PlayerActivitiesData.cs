using CommonPluginsShared.Collections;
using PlayerActivities.Models.Enumerations;
using System.Linq;

namespace PlayerActivities.Models
{
    public class PlayerActivitiesData : PluginDataBaseGame<Activity>
    {
        /// <summary>
        /// Determines whether there are any items of type `ActivityType.PlaytimeFirst` in the collection.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if there are any items of type `ActivityType.PlaytimeFirst`; otherwise, <c>false</c>.
        /// </returns>
        public bool HasFirst()
        {
            return !Items.Any(x => x.Type == ActivityType.PlaytimeFirst);
        }
    }
}