using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using PlayerActivities.Models;
using PlayerActivities.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PlayerActivities.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginActivities.xaml
    /// </summary>
    public partial class PluginActivities : PluginUserControlExtend
    {
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;
        internal override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginActivitiesDataContext ControlDataContext = new PluginActivitiesDataContext();
        internal override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginActivitiesDataContext)controlDataContext;
        }


        public PluginActivities()
        {
            AlwaysShow = true;

            InitializeComponent();
            this.DataContext = ControlDataContext;

            _ = Task.Run(() =>
            {
                // Wait extension database are loaded
                _ = System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher?.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationActivities;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            ControlDataContext.ShowImage = newContext == null;
            ControlDataContext.Items = newContext == null ? PluginDatabase.GetActivitiesData() : PluginDatabase.GetActivitiesData(newContext.Id);
        }

        private void DockPanel_LayoutUpdated(object sender, EventArgs e)
        {
            ControlDataContext.MaxHeight = this.MaxHeight - 40;
        }
    }


    public class PluginActivitiesDataContext : ObservableObject, IDataContext
    {
        private bool isActivated;
        public bool IsActivated { get => isActivated; set => SetValue(ref isActivated, value); }

        private bool showImage;
        public bool ShowImage { get => showImage; set => SetValue(ref showImage, value); }

        private double maxHeight = double.NaN;
        public double MaxHeight { get => maxHeight; set => SetValue(ref maxHeight, value); }

        private ObservableCollection<ActivityListGrouped> items;
        public ObservableCollection<ActivityListGrouped> Items { get => items; set => SetValue(ref items, value); }
    }
}
