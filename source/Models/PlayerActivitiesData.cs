using CommonPluginsShared.Collections;
using PlayerActivities.Models.Enumerations;
using System.Collections.Generic;
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

        /// <summary>
        /// Keeps a single <see cref="ActivityType.PlaytimeFirst"/> entry (oldest <see cref="Activity.DateActivity"/>) and removes duplicates.
        /// </summary>
        /// <returns>Number of duplicate entries removed.</returns>
        public int DeduplicatePlaytimeFirst()
        {
            List<Activity> firsts = Items.Where(x => x.Type == ActivityType.PlaytimeFirst).ToList();
            if (firsts.Count <= 1)
            {
                return 0;
            }

            Activity keep = firsts.OrderBy(x => x.DateActivity).First();
            int removed = 0;
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i].Type == ActivityType.PlaytimeFirst && !ReferenceEquals(Items[i], keep))
                {
                    Items.RemoveAt(i);
                    removed++;
                }
            }

            return removed;
        }
    }
}
