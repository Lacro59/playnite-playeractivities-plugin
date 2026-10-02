using CommonPluginsShared.Collections;
using PlayerActivities.Models.Enumerations;
using System.Linq;

namespace PlayerActivities.Models
{
    /// <summary>
    /// Per-game activity entry stored by PlayerActivities (typed item list).
    /// </summary>
    public class PlayerActivitiesData : PluginGameCollection<Activity>
    {
        /// <summary>
        /// Determines whether there are any items of type <see cref="ActivityType.PlaytimeFirst"/> in the collection.
        /// </summary>
        /// <returns>
        /// <c>true</c> if there are any items of type <see cref="ActivityType.PlaytimeFirst"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool HasFirst()
        {
            return Items.Any(x => x.Type == ActivityType.PlaytimeFirst);
        }
    }
}
