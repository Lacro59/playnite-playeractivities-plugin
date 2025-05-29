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

namespace PlayerActivities.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginActivities.xaml
    /// </summary>
    public partial class PluginActivities : PluginUserControlExtend
    {
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginActivitiesDataContext ControlDataContext = new PluginActivitiesDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginActivitiesDataContext)controlDataContext;
        }


        public PluginActivities()
        {
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
        private bool _isActivated;
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        private bool _showImage;
        public bool ShowImage { get => _showImage; set => SetValue(ref _showImage, value); }

        private double _maxHeight = double.NaN;
        public double MaxHeight { get => _maxHeight; set => SetValue(ref _maxHeight, value); }

        private ObservableCollection<ActivityListGrouped> _items;
        public ObservableCollection<ActivityListGrouped> Items { get => _items; set => SetValue(ref _items, value); }
    }
}
