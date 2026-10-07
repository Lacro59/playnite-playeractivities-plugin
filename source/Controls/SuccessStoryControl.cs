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
using System.Windows.Threading;
using static CommonPluginsShared.PlayniteTools;

namespace PlayerActivities.Controls
{
    /// <summary>
    /// Serializes lazy plugin-host creation on the UI thread so scrolling does not flood GetGameViewControl / ffprobe.
    /// </summary>
    internal static class LazyPluginHostScheduler
    {
        private static readonly object Sync = new object();
        private static readonly Queue<Action> Pending = new Queue<Action>();
        private static bool _pumpScheduled;

        public static void Enqueue(Action action)
        {
            if (action == null)
            {
                return;
            }

            lock (Sync)
            {
                Pending.Enqueue(action);
                if (_pumpScheduled)
                {
                    return;
                }

                _pumpScheduled = true;
            }

            Dispatcher dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                action();
                return;
            }

            _ = dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(Pump));
        }

        private static void Pump()
        {
            Action next = null;
            lock (Sync)
            {
                if (Pending.Count > 0)
                {
                    next = Pending.Dequeue();
                }
                else
                {
                    _pumpScheduled = false;
                    return;
                }
            }

            try
            {
                next();
            }
            finally
            {
                ScheduleNextPumpIfNeeded();
            }
        }

        private static void ScheduleNextPumpIfNeeded()
        {
            bool more;
            lock (Sync)
            {
                more = Pending.Count > 0;
                if (!more)
                {
                    _pumpScheduled = false;
                }
            }

            if (!more)
            {
                return;
            }

            Dispatcher dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                Pump();
                return;
            }

            _ = dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(Pump));
        }
    }

    /// <summary>
    /// Utility class to interface with the SuccessStory plugin.
    /// </summary>
    public class SuccessStoryPlugin
    {
        // Cached instance of the SuccessStory plugin for better performance
        private static readonly Plugin CachedPlugin = API.Instance?.Addons?.Plugins?.FirstOrDefault(p => p.Id == GetPluginId(ExternalPlugin.SuccessStory));

        // Reference to the PlayerActivities plugin database
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        /// <summary>
        /// Indicates whether the SuccessStory plugin is installed.
        /// </summary>
        public static bool IsInstalled => CachedPlugin != null;

        /// <summary>
        /// Opens the SuccessStory view for the specified game.
        /// </summary>
        /// <param name="game">Target game</param>
        public static void SuccessStoryView(Game game)
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
    /// Custom control to host SuccessStory plugin UI for a specific game.
    /// The plugin view is created lazily when the control is loaded and visible (avoids UI freezes on virtualized lists).
    /// </summary>
    public class SuccessStoryControl : ContentControl
    {
        private static readonly Plugin CachedPlugin = API.Instance?.Addons?.Plugins?
            .FirstOrDefault(p => p.Id == PlayniteTools.GetPluginId(ExternalPlugin.SuccessStory));

        private readonly string _controlName;
        private PluginUserControl _control;
        private bool _ensureScheduled;

        /// <summary>
        /// Indicates whether the SuccessStory plugin is installed.
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
            typeof(SuccessStoryControl),
            new FrameworkPropertyMetadata(null, ControlsPropertyChangedCallback));

        /// <summary>
        /// The date when an achievement was unlocked.
        /// </summary>
        public DateTime DateUnlocked
        {
            get => (DateTime)GetValue(DateUnlockedProperty);
            set => SetValue(DateUnlockedProperty, value);
        }

        public static readonly DependencyProperty DateUnlockedProperty = DependencyProperty.Register(
            nameof(DateUnlocked),
            typeof(DateTime),
            typeof(SuccessStoryControl),
            new FrameworkPropertyMetadata(DateTime.Now, ControlsPropertyChangedCallback));

        internal static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            SuccessStoryControl obj = sender as SuccessStoryControl;
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
        /// <param name="controlName">Name of the plugin view to load.</param>
        public SuccessStoryControl(string controlName)
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
                    Common.LogDebug($"[SuccessStoryControl] Created '{_controlName}' for '{GameContext?.Name}'");
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"[SuccessStoryControl] Failed to create '{_controlName}'", false, PlayerActivities.PluginDatabase.PluginName);
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

            _control.Tag = DateUnlocked;
            _control.GameContext = GameContext;
            _control.GameContextChanged(null, GameContext);
        }
    }

    /// <summary>
    /// Derived control to load the 'PluginCompactUnlocked' view from the SuccessStory plugin.
    /// </summary>
    public class SuccessStoryPluginCompactUnlocked : SuccessStoryControl
    {
        public SuccessStoryPluginCompactUnlocked() : base("PluginCompactUnlocked")
        {
        }
    }
}
