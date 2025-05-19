using CommonPluginsShared.Collections;
using Playnite.SDK;

namespace PlayerActivities.Models
{
    public class PlayerActivitiesCollection : PluginItemCollection<PlayerActivitiesData>
    {
        public PlayerActivitiesCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }
    }
}
