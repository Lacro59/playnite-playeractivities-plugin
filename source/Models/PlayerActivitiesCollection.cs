using CommonPluginsShared.Collections;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerActivities.Models
{
    public class PlayerActivitiesCollection : PluginItemCollection<PlayerActivitiesData>
    {
        public PlayerActivitiesCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }
    }
}
