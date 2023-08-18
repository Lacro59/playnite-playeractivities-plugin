using CommonPluginsShared;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Controls;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PlayerActivities.Controls
{
    public class HowLongToBeatPlugin
    {
        private static PlayerActivitiesDatabase PluginDatabase = PlayerActivities.PluginDatabase;

        private static Plugin _plugin;
        private static Plugin Plugin
        {
            get
            {
                if (_plugin == null)
                {
                    _plugin = API.Instance?.Addons?.Plugins?.FirstOrDefault(p => p.Id == Guid.Parse("e08cd51f-9c9a-4ee3-a094-fde03b55492f")) ?? null;
                }
                return _plugin;
            }
        }

        public static bool IsInstalled => Plugin != null;

        public static void HowLongToBeatView(Game game)
        {
            if (game == null || Plugin == null)
            {
                return;
            }

            try
            {
                IEnumerable<GameMenuItem> pluginMenus = Plugin.GetGameMenuItems(new GetGameMenuItemsArgs { Games = new List<Game> { game }, IsGlobalSearchRequest = false });
                if (pluginMenus.Count() > 0)
                {
                    pluginMenus.First().Action.Invoke(null);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
    }
}
