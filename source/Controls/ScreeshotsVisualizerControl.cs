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
    public class ScreeshotsVisualizerControl : ContentControl
    {
        private static Plugin _plugin;
        private static Plugin Plugin
        {
            get
            {
                if (_plugin == null)
                {
                    _plugin = API.Instance?.Addons?.Plugins?.FirstOrDefault(p => p.Id == Guid.Parse("c6c8276f-91bf-48e5-a1d1-4bee0b493488")) ?? null;
                }
                return _plugin;
            }
        }

        private PluginUserControl control;

        public static bool ScreeshotsVisualizerIsInstalled => Plugin != null;


        #region Properties
        public Game GameContext
        {
            get { return (Game)GetValue(GameContextProperty); }
            set { SetValue(GameContextProperty, value); }
        }

        public static readonly DependencyProperty GameContextProperty = DependencyProperty.Register(
            nameof(GameContext),
            typeof(Game),
            typeof(ScreeshotsVisualizerControl),
            new FrameworkPropertyMetadata(null, ControlsPropertyChangedCallback));

        public DateTime DateTaked
        {
            get { return (DateTime)GetValue(DateTakedProperty); }
            set { SetValue(DateTakedProperty, value); }
        }

        public static readonly DependencyProperty DateTakedProperty = DependencyProperty.Register(
            nameof(DateTaked),
            typeof(DateTime),
            typeof(ScreeshotsVisualizerControl),
            new FrameworkPropertyMetadata(DateTime.Now, ControlsPropertyChangedCallback));
        #endregion


        #region OnPropertyChange
        // When a control properties is changed
        internal static void ControlsPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ScreeshotsVisualizerControl obj && e.NewValue != e.OldValue && e.NewValue is DateTime)
            {
                if (obj.control != null)
                {
                    obj.control.Tag = (DateTime)e.NewValue;
                    obj.control.GameContextChanged(null, obj.GameContext);
                }
            }
        }
        #endregion


        public ScreeshotsVisualizerControl(string controlName)
        {
            if (Plugin == null)
            {
                return;
            }

            control = Plugin.GetGameViewControl(new GetGameViewControlArgs
            {
                Name = controlName,
                Mode = ApplicationMode.Desktop,
            }) as PluginUserControl;

            if (control == null)
            {
                return;
            }

            Content = control;
        }
    }


    public class ScreeshotsVisualizerPluginListScreenshots : ScreeshotsVisualizerControl
    {
        public ScreeshotsVisualizerPluginListScreenshots() : base("PluginListScreenshots")
        {

        }
    }
}
