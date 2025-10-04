using CommonPluginsShared;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Controls;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Controls
{
    /// <summary>
    /// Utility class to interface with the ScreenshotsVisualizer plugin.
    /// </summary>
    public class ScreenshotsVisualizerPlugin
    {
        // Cached instance of the ScreenshotsVisualizer plugin for better performance
        private static readonly Plugin CachedPlugin = API.Instance?.Addons?.Plugins?.FirstOrDefault(p => p.Id == GetPluginId(ExternalPlugin.ScreenshotsVisualizer));

        // Reference to the PlayerActivities plugin database
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        /// <summary>
        /// Indicates whether the ScreenshotsVisualizer plugin is installed.
        /// </summary>
        public static bool IsInstalled => CachedPlugin != null;

        /// <summary>
        /// Opens the ScreenshotsVisualizer view for a specific game.
        /// </summary>
        /// <param name="game">Target game</param>
        public static void ScreenshotsVisualizerView(Game game)
        {
            if (game == null || CachedPlugin == null)
            {
                return;
            }

            try
            {
                var pluginMenus = CachedPlugin.GetGameMenuItems(new GetGameMenuItemsArgs
                {
                    Games = new List<Game> { game },
                    IsGlobalSearchRequest = false
                });
                pluginMenus?.FirstOrDefault()?.Action?.Invoke(null);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
    }

    /// <summary>
    /// Custom control to host ScreenshotsVisualizer plugin UI for a specific game.
    /// </summary>
    public class ScreenshotsVisualizerControl : ContentControl
    {
        // Cached instance of the ScreenshotsVisualizer plugin
        private static readonly Plugin CachedPlugin = API.Instance?.Addons?.Plugins?
            .FirstOrDefault(p => p.Id == PlayniteTools.GetPluginId(ExternalPlugin.ScreenshotsVisualizer));

        private PluginUserControl Control { get; }

        /// <summary>
        /// Indicates whether the ScreenshotsVisualizer plugin is installed.
        /// </summary>
        public static bool IsInstalled => CachedPlugin != null;

        #region Dependency Properties

        /// <summary>
        /// The game context used for the control.
        /// </summary>
        public Game GameContext
        {
            get => (Game)GetValue(GameContextProperty);
            set => SetValue(GameContextProperty, value);
        }

        public static readonly DependencyProperty GameContextProperty = DependencyProperty.Register(
            nameof(GameContext),
            typeof(Game),
            typeof(ScreenshotsVisualizerControl),
            new FrameworkPropertyMetadata(null, ControlsPropertyChangedCallback));

        /// <summary>
        /// The date the screenshot was taken.
        /// </summary>
        public DateTime DateTaked
        {
            get => (DateTime)GetValue(DateTakedProperty);
            set => SetValue(DateTakedProperty, value);
        }

        public static readonly DependencyProperty DateTakedProperty = DependencyProperty.Register(
            nameof(DateTaked),
            typeof(DateTime),
            typeof(ScreenshotsVisualizerControl),
            new FrameworkPropertyMetadata(DateTime.Now, ControlsPropertyChangedCallback));
        #endregion

        #region Property Change Handler

        // Called when any of the dependency properties are changed
        internal static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var obj = sender as ScreenshotsVisualizerControl;

            if (obj?.Control != null)
            {
                if (e.Property == DateTakedProperty && e.NewValue is DateTime newDate)
                {
                    obj.Control.Tag = newDate;
                }

                obj.Control.GameContext = obj.GameContext;
                obj.Control.GameContextChanged(null, obj.GameContext);
            }
        }
        #endregion

        /// <summary>
        /// Initializes the control and loads the plugin view with the given control name.
        /// </summary>
        /// <param name="controlName">Name of the control to load from plugin.</param>
        public ScreenshotsVisualizerControl(string controlName)
        {
            if (!IsInstalled)
            {
                return;
            }

            Control = CachedPlugin.GetGameViewControl(new GetGameViewControlArgs
            {
                Name = controlName,
                Mode = ApplicationMode.Desktop
            }) as PluginUserControl;

            if (Control != null)
            {
                Content = Control;
            }
        }
    }

    /// <summary>
    /// Derived control to load the 'PluginListScreenshots' control from the plugin.
    /// </summary>
    public class ScreenshotsVisualizerPluginListScreenshots : ScreenshotsVisualizerControl
    {
        public ScreenshotsVisualizerPluginListScreenshots() : base("PluginListScreenshots")
        {
        }
    }
}