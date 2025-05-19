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
    /// Utility class to interface with the SuccessStory plugin.
    /// </summary>
    public class SuccessStoryPlugin
    {
        // Reference to the PlayerActivities plugin database
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        // Gets the SuccessStory plugin instance by GUID
        private static Plugin Plugin => API.Instance?.Addons?.Plugins?
            .FirstOrDefault(p => p.Id == PlayniteTools.GetPluginId(ExternalPlugin.SuccessStory));

        /// <summary>
        /// Indicates if the SuccessStory plugin is installed.
        /// </summary>
        public static bool IsInstalled => Plugin != null;

        /// <summary>
        /// Opens the SuccessStory view for the specified game.
        /// </summary>
        /// <param name="game">Target game</param>
        public static void SuccessStoryView(Game game)
        {
            if (game == null || Plugin == null)
            {
                return;
            }

            try
            {
                var pluginMenus = Plugin.GetGameMenuItems(new GetGameMenuItemsArgs
                {
                    Games = new List<Game> { game },
                    IsGlobalSearchRequest = false
                });

                var firstMenu = pluginMenus.FirstOrDefault();
                if (firstMenu?.Action != null)
                {
                    firstMenu.Action.Invoke(null);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
    }

    /// <summary>
    /// Custom control to host SuccessStory plugin UI for a specific game.
    /// </summary>
    public class SuccessStoryControl : ContentControl
    {
        private static Plugin Plugin => API.Instance?.Addons?.Plugins?
            .FirstOrDefault(p => p.Id == Guid.Parse("cebe6d32-8c46-4459-b993-5a5189d60788"));

        private PluginUserControl Control { get; }

        /// <summary>
        /// Indicates if the SuccessStory plugin is installed.
        /// </summary>
        public static bool IsInstalled => Plugin != null;

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
        #endregion

        #region Property Change Handler

        // Called when a dependency property is changed
        internal static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var obj = sender as SuccessStoryControl;

            if (obj?.Control != null)
            {
                if (e.Property == DateUnlockedProperty && e.NewValue is DateTime newDate)
                {
                    obj.Control.Tag = newDate;
                }

                obj.Control.GameContext = obj.GameContext;
                obj.Control.GameContextChanged(null, obj.GameContext);
            }
        }
        #endregion

        /// <summary>
        /// Initializes the control and loads the plugin UI for the given control name.
        /// </summary>
        /// <param name="controlName">Name of the plugin view to load.</param>
        public SuccessStoryControl(string controlName)
        {
            if (Plugin == null)
            {
                return;
            }

            Control = Plugin.GetGameViewControl(new GetGameViewControlArgs
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
    /// Derived control to load the 'PluginCompactUnlocked' view from the SuccessStory plugin.
    /// </summary>
    public class SuccessStoryPluginCompactUnlocked : SuccessStoryControl
    {
        public SuccessStoryPluginCompactUnlocked() : base("PluginCompactUnlocked")
        {
        }
    }
}