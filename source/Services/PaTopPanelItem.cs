using CommonPluginsShared;
using PlayerActivities.Views;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlayerActivities.Services
{
    public class PaTopPanelItem : TopPanelItem
    {
        public PaTopPanelItem(PlayerActivities plugin)
        {
            Icon = new TextBlock
            {
                Text = "\ueea5",
                FontSize = 20,
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            };
            Title = ResourceProvider.GetString("LOCPa");
            Activated = () =>
            {
                WindowOptions windowOptions = new WindowOptions
                {
                    ShowMinimizeButton = false,
                    ShowMaximizeButton = true,
                    ShowCloseButton = true,
                    CanBeResizable = true,
                    Width = 1280,
                    Height = 740
                };

                PaView ViewExtension = new PaView(plugin);
                Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(ResourceProvider.GetString("LOCPa"), ViewExtension, windowOptions);
                _ = windowExtension.ShowDialog();
            };
            Visible = plugin.PluginSettings.Settings.EnableIntegrationButtonHeader;
        }
    }
}