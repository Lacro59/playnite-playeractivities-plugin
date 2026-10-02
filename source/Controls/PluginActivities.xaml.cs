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
using System.Windows;

namespace PlayerActivities.Controls
{
    /// <summary>
    /// Theme control listing recent player activities for the selected game (or all games).
    /// </summary>
    public partial class PluginActivities : PluginUserControlExtend
    {
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;
        protected override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginActivitiesDataContext ControlDataContext = new PluginActivitiesDataContext();
        protected override IDataContext controlDataContext
        {
            get => ControlDataContext;
            set => ControlDataContext = (PluginActivitiesDataContext)value;
        }

        public PluginActivities()
        {
            InitializeComponent();
            DataContext = ControlDataContext;
            Loaded += OnLoaded;
        }

        /// <inheritdoc/>
        protected override void AttachStaticEvents()
        {
            base.AttachStaticEvents();

            AttachPluginEvents(PluginDatabase.PluginName, () =>
            {
                PluginDatabase.PluginSettings.PropertyChanged += CreatePluginSettingsHandler();
                PluginDatabase.DatabaseItemUpdated += CreateDatabaseItemUpdatedHandler<PlayerActivitiesData>();
                PluginDatabase.DatabaseItemCollectionChanged += CreateDatabaseCollectionChangedHandler<PlayerActivitiesData>();
            });
        }

        /// <inheritdoc/>
        public override void SetDefaultDataContext()
        {
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.EnableIntegrationActivities;
        }

        /// <inheritdoc/>
        public override void SetData(Game newContext, PluginGameEntry pluginGameData)
        {
            ControlDataContext.ShowImage = newContext == null;
            ControlDataContext.Items = newContext == null
                ? PluginDatabase.GetActivitiesData()
                : PluginDatabase.GetActivitiesData(newContext.Id);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// When no game is selected the control lists all activities — refresh on any item update.
        /// </remarks>
        protected override void OnDatabaseItemUpdated(List<Guid> updatedIds)
        {
            if (GameContext == null)
            {
                ScheduleDataRefresh("database-item-updated");
                return;
            }

            base.OnDatabaseItemUpdated(updatedIds);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Always refresh: the all-games list must update when items are added or removed.
        /// </remarks>
        protected override void OnDatabaseCollectionChanged()
        {
            ScheduleDataRefresh("database-collection-changed");
        }

        private void DockPanel_LayoutUpdated(object sender, EventArgs e)
        {
            ControlDataContext.MaxHeight = this.MaxHeight - 40;
        }
    }

    /// <summary>
    /// Data context for <see cref="PluginActivities"/>.
    /// </summary>
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
