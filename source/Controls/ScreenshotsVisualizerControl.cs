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
    /// The plugin view is created lazily when the control is loaded and visible (avoids UI freezes / ffprobe storms on virtualized lists).
    /// </summary>
    public class ScreenshotsVisualizerControl : ContentControl
    {
        private static readonly Plugin CachedPlugin = API.Instance?.Addons?.Plugins?
            .FirstOrDefault(p => p.Id == PlayniteTools.GetPluginId(ExternalPlugin.ScreenshotsVisualizer));

        private readonly string _controlName;
        private PluginUserControl _control;
        private bool _ensureScheduled;

        /// <summary>
        /// Indicates whether the ScreenshotsVisualizer plugin is installed.
        /// </summary>
        public static bool IsInstalled => CachedPlugin != null;

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

        internal static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ScreenshotsVisualizerControl obj = sender as ScreenshotsVisualizerControl;
            if (obj == null)
            {
                return;
            }

            obj.ScheduleEnsurePluginControl();
            obj.ApplyContextToPlugin();
        }

        /// <summary>
        /// Initializes the host; plugin UI is created later when visible.
        /// </summary>
        /// <param name="controlName">Name of the control to load from plugin.</param>
        public ScreenshotsVisualizerControl(string controlName)
        {
            _controlName = controlName;
            Loaded += (s, e) => ScheduleEnsurePluginControl();
            Unloaded += (s, e) => TearDownPluginControl();
            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    ScheduleEnsurePluginControl();
                }
                else
                {
                    TearDownPluginControl();
                }
            };
        }

        private void ScheduleEnsurePluginControl()
        {
            if (_control != null || !IsInstalled || _ensureScheduled)
            {
                return;
            }

            if (!IsLoaded || !IsVisible)
            {
                return;
            }

            _ensureScheduled = true;
            LazyPluginHostScheduler.Enqueue(() =>
            {
                _ensureScheduled = false;
                EnsurePluginControl();
            });
        }

        private void EnsurePluginControl()
        {
            if (_control != null || !IsInstalled || !IsLoaded || !IsVisible)
            {
                return;
            }

            try
            {
                _control = CachedPlugin.GetGameViewControl(new GetGameViewControlArgs
                {
                    Name = _controlName,
                    Mode = ApplicationMode.Desktop
                }) as PluginUserControl;

                if (_control != null)
                {
                    Content = _control;
                    ApplyContextToPlugin();
                    Common.LogDebug($"[ScreenshotsVisualizerControl] Created '{_controlName}' for '{GameContext?.Name}'");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"[ScreenshotsVisualizerControl] Failed to create '{_controlName}'", false, PlayerActivities.PluginDatabase.PluginName);
            }
        }

        private void TearDownPluginControl()
        {
            if (_control == null && Content == null)
            {
                return;
            }

            Content = null;
            _control = null;
            _ensureScheduled = false;
        }

        private void ApplyContextToPlugin()
        {
            if (_control == null)
            {
                return;
            }

            _control.Tag = DateTaked;
            _control.GameContext = GameContext;
            _control.GameContextChanged(null, GameContext);
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
