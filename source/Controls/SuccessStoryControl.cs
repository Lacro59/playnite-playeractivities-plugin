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
    public class SuccessStoryPlugin
    {
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;
        private static Plugin Plugin => API.Instance?.Addons?.Plugins?.FirstOrDefault(p => p.Id == Guid.Parse("cebe6d32-8c46-4459-b993-5a5189d60788")) ?? null;

        public static bool IsInstalled => Plugin != null;

        public static void SuccessStoryView(Game game)
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

    public class SuccessStoryControl : ContentControl
    {
        private static Plugin Plugin => API.Instance?.Addons?.Plugins?.FirstOrDefault(p => p.Id == Guid.Parse("cebe6d32-8c46-4459-b993-5a5189d60788")) ?? null;

        private PluginUserControl Control { get; }

        public static bool IsInstalled => Plugin != null;


        #region Properties
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


        #region OnPropertyChange
        // When a control properties is changed
        internal static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is SuccessStoryControl obj && e.NewValue != e.OldValue && e.NewValue is DateTime)
            {
                if (obj.Control != null)
                {
                    obj.Control.Tag = (DateTime)e.NewValue;
                    obj.Control.GameContext = obj.GameContext;
                    obj.Control.GameContextChanged(null, obj.GameContext);
                }
            }
        }
        #endregion


        public SuccessStoryControl(string controlName)
        {
            if (Plugin == null)
            {
                return;
            }

            Control = Plugin.GetGameViewControl(new GetGameViewControlArgs
            {
                Name = controlName,
                Mode = ApplicationMode.Desktop,
            }) as PluginUserControl;

            if (Control == null)
            {
                return;
            }

            Content = Control;
        }
    }


    public class SuccessStoryPluginCompactUnlocked : SuccessStoryControl
    {
        public SuccessStoryPluginCompactUnlocked() : base("PluginCompactUnlocked")
        {

        }
    }
}
