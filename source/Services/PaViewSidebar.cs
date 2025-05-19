using CommonPluginsShared.Controls;
using PlayerActivities.Views;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlayerActivities.Services
{
    public class PaViewSidebar : SidebarItem
    {
        public PaViewSidebar(PlayerActivities plugin)
        {
            Type = SiderbarItemType.View;
            Title = ResourceProvider.GetString("LOCPa");
            Icon = new TextBlock
            {
                Text = "\ueea5",
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            };
            Opened = () =>
            {
                if (plugin.SidebarItemControl == null)
                {
                    plugin.SidebarItemControl = new SidebarItemControl();
                    plugin.SidebarItemControl.SetTitle(ResourceProvider.GetString("LOCPa"));
                    plugin.SidebarItemControl.AddContent(new PaView(plugin));
                }

                return plugin.SidebarItemControl;
            };
            Visible = plugin.PluginSettings.Settings.EnableIntegrationButtonSide;
        }
    }
}