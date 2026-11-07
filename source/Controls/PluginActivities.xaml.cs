using CommonPlayniteShared.Commands;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Commands;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using PlayerActivities.Models;
using PlayerActivities.Services;
using PlayerActivities.Services.Achievements;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

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
            try
            {
                ControlDataContext.ShowImage = newContext == null;
                ControlDataContext.GameContext = newContext;

                if (newContext == null)
                {
                    ControlDataContext.Items = PluginDatabase.GetActivitiesData();
                    ControlDataContext.ActivitiesView = null;
                    Common.LogDebug($"[PluginActivities] SetData mode=multi groups={ControlDataContext.Items?.Count ?? 0}");
                    return;
                }

                ObservableCollection<ActivityListGrouped> groups = PluginDatabase.GetActivitiesData(newContext.Id);
                ControlDataContext.Items = groups;
                ListCollectionView monoView = BuildMonoActivitiesView(groups);
                ControlDataContext.ActivitiesView = monoView;
                int activityCount = monoView?.Count ?? 0;
                int dayGroups = monoView?.Groups?.Count ?? 0;
                Common.LogDebug($"[PluginActivities] SetData mode=mono gameId={newContext.Id} activities={activityCount} dayGroups={dayGroups}");
            }
            catch (Exception ex)
            {
                string mode = newContext == null ? "multi" : "mono";
                string gameId = newContext == null ? "none" : newContext.Id.ToString();
                Common.LogError(ex, false, $"[PluginActivities] SetData failed mode={mode} gameId={gameId}", false, PluginDatabase.PluginName);
            }
        }

        /// <summary>
        /// Builds a date-grouped collection view over the single-game activity list (no store API change).
        /// </summary>
        /// <param name="groups">Grouped payload from <c>GetActivitiesData(Guid)</c> (typically one group).</param>
        /// <returns>A <see cref="ListCollectionView"/> grouped by <see cref="Activity.DateActivity"/>, or null if empty.</returns>
        private static ListCollectionView BuildMonoActivitiesView(ObservableCollection<ActivityListGrouped> groups)
        {
            ObservableCollection<ActivityList> flat = new ObservableCollection<ActivityList>();
            if (groups == null || groups.Count == 0)
            {
                return null;
            }

            ActivityListGrouped group = groups[0];
            if (group?.ActivitiesOrdered == null)
            {
                return null;
            }

            foreach (Activity activity in group.ActivitiesOrdered)
            {
                if (activity == null)
                {
                    continue;
                }

                flat.Add(new ActivityList
                {
                    GameContext = group.GameContext,
                    DateActivity = activity.DateActivity,
                    Type = activity.Type,
                    Value = activity.Value
                });
            }

            if (flat.Count == 0)
            {
                return null;
            }

            ListCollectionView view = new ListCollectionView(flat);
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Activity.DateActivity)));
            return view;
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
        private static PlayerActivitiesDatabase PluginDatabase => PlayerActivities.PluginDatabase;

        private bool _isActivated;
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        private bool _showImage;
        public bool ShowImage { get => _showImage; set => SetValue(ref _showImage, value); }

        private double _maxHeight = double.NaN;
        public double MaxHeight { get => _maxHeight; set => SetValue(ref _maxHeight, value); }

        private Game _gameContext;
        /// <summary>Selected game when the host provides a single-game context; null in multi-game mode.</summary>
        public Game GameContext { get => _gameContext; set => SetValue(ref _gameContext, value); }

        private ObservableCollection<ActivityListGrouped> _items;
        public ObservableCollection<ActivityListGrouped> Items { get => _items; set => SetValue(ref _items, value); }

        private ICollectionView _activitiesView;
        /// <summary>Date-grouped flat activities for mono-game feed; null in multi-game mode.</summary>
        public ICollectionView ActivitiesView { get => _activitiesView; set => SetValue(ref _activitiesView, value); }

        #region Context menu commands (PART_LbTimeLine DataContext)

        public RelayCommand<Game> StartGameCommand { get; } = new RelayCommand<Game>((game)
            => API.Instance.StartGame(game.Id));

        public RelayCommand<Game> InstallGameCommand { get; } = new RelayCommand<Game>((game)
            => API.Instance.InstallGame(game.Id));

        public RelayCommand<Game> ShowGameInLibraryCommand { get; } = new RelayCommand<Game>((game)
            => CommandsNavigation.GoToGame.Execute(game.Id));

        public RelayCommand<Game> RefreshGameDataCommand { get; } = new RelayCommand<Game>((game) =>
        {
            if (game == null)
            {
                return;
            }

            _ = Task.Run(() =>
            {
                try
                {
                    PluginDatabase.InitializePluginData(true, game.Id);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, "[PluginActivities] RefreshGameDataCommand failed", false, PluginDatabase.PluginName);
                }
            });
        });

        public RelayCommand<Game> ShowGameSuccessStoryCommand { get; } = new RelayCommand<Game>((game)
            => PaAchievementsView.Open(game));

        public RelayCommand<Game> ShowGameHowLongToBeatCommand { get; } = new RelayCommand<Game>((game)
            => HowLongToBeatPlugin.HowLongToBeatView(game));

        public RelayCommand<Game> ShowGameScreenshotsVisualizerCommand { get; } = new RelayCommand<Game>((game)
            => ScreenshotsVisualizerPlugin.ScreenshotsVisualizerView(game));

        #endregion
    }
}
