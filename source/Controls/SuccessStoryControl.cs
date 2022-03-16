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
    public class SuccessStoryControl : ContentControl
    {
        private static Plugin _plugin;
        private static Plugin Plugin
        {
            get
            {
                if (_plugin == null)
                {
                    _plugin = API.Instance.Addons.Plugins.FirstOrDefault(p => p.Id == Guid.Parse("cebe6d32-8c46-4459-b993-5a5189d60788"));
                }
                return _plugin;
            }
        }

        private PluginUserControl control;

        public static bool SuccessStoryIsInstalled => Plugin != null;


        #region Properties
        public Game GameContext
        {
            get { return (Game)GetValue(GameContextProperty); }
            set { SetValue(GameContextProperty, value); }
        }

        public static readonly DependencyProperty GameContextProperty = DependencyProperty.Register(
            nameof(GameContext),
            typeof(Game),
            typeof(SuccessStoryControl),
            new FrameworkPropertyMetadata(null, ControlsPropertyChangedCallback));

        public DateTime DateUnlocked
        {
            get { return (DateTime)GetValue(DateUnlockedProperty); }
            set { SetValue(DateUnlockedProperty, value); }
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
                if (obj.control != null)
                {
                    obj.control.Tag = (DateTime)e.NewValue;
                    obj.control.GameContextChanged(null, obj.GameContext);
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


    public class SuccessStoryPluginCompactUnlocked : SuccessStoryControl
    {
        public SuccessStoryPluginCompactUnlocked() : base("PluginCompactUnlocked")
        {

        }
    }
}
